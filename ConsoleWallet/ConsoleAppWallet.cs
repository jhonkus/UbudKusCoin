// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Globalization;
using System.Threading.Tasks;
using UbudKusCoin.Grpc;
using static UbudKusCoin.Grpc.TransactionService;
using static UbudKusCoin.Grpc.BlockService;
using static UbudKusCoin.Grpc.AccountService;
using Grpc.Net.Client;

namespace UbudKusCoin.ConsoleWallet
{
    public class ConsoleAppWallet
    {
        private readonly AccountServiceClient _accountService;
        private readonly BlockServiceClient _blockService;
        private readonly TransactionServiceClient _transactionService;

        private Wallet _accountExt;

        public ConsoleAppWallet(GrpcChannel channel)
        {
            _accountService = new AccountServiceClient(channel);
            _blockService = new BlockServiceClient(channel);
            _transactionService = new TransactionServiceClient(channel);
            MenuItem();
            GetInput();
        }

        private void MenuItem()
        {
            if (_accountExt == null)
            {
                Console.Clear();
                Console.WriteLine("\n\n\n");
                Console.WriteLine("                    UBUDKUS COIN WALLET ");
                Console.WriteLine("============================================================");
                Console.WriteLine("  Address: - ");
                Console.WriteLine("============================================================");
                Console.WriteLine("                    1. Create Account");
                Console.WriteLine("                    2. Restore Account");
                Console.WriteLine("                    9. Exit");
                Console.WriteLine("------------------------------------------------------------");
            }
            else
            {
                Console.Clear();
                Console.WriteLine("\n\n\n");
                Console.WriteLine("                    UBUDKUS COIN WALLET ");
                Console.WriteLine("============================================================");
                Console.WriteLine("  Address: {0}", _accountExt.GetAddress());
                Console.WriteLine("============================================================");
                Console.WriteLine("                    1. Create Account");
                Console.WriteLine("                    2. Restore Account");
                Console.WriteLine("                    3. Send Coin");
                Console.WriteLine("                    4. Check Balance");
                Console.WriteLine("                    5. Transaction History");
                Console.WriteLine("                    6. Account Info");
                Console.WriteLine("                    7. Send Bulk Tx");
                Console.WriteLine("                    9. Exit");
                Console.WriteLine("------------------------------------------------------------");
            }
        }

        private void GetInput()
        {
            int selection = 0;
            while (selection != 20)
            {
                switch (selection)
                {
                    case 1:
                        DoCreateAccount();
                        break;
                    case 2:
                        DoRestore();
                        break;
                    case 3:
                        DoSendCoin();
                        break;
                    case 4:
                        DoGetBalance();
                        break;
                    case 5:
                        DoGetTransactionHistory();
                        break;
                    case 6:
                        DoShowAccountInfo();
                        break;
                    case 7:
                        DoSendBulkTx();
                        break;
                    case 9:
                        DoExit();
                        break;
                }

                if (selection != 0)
                {
                    Console.WriteLine("\n===== Press enter to continue! =====");
                    string strKey = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(strKey))
                    {
                        Console.Clear();
                        MenuItem();
                    }
                }

                Console.WriteLine("\n**** Please select a menu item!!! *****");
                string action = Console.ReadLine();
                if (!int.TryParse(action, out selection))
                {
                    selection = 0;
                    Console.Clear();
                    MenuItem();
                }
            }
        }

        private void DoSendBulkTx()
        {
            Console.Clear();
            Console.WriteLine("\n\n\n\nTransfer Coin");
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");

            Console.WriteLine("Sender address:");
            string sender = _accountExt.GetAddress();
            Console.WriteLine(sender);

            Console.WriteLine("\nPlease enter the recipient address!:");
            string recipient = Console.ReadLine();

            Console.WriteLine("\nPlease enter the number of Tx!:");
            string strNumOfTx = Console.ReadLine();

            double amount = 0.0001d;
            float fee = 0.0001f;

            if (string.IsNullOrEmpty(sender) ||
                string.IsNullOrEmpty(strNumOfTx) ||
                string.IsNullOrEmpty(recipient))
            {
                Console.WriteLine("\n\nError, Please input all data: sender, recipient, amount and fee!\n");
                return;
            }

            var response = _accountService.GetByAddress(new Account());
            var senderBalance = response.Balance;
            var numOfTx = int.Parse(strNumOfTx);

            if ((numOfTx * amount + fee) > senderBalance)
            {
                Console.WriteLine("\nError! The sender ({0}) does not have enough balance!", sender);
                Console.WriteLine("Sender's balance is {0}", senderBalance);
                return;
            }

            for (int i = 0; i < numOfTx; i++)
            {
                Console.Write(i + "- ");
                SendCoin(_accountExt.GetAddress(), recipient, amount, fee);
                System.Threading.Thread.Sleep(50);
            }
        }

