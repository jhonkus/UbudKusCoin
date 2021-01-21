using NBitcoin;

namespace DesktopWallet {
    public class KeyPair {
        public ExtKey PrivateKey { set; get; }
        public ExtPubKey PublicKey { set; get; }
    }
}        