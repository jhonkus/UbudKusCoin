using NBitcoin;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;
using CoreMoney = UbudKusCoin.Core.Types.Money;
using CoreTransaction = UbudKusCoin.Core.Types.Transaction;

var privateKey = new byte[32];
privateKey[^1] = 1;
var key = new Key(privateKey);
var publicKey = key.PubKey.ToBytes();
var validatorKeyHex = Environment.GetEnvironmentVariable("VALIDATOR_PUBKEY_HEX");
if (string.IsNullOrWhiteSpace(validatorKeyHex))
    throw new InvalidOperationException("VALIDATOR_PUBKEY_HEX must contain the node's Ed25519 consensus key.");

var validatorPublicKey = Convert.FromHexString(validatorKeyHex);
if (validatorPublicKey.Length != 32)
    throw new InvalidOperationException("VALIDATOR_PUBKEY_HEX must decode to exactly 32 bytes.");

var address = Address.FromPublicKey(Address.TestnetVersion, publicKey);
var transaction = new CoreTransaction
{
    ChainId = ChainInfo.ChainIdTestnet,
    Kind = TransactionKind.Bond,
    Nonce = 1,
    From = address,
    To = address,
    Amount = CoreMoney.FromCoins(1m),
    Fee = FeePolicy.MinRelayFee,
    PubKey = publicKey,
    ValidatorPubKey = validatorPublicKey
};
transaction.Signature = TransactionSigner.Sign(transaction, privateKey);

if (!transaction.IsEnvelopeWellFormed(ChainInfo.ChainIdTestnet)
    || !transaction.VerifySignature())
{
    throw new InvalidOperationException("Generated staking transaction failed local validation.");
}

Console.WriteLine(Convert.ToHexString(TransactionCodec.Encode(transaction)));
