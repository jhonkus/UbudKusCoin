
namespace UbudKusCoin.P2P
{
    public class Peer
    {

        public P2PService p2pserver { set; get; }

        public Peer( P2PService p2pserver)
        {
            this.p2pserver = p2pserver;
        }
    }
}