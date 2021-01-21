using System;
using LiteDB;
using Newtonsoft.Json;
using PKusCoin.helper;

namespace PKusCoin.blockchain
{
    public class Account{
        public Int32 Id{set; get;}
        public string PublicKey {set; get;}
        public Int32 Balance {set; get;}
    }

    public class Accounts
    {
        
        public Accounts(){
            Console.WriteLine("== loading accounts ");
        }

        public static LiteCollection<Account> GetAccounts(){
            var coll  = LdbAccess.DB.GetCollection<Account>(LdbAccess.TBL_ACCOUNT);
            coll.EnsureIndex(x => x.PublicKey);
            return coll;
        }

        public static void Increment(string to, int amount)
        {
            var accounts = GetAccounts(); 
            var acc = accounts.FindOne(x=>x.PublicKey == to);
            if (acc==null){
                acc = new Account{
                    PublicKey = to,
                    Balance = amount
                };
                accounts.Insert(acc);
            }else{
                acc.Balance = acc.Balance + amount;
                accounts.Update(acc);
            }
        }

        public static void Decrement(string from, int amount)
        {
            var accounts = GetAccounts(); 
            var acc = accounts.FindOne(x=>x.PublicKey == from);
            if (acc==null){
                acc = new Account{
                    PublicKey = from,
                    Balance = -amount
                };
                accounts.Insert(acc);
            }else{
                acc.Balance = acc.Balance - amount;
                accounts.Update(acc);
            }
        }

        public static int GetBalance(string address)
        {
            var accounts = GetAccounts(); 
            var acc = accounts.FindOne(x=>x.PublicKey == address);
            if (acc==null){
                acc = new Account{
                    PublicKey = address,
                    Balance = 0
                };
                return 0;
            }else{
                return acc.Balance;
            }
        }

        public static void Transfer(string from, string to, int amount) {
            Console.WriteLine("======= 22  Transafer from:{0}, to:{1}, amount:{2}", from, to, amount);
            Increment(to, amount);
            Decrement(from, amount);
        }
        public static void Update(wallet.Transaction transaction) {
            Console.WriteLine("=====hhhhh=== Transaction: {0}", JsonConvert.SerializeObject(transaction));
            var amount = transaction.TxOutput.Amount;
            var from = transaction.TxInput.PublicKey;
            string to = transaction.TxOutput.PublicKey;
            Console.WriteLine("=======11  Transafer to:{0}", to);
            Transfer(from, to, amount);
        }

        public static void TransferFee(Block block, wallet.Transaction transaction) {
            var amount = transaction.TxOutput.Amount;
            var from = transaction.TxInput.PublicKey;
            var to = block.Validator;
            Console.WriteLine("============ xxxxx Transfer Fee Block Validator: {0}", to);

            Transfer(from, to , amount);
        }
   
    }
}