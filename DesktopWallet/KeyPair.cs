using NBitcoin;

namespace PKusCoin.blockchain {
    public class KeyPair {
        public ExtKey PrivateKey { set; get; }
        public ExtPubKey PublicKey { set; get; }
    }
}        