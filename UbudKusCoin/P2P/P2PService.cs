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
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;
using UbudKusCoin.Services;
using Grpc.Net.Client;
using static UbudKusCoin.Grpc.PeerService;
using static UbudKusCoin.Grpc.BlockService;

namespace UbudKusCoin.P2P
{


    public class GetData
    {
        public string Type { set; get; }
        public string ID { set; get; }
    }

    public class P2PService
    {


        IList<string> BlocksInTransit { set; get; }

        public string nodeAddress { set; get; }

        private int nodePort { set; get; }

        public P2PService()
        {
            this.nodeAddress = DotNetEnv.Env.GetString("NODE_ADDRESS");
        }

        public void Start()
        {
            Console.WriteLine("... P2P service is starting");

            ListenEvent();

            // Task.Run(() =>
            // {
            //     this.StartP2PServer();

            // });
            ServicePool.StateService.IsP2PServiceReady = true;
        }

        private void ListenEvent()
        {
            ServicePool.EventService.EventBlockCreated += Evt_EventBlockCreated;
            ServicePool.EventService.EventTransactionCreated += Evt_EventTransactionCreated;
        }

        void Evt_EventBlockCreated(object sender, Block block)
        {
            BroadcastBlock(block);
        }

        private void BroadcastBlock(Block block)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            Console.WriteLine("Will broadcasting block ! {0}", knownPeers);
            foreach (var peer in knownPeers)
            {
                Console.WriteLine("Will broadcasting block ! {0}", peer);
                if (!peer.Equals(nodeAddress))
                {
                    this.SendBlock(peer, block);
                }
            }
        }


        void Evt_EventTransactionCreated(object sender, Transaction txn)
        {

        }


        // public void StartP2PServer()
        // {
        //     nodePort = GetPortFromAddress(nodeAddress);
        //     IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, nodePort);
        //     Console.WriteLine("...... starting on PORT {0} {1}", System.Net.IPAddress.Any, nodePort);

        //     // create listener
        //     Socket listener = null;
        //     try
        //     {
        //         IPAddress nodeIP = MakeIPLocal(nodeAddress);
        //         listener = new Socket(nodeIP.AddressFamily,
        //                  SocketType.Stream, ProtocolType.Tcp);
        //         listener.Bind(localEndPoint);
        //         listener.Listen(100);
        //     }
        //     catch (Exception e)
        //     {
        //         listener.Close();
        //         Console.WriteLine(e.ToString());
        //     }

        //     ServicePool.StateService.IsP2PServiceReady = true;
        //     Console.WriteLine("...... Waiting connection");
        //     Console.WriteLine("... P2P service is ready");

        //     while (true)
        //     {

        //         Socket client = null;

        //         try
        //         {
        //             client = listener.Accept();
        //             Console.WriteLine("== Accept connection from: {0}", client.RemoteEndPoint);
        //             HandleConnection(client);

        //             //client.Shutdown(SocketShutdown.Both);
        //             client.Close();

        //         }
        //         catch (Exception e)
        //         {
        //             client.Close();
        //             Console.WriteLine(e.ToString());
        //         }
        //     }
        // }

        // public void HandleConnection(Socket clientSocket)
        // {
        //     //socket clientSocket = (Socket)obj;
        //     byte[] bytes = new Byte[256];
        //     StringBuilder sbf = new StringBuilder();
        //     while (true)
        //     {

        //         int numByte = clientSocket.Receive(bytes);
        //         var data = Encoding.ASCII.GetString(bytes, 0, numByte);
        //         sbf.Append(data);

        //         if (data.IndexOf("<EOF>", StringComparison.Ordinal) > -1)
        //         {
        //             break;
        //         }
        //     }

        //     var dataReceived = sbf.ToString().Replace("<EOF>", "");
        //     Console.WriteLine("Received: {0}, on timestamp: {1}", dataReceived, DateTime.Now);

        //     String[] msgs = dataReceived.Split(Constants.MESSAGE_SEPARATOR);
        //     var command = msgs[0];
        //     var paylod = msgs[1];
        //     var clientIP = msgs[2];


