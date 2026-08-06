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

var kindName = Environment.GetEnvironmentVariable("TRANSACTION_KIND") ?? "Bond";
if (!Enum.TryParse<TransactionKind>(kindName, ignoreCase: true, out var kind)
    || kind is not (TransactionKind.Bond or TransactionKind.RotateValidatorKey))
{
    throw new InvalidOperationException("TRANSACTION_KIND must be Bond or RotateValidatorKey.");
}

var address = Address.FromPublicKey(Address.TestnetVersion, publicKey);
var transaction = new CoreTransaction
{
    ChainId = ChainInfo.ChainIdTestnet,
    Kind = kind,
    Nonce = kind == TransactionKind.Bond ? 1UL : 2UL,
    From = address,
    To = address,
    // The integration drill uses one base unit so a newly rotated key keeps
    // the three remaining genesis validators above CometBFT's quorum.
    Amount = kind == TransactionKind.Bond ? new CoreMoney(1) : CoreMoney.Zero,
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
