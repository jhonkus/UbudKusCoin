using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;

namespace UbudKusCoin.Services;

public sealed class BlockCommitResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    internal List<Transaction> Transactions { get; init; }

    public static BlockCommitResult Ok() => new() { Success = true, Message = "Block committed successfully" };
    public static BlockCommitResult Fail(string message) => new() { Success = false, Message = message };
}

/// <summary>
/// Validates and commits legacy gRPC blocks through one serialized write path.
/// </summary>
public sealed class BlockCommitService
{
    private readonly object writeLock = new();

    public BlockCommitResult ValidateAndCommit(Block block)
    {
        lock (writeLock)
        {
            var validation = Validate(block);
            if (!validation.Success)
            {
                return validation;
            }

            var transactions = validation.Transactions;
            var touchedAddresses = transactions
                .SelectMany(x => new[] { x.Sender, x.Recipient })
                .Where(x => x != "-")
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var snapshots = touchedAddresses.ToDictionary(
                address => address,
                address => Clone(ServicePool.DbService.AccountDb.GetByAddress(address)),
                StringComparer.Ordinal);

            try
            {
                ApplyBalances(transactions);
                if (!ServicePool.DbService.TransactionDb.AddBulk(transactions))
                {
                    throw new InvalidOperationException("Unable to persist block transactions");
                }

                var blockStatus = ServicePool.DbService.BlockDb.Add(block);
                if (blockStatus.Status != Constants.TXN_STATUS_SUCCESS)
                {
                    throw new InvalidOperationException("Unable to persist block");
                }

                ServicePool.DbService.PoolTransactionsDb.DeleteAll();
                return BlockCommitResult.Ok();
            }
            catch (Exception exception)
            {
                Rollback(block, transactions, snapshots);
                return BlockCommitResult.Fail($"Block commit rolled back: {exception.Message}");
            }
        }
    }

    public BlockCommitResult Validate(Block block)
    {
        var lastBlock = ServicePool.DbService.BlockDb.GetLast();
        if (block is null || lastBlock is null)
        {
            return BlockCommitResult.Fail("Missing block or chain head");
        }

        if (ServicePool.DbService.BlockDb.GetByHash(block.Hash) is not null)
        {
            return BlockCommitResult.Fail("Duplicate block");
        }

        if (block.Height != lastBlock.Height + 1 || block.PrevHash != lastBlock.Hash)
        {
            return BlockCommitResult.Fail("Invalid block height or previous hash");
        }

        if (block.TimeStamp <= lastBlock.TimeStamp || string.IsNullOrWhiteSpace(block.Validator)
            || string.IsNullOrWhiteSpace(block.Hash) || string.IsNullOrWhiteSpace(block.Signature))
        {
            return BlockCommitResult.Fail("Invalid block timestamp or validator metadata");
        }

        if (block.Hash != ServicePool.FacadeService.Block.GetBlockHash(block)
            || !WalletService.CheckSignatureForAddress(block.Validator, block.Signature, block.Hash))
        {
            return BlockCommitResult.Fail("Invalid block hash or validator signature");
        }

        List<Transaction> transactions;
        try
        {
            transactions = JsonConvert.DeserializeObject<List<Transaction>>(block.Transactions);
        }
        catch
        {
            transactions = null;
        }

        if (transactions is null || transactions.Count == 0 || transactions.Count != block.NumOfTx)
        {
            return BlockCommitResult.Fail("Invalid block transaction list");
        }

        if (UkcUtils.CreateMerkleRoot(transactions.Select(x => x.Hash).ToArray()) != block.MerkleRoot)
        {
            return BlockCommitResult.Fail("Invalid merkle root");
        }

        if (transactions.Count(x => x.Sender == "-") != 1 || transactions[0].Sender != "-"
            || transactions[0].Recipient != block.Validator || transactions[0].Fee != 0
            || transactions[0].Signature != "-" || transactions[0].PubKey != "-")
        {
            return BlockCommitResult.Fail("Invalid coinbase transaction");
        }

        var balances = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var transaction in transactions)
        {
            if (!double.IsFinite(transaction.Amount) || !double.IsFinite(transaction.Fee)
                || transaction.Amount < 0 || transaction.Fee < 0
                || UkcUtils.GetTransactionHash(transaction) != transaction.Hash)
            {
                return BlockCommitResult.Fail("Invalid transaction amount, fee, or hash");
            }

            if (transaction.Sender == "-")
            {
                AddBalance(balances, transaction.Recipient, transaction.Amount);
                continue;
            }

            if (!TransactionServiceImpl.VerifySignature(transaction) || transaction.Sender == transaction.Recipient
                || transaction.Amount <= 0 || GetBalance(balances, transaction.Sender) < transaction.Amount + transaction.Fee)
            {
                return BlockCommitResult.Fail("Invalid transfer or insufficient balance");
            }

            AddBalance(balances, transaction.Sender, -(transaction.Amount + transaction.Fee));
            AddBalance(balances, transaction.Recipient, transaction.Amount);
        }

