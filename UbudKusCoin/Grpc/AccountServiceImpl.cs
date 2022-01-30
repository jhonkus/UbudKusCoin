using Grpc.Core;
using System.Threading.Tasks;
using UbudKusCoin.Services;
namespace UbudKusCoin.Grpc
{

    public class AccountServiceImpl : AccountService.AccountServiceBase
    {

        public override Task<AccountList> GetRange(AccountParams request, ServerCallContext context)
        {
            var accounts = ServicePool.DbService.accountDb.GetRange(request.PageNumber, request.ResultPerPage);
            var response = new AccountList();
            response.Accounts.AddRange(accounts);
            return Task.FromResult(response);
        }


        public override Task<Account> GetByAddress(Account request, ServerCallContext context)
        {
            var account = ServicePool.DbService.accountDb.GetByAddress(request.Address);
            return Task.FromResult(account);
        }

        public override Task<Account> GetByPubKey(Account request, ServerCallContext context)
        {
            var account = ServicePool.DbService.accountDb.GetByPubKey(request.PubKey);
            return Task.FromResult(account);
        }

        // public override Task<BalanceResponse> GetBalance(CommonRequest request, ServerCallContext context)
        // {
        //     var balance = Transaction.GetBalance(request.Address);
        //     return Task.FromResult(new BalanceResponse
        //     {
        //         Balance = balance
        //     });
        // }

        // public override Task<AccountResponse> GetAccount(CommonRequest request, ServerCallContext context)
        // {
        //     AccountResponse response = new AccountResponse();


        //     // 1. get all transaction bellong this account
        //     var transactions = Transaction.GetAccountTransactions(request.Address);

        //     if (transactions is null)
        //     {
        //         response.Transactions.Add(new TxnModel()); //no txn
        //     }
        //     else
        //     {

        //         foreach (Transaction Txn in transactions)
        //         {
        //             TxnModel mdl = ConvertTxnModel(Txn);
        //             response.Transactions.Add(mdl);
        //         }
        //     }

        //     // get Blocks by validator
        //     var blocks = Blockchain.GetBlocksByValidator(request.Address);
        //     if (blocks is null)
        //     {
        //         response.Blocks.Add(new BlockModel()); //no txn
        //     }
        //     else
        //     {
        //         response.NumBlockValidate = 0;
        //         foreach (Block block in blocks)
        //         {
        //             BlockModel mdl = ConvertBlockForList(block);
        //             response.Blocks.Add(mdl);
        //             // count number of block validated by this account
        //             response.NumBlockValidate += 1;
        //         }


        //     }

        //     // Get Account Balance
        //     var balance = Transaction.GetBalance(request.Address);
        //     response.Balance = balance;

        //     return Task.FromResult(response);
        // }

    }
}