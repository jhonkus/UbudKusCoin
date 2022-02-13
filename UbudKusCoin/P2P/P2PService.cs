// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;
using UbudKusCoin.Services;
using System.Threading;

namespace UbudKusCoin.P2P
{

    public class P2PService
    {


        IList<string> BlocksInTransit { set; get; }

        public IList<Peer> knownPeers { set; get; }

        public string nodeAddress { set; get; }

        private int nodePort { set; get; }

        public P2PService()
        {
            this.knownPeers = new List<Peer>();
        }

        public void Start()
        {
            Console.WriteLine("... P2P service is starting");
            this.nodeAddress = DotNetEnv.Env.GetString("NODE_ADDRESS");
            this.knownPeers = ServicePool.DbService.peerDb.GetAll().FindAll().ToList();
            Task.Run(() =>
            {
                this.StartP2PServer();

            });
        }

        public void StartP2PServer()
        {
            nodePort = GetPortFromAddress(nodeAddress);
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, nodePort);
            Console.WriteLine("...... starting on PORT {0} {1}", System.Net.IPAddress.Any, nodePort);

            // create listener
            Socket listener = null;
            try
            {
                IPAddress nodeIP = MakeIPLocal(nodeAddress);
                listener = new Socket(nodeIP.AddressFamily,
                         SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(localEndPoint);
                listener.Listen(100);
            }
            catch (Exception e)
            {
                listener.Close();
                Console.WriteLine(e.ToString());
            }

            ServicePool.StateService.IsP2PServiceReady = true;
            Console.WriteLine("...... Waiting connection");
            Console.WriteLine("... P2P service is ready");

            while (true)
            {

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
            var remoteAddress = payload;
            Console.WriteLine("==== HandleGetBlocks from {0}", remoteAddress);
            var blocks = ServicePool.DbService.blockDb.GetHashList();
            this.sendInv(remoteAddress, blocks);
        }

        private void sendInv(string remoteAddr, IList<string> items)
        {
            var inv = new Facade.Inventory
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
            //socket clientSocket = (Socket)obj;
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
            var remoteAddress = cmds[1];

            switch (command)
            {
                case Constants.MESSAGE_TYPE_STATE:
                    this.HandleNodeState(remoteAddress);
                    break;
                case Constants.MESSAGE_TYPE_INV:
                    this.HandleInventory(remoteAddress);
                    break;
                case Constants.MESSAGE_TYPE_GET_BLOCKS:
                    this.HandleGetBlocks(remoteAddress);
                    break;
                case Constants.MESSAGE_TYPE_TRANSACTION:
                    this.HandleTx(remoteAddress);
                    break;
                case Constants.MESSAGE_TYPE_BLOCK:
                    this.HandleBlock(remoteAddress);
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
            var inventory = JsonConvert.DeserializeObject<Facade.Inventory>(payload);
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

        private void HandleNodeState(string payload)
        {
            var remoteAddress = payload;
            var nodeState = JsonConvert.DeserializeObject<NodeState>(payload);
            Console.WriteLine("=== HandleNodeState, state: {0}", nodeState);

            // local block height
            var myBestHeight = ServicePool.DbService.blockDb.GetLast().Height;

            // remote block height
            var peerBestHeight = nodeState.Height;

            if (myBestHeight < peerBestHeight)
            {
                this.SendGetBlocks(remoteAddress);
            }

            else if (myBestHeight > peerBestHeight)
            {
                this.RequestNodeState(remoteAddress);
            }

            // if socket not in list
            if (!IsNewPeer(remoteAddress))
            {
                var newPeer = new Peer
                {
                    Address = remoteAddress,
                    IsBootstrap = false,
                    IsCanreach = true,
                    LastReach = Utils.GetTime(),
                    TimeStamp = Utils.GetTime()
                };

                this.knownPeers.Add(newPeer);
            }
        }

        private bool IsNewPeer(string address)
        {
            foreach (var peer in this.knownPeers)
            {
                if (address == peer.Address)
                {
                    return true;
                }
            }
            return false;
        }

        private void RequestNodeState(string remoteAddr)
        {
            var nodeState = ServicePool.FacadeService.Peer.GetNodeState();
            var payload = JsonConvert.SerializeObject(nodeState);
            // Console.WriteLine("=== payload {0}", payload);

            var msg = Constants.MESSAGE_TYPE_STATE + Constants.MESSAGE_SEPARATOR + payload;
            SendData(remoteAddr, msg);
        }

        private void SendBlock(string remoteAddr, Block block)
        {
            var payload = JsonConvert.SerializeObject(block);
            var msg = Constants.MESSAGE_TYPE_BLOCK + Constants.MESSAGE_SEPARATOR + payload;
            SendData(remoteAddr, msg);
        }

        public void SyncState()
        {

            foreach (var peer in this.knownPeers)
            {
                if (!nodeAddress.Equals(peer.Address))
                {
                    try
                    {
                        // send state to known peers
                        Console.WriteLine("...... Reequest state from Node: {0}", peer.Address);
                        RequestNodeState(peer.Address);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(" error when connecting: {0}", e.Message);
                    }
                }

                Thread.Sleep(30000); // give time to next peer
            }


        }

        private void BroadcastBlock(Block block)
        {
            Console.WriteLine("Will broadcasting block !");
            foreach (var peer in this.knownPeers)
            {
                if (!peer.Equals(nodeAddress))
                {
                    this.SendBlock(peer.Address, block);
                }
            }
        }

        public void BroadcastTransaction(Transaction transaction)
        {
            Console.WriteLine("Will broadcasting transaction, node Address: {0}", this.nodeAddress);
            foreach (var peer in this.knownPeers)
            {
                if (!peer.Equals(nodeAddress))
                {
                    this.SendTransaction(peer.Address, transaction);
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
                Socket socket = null;
                try
                {
                    IPEndPoint remoteEndPoint = MakeRemoteEndPoint(remoteAddr);
                    Console.WriteLine("==== 1 {0}", remoteEndPoint);
                    IPAddress nodeIP = MakeIPLocal(nodeAddress);
                    socket = new Socket(nodeIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(remoteEndPoint);

                    byte[] messageSent = Encoding.ASCII.GetBytes(msg + "<EOF>");
                    int byteSent = socket.Send(messageSent);
                }
                catch (Exception e)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
                finally
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }

            });
        }

        private int GetPortFromAddress(string nodeAddress)
        {
            string[] address = nodeAddress.Split(':');
            var nodePort = int.Parse(address[1]);
            return nodePort;
        }

        private IPAddress MakeIPLocal(string nodeAddress)
        {
            string[] nodeAddr = nodeAddress.Split(':');
            var nodeIP = nodeAddr[0];
            return IPAddress.Parse(nodeIP);
        }

        private IPEndPoint MakeRemoteEndPoint(string remotAddr)
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