        if (!NearlyEqual(block.TotalAmount, UkcUtils.GetTotalAmount(transactions))
            || !NearlyEqual(block.TotalReward, UkcUtils.GetTotalFees(transactions))
            || transactions.Any(x => ServicePool.DbService.TransactionDb.GetByHash(x.Hash) is not null))
        {
            return BlockCommitResult.Fail("Invalid block totals or duplicate transaction");
        }

        return new BlockCommitResult { Success = true, Transactions = transactions };
    }

    private void ApplyBalances(IEnumerable<Transaction> transactions)
    {
        foreach (var transaction in transactions)
        {
            if (transaction.Sender != "-")
            {
                UpdateAccount(transaction.Sender, -(transaction.Amount + transaction.Fee), transaction.PubKey);
            }

            UpdateAccount(transaction.Recipient, transaction.Amount, transaction.Sender == "-" ? "-" : null);
        }
    }

    private static void UpdateAccount(string address, double delta, string publicKey)
    {
        var account = ServicePool.DbService.AccountDb.GetByAddress(address);
        if (account is null)
        {
            account = new Account
            {
                Address = address,
                PubKey = publicKey ?? "-",
                Balance = delta,
                TxnCount = 1,
                Created = UkcUtils.GetTime(),
                Updated = UkcUtils.GetTime()
            };
            ServicePool.DbService.AccountDb.Add(account);
            return;
        }

        account.Balance += delta;
        account.TxnCount++;
        account.Updated = UkcUtils.GetTime();
        if (!string.IsNullOrWhiteSpace(publicKey) && publicKey != "-")
        {
            account.PubKey = publicKey;
        }
        ServicePool.DbService.AccountDb.Update(account);
    }

    private static void Rollback(Block block, IEnumerable<Transaction> transactions, Dictionary<string, Account> snapshots)
    {
        ServicePool.DbService.BlockDb.RemoveByHash(block.Hash);
        foreach (var transaction in transactions)
        {
            ServicePool.DbService.TransactionDb.RemoveByHash(transaction.Hash);
        }

        foreach (var snapshot in snapshots)
        {
            ServicePool.DbService.AccountDb.RemoveByAddress(snapshot.Key);
            if (snapshot.Value is not null)
            {
                ServicePool.DbService.AccountDb.Add(snapshot.Value);
            }
        }
    }

    private static Account Clone(Account account)
    {
        return account is null ? null : new Account
        {
            Id = account.Id,
            Address = account.Address,
            PubKey = account.PubKey,
            Balance = account.Balance,
            TxnCount = account.TxnCount,
            Created = account.Created,
            Updated = account.Updated
        };
    }

    private static double GetBalance(Dictionary<string, double> balances, string address)
    {
        if (balances.TryGetValue(address, out var balance))
        {
            return balance;
        }

        return ServicePool.DbService.AccountDb.GetByAddress(address)?.Balance ?? 0;
    }

    private static void AddBalance(Dictionary<string, double> balances, string address, double amount)
    {
        balances[address] = GetBalance(balances, address) + amount;
    }

    private static bool NearlyEqual(double left, double right)
        => Math.Abs(left - right) <= 0.000000001;

}
