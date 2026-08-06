// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.


using System;
using System.Linq;
using Grpc.Core;
using System.Threading.Tasks;
using UbudKusCoin.Services;

namespace UbudKusCoin.Grpc
{
    public class AccountServiceImpl : AccountService.AccountServiceBase
    {
        public override Task<AccountList> GetRange(AccountParams request, ServerCallContext context)
        {
            var accounts = ServicePool.CanonicalNodeService.Chain.State.Accounts
                .Skip(Math.Max(0, request.PageNumber - 1) * request.ResultPerPage)
                .Take(Math.Max(0, request.ResultPerPage))
                .Select(ToAccount);
            var response = new AccountList();
            response.Accounts.AddRange(accounts);
            return Task.FromResult(response);
        }
        
        public override Task<Account> GetByAddress(Account request, ServerCallContext context)
        {
            var account = ServicePool.CanonicalNodeService.Chain.State.Accounts
                .FirstOrDefault(item => item.Address.Encoded == request.Address);
            return Task.FromResult(account is null ? new Account() : ToAccount(account));
        }

        public override Task<Account> GetByPubKey(Account request, ServerCallContext context)
        {
            var account = ServicePool.CanonicalNodeService.Chain.State.Accounts
                .FirstOrDefault(item => Convert.ToHexString(item.PubKey).Equals(request.PubKey, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(account is null ? new Account() : ToAccount(account));
        }

        private static Account ToAccount(UbudKusCoin.Core.Types.Account account)
            => new()
            {
                Address = account.Address.Encoded,
                PubKey = Convert.ToHexString(account.PubKey).ToLowerInvariant(),
                Balance = account.Balance.BaseUnits,
                TxnCount = (long)account.Nonce
            };
    }
}