        //     switch (command)
        //     {
        //         case Constants.MESSAGE_TYPE_STATE:
        //             this.HandleNodeState(paylod, clientSocket, clientIP);
        //             break;
        //         case Constants.MESSAGE_TYPE_INV:
        //             this.HandleInventory(paylod, clientSocket, clientIP);
        //             break;
        //         case Constants.MESSAGE_TYPE_GET_BLOCKS:
        //             this.HandleGetBlocks(paylod, clientSocket, clientIP);
        //             break;
        //         case Constants.MESSAGE_TYPE_TRANSACTION:
        //             this.HandleTx(paylod, clientSocket, clientIP);
        //             break;
        //         case Constants.MESSAGE_TYPE_BLOCK:
        //             this.HandleBlock(paylod, clientSocket, clientIP);
        //             break;
        //         default:
        //             Console.WriteLine("Unknown command!");
        //             break;
        //     }
        // }
        // public void HandleBlock(string payload, Socket cleintsocket, string clientIP)
        // {
        //     var block = JsonConvert.DeserializeObject<Block>(payload);

        //     Console.WriteLine("==== handleBlock {0}", payload + "||" + DateTime.Now.ToString("dd:mm:yy HH:mm:ss tt"));

        //     if (ServicePool.FacadeService.Block.isValidBlock(block))
        //     {
        //         ServicePool.DbService.blockDb.Add(block);

        //         //ServicePool.FacadeService.Transaction.UpdateBalance(block.Transactions);

        //         // move pool to to transactions db
        //         //ServicePool.FacadeService.Transaction.AddBulk(block.Transactions);

        //         // clear mempool
        //         ServicePool.DbService.transactionsPooldb.DeleteAll();

        //         //triger event block created
        //         ServicePool.EventService.OnEventBlockCreated(block);

        //         //broadcat block again
        //         // this.BroadcastBlock(block);

        //         // TODO    
        //         // this.transactionPool.Clear();
        //     }

        // }


        // public void HandleGetBlocks(string payload, Socket clientSocket, string clientIP)
        // {
        //     Console.WriteLine("==== HandleGetBlocks from {0}", clientIP);
        //     var blocks = ServicePool.DbService.blockDb.GetHashList();
        //     this.sendInv(clientIP, blocks);
        // }

        private void sendInv(string peerIP, IList<string> items)
        {
            var inv = new Facade.Inventory
            {
                Type = "block",
                Items = items
            };

            var payload = JsonConvert.SerializeObject(inv);
            var msg = Constants.MESSAGE_TYPE_INV + Constants.MESSAGE_SEPARATOR + payload + Constants.MESSAGE_SEPARATOR + peerIP;
            this.SendRequest(peerIP, msg);
        }


        public void HandleTx(string payload, Socket clientSocket, string clientIP)
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

        private void SendGetData(string peerIP, string type, string id)
        {
            var data = new GetData
            {
                Type = type,
                ID = id
            };
            var payload = JsonConvert.SerializeObject(data);
            var msg = Constants.MESSAGE_TYPE_GET_DATA + Constants.MESSAGE_SEPARATOR + payload + Constants.MESSAGE_SEPARATOR + peerIP;
            SendRequest(peerIP, msg);
        }