        private void SendCoin(string sender, string recipient, double amount, float fee)
        {
            var newTxn = new Transaction
            {
                Sender = sender,
                TimeStamp = Others.Utils.GetTime(),
                Recipient = recipient,
                Amount = amount,
                Fee = fee,
                Height = 0,
                TxType = "Transfer",
            };

            var TxnHash = Others.Utils.GetTransactionHash(newTxn);
            var signature = _accountExt.Sign(TxnHash);
            newTxn.Hash = TxnHash;
            newTxn.Signature = signature;

            var transferRequest = new TransactionPost
            {
                SendingFrom = "Console Wallet",
                Transaction = newTxn
            };

            try
            {
                var transferResponse = _transactionService.Transfer(transferRequest);
                if (transferResponse.Status.ToLower() == "success")
                {
                    Console.WriteLine("== Success == ");
                }
                else
                {
                    Console.WriteLine("Error: {0}", transferResponse.Message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
        }

        private void DoShowAccountInfo()
        {
            WalletInfo();
        }

        private void DoSendCoin()
        {
            Console.Clear();
            Console.WriteLine("\n\n\n\nTransfer Coin");
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");

            Console.WriteLine("Sender address:");
            string sender = _accountExt.GetAddress();
            Console.WriteLine(sender);

            Console.WriteLine("\nPlease enter the recipient address!:");
            string recipient = Console.ReadLine();

            Console.WriteLine("\nPlease enter the amount (number)!:");
            string strAmount = Console.ReadLine();

            Console.WriteLine("\nPlease enter fee (number)!:");
            string strFee = Console.ReadLine();
            double amount;

            if (string.IsNullOrEmpty(sender) ||
                string.IsNullOrEmpty(recipient) ||
                string.IsNullOrEmpty(strAmount) ||
                string.IsNullOrEmpty(strFee))
            {
                Console.WriteLine("\n\nError, Please input all data: sender, recipient, amount and fee!\n");
                return;
            }

            if (!double.TryParse(strAmount, out amount))
            {
                Console.WriteLine("\nError! You have inputted the wrong value for  the amount!");
                return;
            }

            if (!float.TryParse(strFee, out var fee))
            {
                Console.WriteLine("\nError! You have inputted the wrong value for the fee!");
                return;
            }
            
            var response = _accountService.GetByAddress(new Account
            {
                Address = sender
            });

            var senderBalance = response.Balance;

            if ((amount + fee) > senderBalance)
            {
                Console.WriteLine("\nError! Sender ({0}) does not have enough balance!", sender);
                Console.WriteLine("Sender balance is {0}", senderBalance);
                return;
            }

            var NewTxn = new Transaction
            {
                Sender = _accountExt.GetAddress(),
                TimeStamp = Others.Utils.GetTime(),
                Recipient = recipient,
                Amount = amount,
                Fee = fee,
                Height = 0,
                TxType = "Transfer",
                PubKey = _accountExt.GetKeyPair().PublicKeyHex,
            };

            var TxnHash = Others.Utils.GetTransactionHash(NewTxn);
            var signature = _accountExt.Sign(TxnHash);

            NewTxn.Hash = TxnHash;
            NewTxn.Signature = signature;

            var transferRequest = new TransactionPost
            {
                SendingFrom = "Console Wallet",
                Transaction = NewTxn
            };

            try
            {
                var transferResponse = _transactionService.Transfer(transferRequest);
                if (transferResponse.Status.ToLower() == "success")
                {
                    DateTime utcDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(Convert.ToDouble(NewTxn.TimeStamp));

                    Console.Clear();
                    Console.WriteLine("\n\n\n\nTransaction has been sent to the Blockchain!");
                    Console.WriteLine("Timestamp: {0}", utcDate.ToLocalTime());
                    Console.WriteLine("Sender: {0}", NewTxn.Sender);
                    Console.WriteLine("Recipient {0}", NewTxn.Recipient);
                    Console.WriteLine("Amount: {0}", NewTxn.Amount);
                    Console.WriteLine("Fee: {0}", NewTxn.Fee);
                    Console.WriteLine("-------------------");
                    Console.WriteLine("Please, wait the transaction processing!");
                }
                else
                {
                    Console.WriteLine("Error: {0}", transferResponse.Message);
                }
            }
            catch
            {
                Console.WriteLine("\nError! Please check the UbudKusCoin Node, it needs to be running!");
            }
        }

        private void DoRestore()
        {
            Console.Clear();
            Console.WriteLine("Restore Wallet");
            Console.WriteLine("Please enter 12 words passphrase:");
            
            string secret = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(secret))
            {
                Console.WriteLine("\n\nError, please input your 12 words passphrase!\n");
                return;
            }

            try
            {
                _accountExt = new Wallet(secret);
                WalletInfo();
            }
            catch
            {
                Console.WriteLine("Invalid passphrase!");
            }
        }

        private void DoCreateAccount()
        {
            _accountExt = new Wallet();
            WalletInfo();
        }

        private void WalletInfo()
        {
            Console.Clear();
            Console.WriteLine("\n\n\nYour Wallet");
            Console.WriteLine("======================");
            Console.WriteLine("\nADDRESS:\n{0}", _accountExt.GetAddress());
            Console.WriteLine("\nPUBLIC KEY:\n{0}", _accountExt.GetKeyPair().PublicKeyHex);
            Console.WriteLine("\nPASSPHRASE 12 words:\n{0}", _accountExt.Passphrase);
            Console.WriteLine("\n - - - - - - - - - - - - - - - - - - - - - - ");
            Console.WriteLine("*** Store your passphrase in a safe place! And please don't tell it to anyone!  ***");
            Console.WriteLine("*** If you lose your passphrase, your money will be lost!    ***");
            Console.WriteLine("*** Use your secret number to restore your account!              ***");
        }

        private static async void DoExit()
        {
            Console.Clear();
            Console.WriteLine("\n\nApplication closed!\n");
            await Task.Delay(2000);
            Environment.Exit(0);
        }

        private void DoGetTransactionHistory()
        {
            string address = _accountExt.GetAddress();
            if (string.IsNullOrWhiteSpace(address))
            {
                Console.WriteLine("\n\nError, the address is empty, please create an account first!\n");
                return;
            }

            Console.Clear();
            Console.WriteLine("Transaction History for {0}", address);
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");

            try
            {
                Console.WriteLine("OK");

                var response = _transactionService.GetRangeByAddress(new TransactionPaging
                {
                    Address = address,
                    PageNumber = 1,
                    ResultPerPage = 50
                });

                Console.WriteLine("=== Response");

                if (response != null && response.Transactions != null)
                {
                    foreach (var Txn in response.Transactions)
                    {
                        Console.WriteLine("Hash        : {0}", Txn.Hash);
                        Console.WriteLine("Timestamp   : {0}", Others.Utils.ToDateTime(Txn.TimeStamp));
                        Console.WriteLine("Sender      : {0}", Txn.Sender);
                        Console.WriteLine("Recipient   : {0}", Txn.Recipient);
                        Console.WriteLine("Amount      : {0}", Txn.Amount.ToString("N", CultureInfo.InvariantCulture));
                        Console.WriteLine("Fee         : {0}", Txn.Fee.ToString("N4", CultureInfo.InvariantCulture));
                        Console.WriteLine("--------------\n");
                    }
                }
                else
                {
                    Console.WriteLine("\n---- No records found! ---");
                }
            }
            catch
            {
                Console.WriteLine("\nError! Please check your UbudKusCoin Node, it needs to be running!");
            }
        }

        private void DoGetBalance()
        {
            string address = _accountExt.GetAddress();
            if (string.IsNullOrEmpty(address))
            {
                Console.WriteLine("\n\nError, the address is empty, please create an account first!\n");
                return;
            }

            Console.Clear();
            Console.WriteLine("Balance for {0}", address);
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");
            try
            {
                var response = _accountService.GetByAddress(new Account
                {
                    Address = address
                });
                Console.WriteLine("Balance: {0}", response.Balance.ToString("N", CultureInfo.InvariantCulture));
            }
            catch
            {
                Console.WriteLine("\nError! Please check your UbudKusCoin Node, it needs to be running!");
            }
        }
    }
}