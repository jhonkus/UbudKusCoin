using System.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UbudKusCoin.Grpc;
using UbudKusCoin.Models;
using UbudKusCoin.Others;
using UbudKusCoin.Services;

namespace UbudKusCoin.P2P
{

    public class P2PService
    {

        IList<string> BlocksInTransit { set; get; }

        IList<string> sockets = new List<string>{
            "127.0.0.1:7170"
            // "127.0.0.1:7178",
            // "127.0.0.1:7177",
            // "127.0.0.1:7176",
            // "127.0.0.1:7175"
        };
        IList<string> peers { set; get; }
        private string nodeAddress { set; get; }

        private int nodePort { set; get; }

        public P2PService(string nodeAddress)
        {
            this.nodeAddress = nodeAddress; // config.Config.NodeAddress;
        }

        public void Start()
        {
            Task.Run(() =>
            {
                this.BroadcastVersion();
                this.StartNode();
            });
        }

        public void StartNode()
        {

            nodePort = getPortFromAddress(nodeAddress);
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, nodePort);
            //  Console.WriteLine("===== server starting on {0} {1}", System.Net.IPAddress.Any, nodePort);


            // create listener
            Socket listener = null;
            try
            {

                IPAddress senderIP = makeIPLocal(nodeAddress);
                listener = new Socket(senderIP.AddressFamily,
                         SocketType.Stream, ProtocolType.Tcp);

                listener.Bind(localEndPoint);
                listener.Listen(100);
            }
            catch (Exception e)
            {
                listener.Close();
                Console.WriteLine(e.ToString());
            }
            // ned off