        private void HandleInventory(string payload, Socket clientSocket, string clientIP)
        {
            var inventory = JsonConvert.DeserializeObject<Facade.Inventory>(payload);
            Console.WriteLine("Recevied inventory with {0} {0}", inventory.Items.Count, inventory.Type);

            if (inventory.Type == "block")
            {
                BlocksInTransit = inventory.Items;
                var blockHash = inventory.Items[0];
                this.SendGetData(clientIP, "block", blockHash);

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


        private void DownloadBlocks(string address, long lastBlockHeight, long peerHeight)
        {


            GrpcChannel channel = GrpcChannel.ForAddress("http://" + address);
            var blockService = new BlockServiceClient(channel);
            var response = blockService.GetRemains(new StartingParam { Height = lastBlockHeight });
            List<Block> blocks = response.Blocks.ToList();
            blocks.Reverse();
            var lastHeight = lastBlockHeight;
            Console.WriteLine("=== Downloading Block , from: {0}, to {1}", lastBlockHeight, lastBlockHeight + 50);
            foreach (var block in blocks)
            {
                Console.WriteLine("==== Block" + block.Height);
                // TODO, VALIDATE BLOCK
                ServicePool.DbService.blockDb.Add(block);
                lastHeight = block.Height;
            }

            if (lastHeight < peerHeight)
            {
                DownloadBlocks(address, lastHeight, peerHeight);
            }

        }

        private bool IsNewPeer(string address)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            foreach (var peer in knownPeers)
            {
                if (address == peer.Address)
                {
                    return true;
                }
            }
            return false;
        }

        private void SendGetBlocks(string peerIP)
        {
            var payload = this.nodeAddress;
            var msg = Constants.MESSAGE_TYPE_GET_BLOCKS + Constants.MESSAGE_SEPARATOR + payload;
            SendRequest(peerIP, msg);
        }

        private void RequestState(string peerIP)
        {
            var localState = ServicePool.FacadeService.Peer.GetNodeState();
            var json = JsonConvert.SerializeObject(localState);
            var payload = Constants.MESSAGE_TYPE_STATE + Constants.MESSAGE_SEPARATOR + json;
            SendRequest(peerIP, payload);
        }

        private void SendBlock(Peer peer, Block block)
        {
            GrpcChannel channel = GrpcChannel.ForAddress("http://" + peer.Address);
            var blockService = new BlockServiceClient(channel);
            var status = blockService.Add(block);
            //TODO WITH status
        }

        public void SyncState()
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            Console.WriteLine("...... Will download block");
            foreach (var peer in knownPeers)
            {
                if (!nodeAddress.Equals(peer.Address))
                {

                    try
                    {
                        GrpcChannel channel = GrpcChannel.ForAddress("http://" + peer.Address);
                        var peerService = new PeerServiceClient(channel);
                        var peerState = peerService.GetNodeState(new NodeParam { NodeIpAddress = nodeAddress });

                        // local block height
                        var lastBlockHeight = ServicePool.DbService.blockDb.GetLast().Height;


                        if (lastBlockHeight < peerState.Height)
                        {
                            DownloadBlocks(peer.Address, lastBlockHeight, peerState.Height);
                        }

                        // checking known peers
                        foreach (var newPeer in peerState.KnownPeers)
                        {
                            if (!IsNewPeer(newPeer.Address))
                            {
                                ServicePool.FacadeService.Peer.Add(newPeer);
                            }

                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(" error when connecting: {0}", e.Message);
                    }
                }

                Thread.Sleep(10000); // give time to next peer
            }


        }



        public void BroadcastTransaction(Transaction transaction)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            Console.WriteLine("Will broadcasting transaction, node Address: {0}", nodeAddress);
            foreach (var peer in knownPeers)
            {
                if (!peer.Equals(nodeAddress))
                {
                    this.SendTransaction(peer.Address, transaction);
                }
            }
        }

        public void SendTransaction(string peerIP, Transaction trx)
        {
            string txJson = JsonConvert.SerializeObject(trx);
            var payload = Constants.MESSAGE_TYPE_TRANSACTION + Constants.MESSAGE_SEPARATOR + txJson;
            Console.WriteLine("Transaction will send msg: {0}", payload);
            SendRequest(peerIP, payload);
        }

        private void SendRequest(string peerIp, string msg)
        {

            Console.WriteLine("=== SendData, msg:", msg);

            Task.Run(() =>
            {
                Socket socket = null;
                try
                {
                    IPEndPoint peerEndPoint = MakeRemoteEndPoint(peerIp);
                    Console.WriteLine("==== 1 {0}", peerEndPoint);
                    IPAddress nodeIP = MakeIPLocal(nodeAddress);
                    socket = new Socket(nodeIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(peerEndPoint);

                    byte[] message = Encoding.ASCII.GetBytes(msg + Constants.MESSAGE_SEPARATOR + nodeAddress + "<EOF>");
                    int byteSent = socket.Send(message);
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
