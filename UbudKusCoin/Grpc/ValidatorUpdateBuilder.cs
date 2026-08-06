#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using UbudKusCoin.CometBft.Abci;
using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Grpc;

public static class ValidatorUpdateBuilder
{
    public static IReadOnlyList<ValidatorUpdate> Build(State previousState, State state)
    {
        var updates = new List<ValidatorUpdate>();
        var currentKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var stake in state.Stakes.OrderBy(x => x.Address.Encoded, StringComparer.Ordinal))
        {
            if (stake.PubKey.Length is < 33 or > 65)
                continue;

            var key = Convert.ToHexString(stake.PubKey);
            currentKeys.Add(key);
            var power = stake.Jailed || stake.UnlockHeight != 0
                ? 0
                : Math.Max(1, Math.Min(long.MaxValue, stake.Amount.BaseUnits));
            updates.Add(CreateUpdate(stake.PubKey, power));
        }

        foreach (var stake in previousState.Stakes
            .Where(x => !currentKeys.Contains(Convert.ToHexString(x.PubKey)))
            .OrderBy(x => x.Address.Encoded, StringComparer.Ordinal))
        {
            updates.Add(CreateUpdate(stake.PubKey, 0));
        }

        return updates;
    }

    private static ValidatorUpdate CreateUpdate(byte[] publicKey, long power)
        => new()
        {
            PubKey = new PublicKey { Secp256K1 = ByteString.CopyFrom(publicKey) },
            Power = power
        };
}