            while (true)
            {

                Console.WriteLine("Waiting connection ... ");

                Socket client = null;

                try
                {
                    client = listener.Accept();
                    Console.WriteLine("== Accept connection from: {0}", client.RemoteEndPoint);
                    HandleConnection(client);

                    //client.Shutdown(SocketShutdown.Both);
                    client.Close();

                }
                catch (Exception e)
                {
                    client.Close();
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public void HandleBlock(string payload)
        {
            var block = JsonConvert.DeserializeObject<Block>(payload);

            Console.WriteLine("==== handleBlock {0}", payload + "||" + DateTime.Now.ToString("dd:mm:yy HH:mm:ss tt"));

            if (ServicePool.FacadeService.Block.isValidBlock(block))
            {
                ServicePool.DbService.blockDb.Add(block);

                //ServicePool.FacadeService.Transaction.UpdateBalance(block.Transactions);

                // move pool to to transactions db
                //ServicePool.FacadeService.Transaction.AddBulk(block.Transactions);

                // clear mempool
                ServicePool.DbService.transactionsPooldb.DeleteAll();

                //triger event block created
                ServicePool.EventService.OnEventBlockCreated(block);

                //broadcat block again
                // this.BroadcastBlock(block);

                // TODO    
                // this.transactionPool.Clear();
            }

        }


        public void HandleGetBlocks(string payload)
        {
            var sourceAddress = payload;
            Console.WriteLine("==== HandleGetBlocks from {0}", sourceAddress);
            var blocks = ServicePool.DbService.blockDb.GetHashList();
            this.sendInv(sourceAddress, blocks);
        }

        private void sendInv(string remoteAddr, IList<string> items)
        {
            var inv = new Inventory
            {
                AddrFrom = this.nodeAddress,
                Type = "block",
                Items = items
            };

            var payload = JsonConvert.SerializeObject(inv);
            var msg = Constants.MESSAGE_TYPE_INV + Constants.MESSAGE_SEPARATOR + payload;
            this.SendData(remoteAddr, msg);
        }


        public void HandleTx(string payload)
        {
            //1. convert string to Transaction obj
            var trx = JsonConvert.DeserializeObject<Transaction>(payload);
            Console.WriteLine(" on handle tx, Transaction receive: {0}", JsonConvert.SerializeObject(trx));

            //2. check if Transaction exist in transaction pool
            var isExist = ServicePool.FacadeService.TransactionPool.IsTransactionExist(trx);
            Console.WriteLine("== transaction exist: {0}", isExist);

            if (!isExist)
            {
                ServicePool.DbService.transactionsPooldb.Add(trx);
            }

        }


        public void HandleConnection(Socket clientSocket)
        {
            //ocket clientSocket = (Socket)obj;
            byte[] bytes = new Byte[256];
            StringBuilder sbf = new StringBuilder();
            while (true)
            {

                int numByte = clientSocket.Receive(bytes);
                var data = Encoding.ASCII.GetString(bytes, 0, numByte);
                sbf.Append(data);

                if (data.IndexOf("<EOF>", StringComparison.Ordinal) > -1)
                {
                    break;
                }
            }

            var dataReceived = sbf.ToString().Replace("<EOF>", "");
            Console.WriteLine("Received: {0}, on timestamp: {1}", dataReceived, DateTime.Now);

            String[] cmds = dataReceived.Split(Constants.MESSAGE_SEPARATOR);
            var command = cmds[0];
            var payload = cmds[1];

            switch (command)
            {
                case Constants.MESSAGE_TYPE_VERSION:
                    this.HandleVersion(payload);
                    break;
                case Constants.MESSAGE_TYPE_INV:
                    this.HandleInventory(payload);
                    break;
                case Constants.MESSAGE_TYPE_GET_BLOCKS:
                    this.HandleGetBlocks(payload);
                    break;
                case Constants.MESSAGE_TYPE_TRANSACTION:
                    this.HandleTx(payload);
                    break;
                case Constants.MESSAGE_TYPE_BLOCK:
                    this.HandleBlock(payload);
                    break;
                default:
                    Console.WriteLine("Unknown command!");
                    break;
            }
        }
        private void SendGetData(string remoteAddr, string type, string id)
        {
            var data = new GetData
            {
                AddrFrom = this.nodeAddress,
                Type = type,
                ID = id
            };
            var payload = JsonConvert.SerializeObject(data);
            var msg = Constants.MESSAGE_TYPE_GET_DATA + Constants.MESSAGE_SEPARATOR + payload;
            SendData(remoteAddr, msg);
        }

        private void HandleInventory(string payload)
        {
            var inventory = JsonConvert.DeserializeObject<Inventory>(payload);
            Console.WriteLine("Recevied inventory with {0} {0}", inventory.Items.Count, inventory.Type);

            if (inventory.Type == "block")
            {
                BlocksInTransit = inventory.Items;
                var blockHash = inventory.Items[0];
                this.SendGetData(inventory.AddrFrom, "block", blockHash);

                IList<string> newInTransit = new List<string>();

                foreach (var hash in BlocksInTransit)
                {
                    if (!hash.Equals(blockHash))
                    {
                        newInTransit.Append(hash);
                    }
                }

                BlocksInTransit = newInTransit;
            }

        }

        private void SendGetBlocks(string remoteAddr)
        {
            var payload = this.nodeAddress;
            var msg = Constants.MESSAGE_TYPE_GET_BLOCKS + Constants.MESSAGE_SEPARATOR + payload;
            SendData(remoteAddr, msg);
        }

        private void HandleVersion(string payload)
        {
            var verzion = JsonConvert.DeserializeObject<Verzion>(payload);
            Console.WriteLine("=== HandleVersion, version: {0}", verzion);

            // local block height
            var myBestHeight = ServicePool.DbService.blockDb.GetLast().Height;

            // remote block height
            var peerBestHeight = verzion.BestHeight;

            if (myBestHeight < peerBestHeight)
            {
                this.SendGetBlocks(verzion.AddrFrom);
            }

            else if (myBestHeight > peerBestHeight)
            {
                this.SendVersion(verzion.AddrFrom);
            }

            // if socket not in list
            if (!socketIsKnown(verzion.AddrFrom))
            {
                this.sockets.Add(verzion.AddrFrom);
            }
        }

        private bool socketIsKnown(string address)
        {
            foreach (string item in this.sockets)
            {
                if (address == item)
                {
                    return true;
                }
            }
            return false;
        }

        private Verzion getVersion()
        {

            var bestHeight = ServicePool.DbService.blockDb.GetLast().Height;
            var payload = new Verzion
            {
                AddrFrom = this.nodeAddress,
                Version = Constants.VERZION,
                BestHeight = bestHeight,
            };
            return payload;
        }


        private void SendVersion(string remoteAddr)
        {
            var version = getVersion();
            var payload = JsonConvert.SerializeObject(version);
            var msg = Constants.MESSAGE_TYPE_VERSION + Constants.MESSAGE_SEPARATOR + payload;
            SendData(remoteAddr, msg);
        }


        private void SendBlock(string remoteAddr, Block block)
        {
            var payload = JsonConvert.SerializeObject(block);
            var msg = Constants.MESSAGE_TYPE_BLOCK + Constants.MESSAGE_SEPARATOR + payload;
            SendData(remoteAddr, msg);
        }

        private void BroadcastVersion()
        {
            var centralNode = this.sockets[0];
            Console.WriteLine("== Will send version to Central Node: {0}", centralNode);
            // foreach (var socket in this.sockets)
            // {
            //     if (!socket.Equals(nodeAddress))
            //     {
            //         this.SendVersion(socket);
            //     }
            // }
            this.SendVersion(centralNode);
        }

        private void BroadcastBlock(Block block)
        {
            Console.WriteLine("Will broadcasting block !");
            foreach (var socket in this.sockets)
            {
                if (!socket.Equals(nodeAddress))
                {
                    this.SendBlock(socket, block);
                }
            }
        }

        public void BroadcastTransaction(Transaction transaction)
        {
            Console.WriteLine("Will broadcasting transaction, node Address: {0}", this.nodeAddress);
            foreach (var socket in this.sockets)
            {
                if (!socket.Equals(nodeAddress))
                {
                    this.SendTransaction(socket, transaction);
                }
            }
        }

        public void SendTransaction(string remoteAddr, Transaction trx)
        {
            string payload = JsonConvert.SerializeObject(trx);
            var msg = Constants.MESSAGE_TYPE_TRANSACTION + Constants.MESSAGE_SEPARATOR + payload;
            Console.WriteLine("Transaction will send msg: {0}", msg);
            SendData(remoteAddr, msg);
        }

        private void SendData(string remoteAddr, string msg)
        {

            Console.WriteLine("=== SendData, msg:", msg);

            Task.Run(() =>
            {
                Socket sender = null;
                try
                {
                    IPEndPoint remoteEndPoint = makeRemoteEndPoint(remoteAddr);
                    Console.WriteLine("==== 1 {0}", remoteEndPoint);
                    IPAddress senderIP = makeIPLocal(nodeAddress);
                    sender = new Socket(senderIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    sender.Connect(remoteEndPoint);

                    byte[] messageSent = Encoding.ASCII.GetBytes(msg + "<EOF>");
                    int byteSent = sender.Send(messageSent);
                }
                catch (Exception e)
                {
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
                finally
                {
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }

            });
        }

        private int getPortFromAddress(string nodeAddress)
        {
            string[] address = nodeAddress.Split(':');
            var nodePort = int.Parse(address[1]);
            return nodePort;
        }

        private IPAddress makeIPLocal(string nodeAddress)
        {
            string[] nodeAddr = nodeAddress.Split(':');
            var nodeIP = nodeAddr[0];
            return IPAddress.Parse(nodeIP);
        }

        private IPEndPoint makeRemoteEndPoint(string remotAddr)
        {
            string[] remoteAddress = remotAddr.Split(':');
            var remoteIP = remoteAddress[0];
            var remotePort = int.Parse(remoteAddress[1]);
            // Console.WriteLine("=== Remote iP detecte: {0}", remoteIP);
            // Console.WriteLine("==== remote poort: {0}", remotePort);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);
            return remoteEndPoint;
        }


    }



}
