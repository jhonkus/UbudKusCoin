using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using GrpcService.Protos;
using static GrpcService.Protos.BChainService;

namespace DesktopWallet
{
    public class BlockchainClient
    {

        private readonly string serverAddress = "https://localhost:5002";
        private readonly GrpcChannel channel;
        private readonly BChainServiceClient bcservice;

        public BlockchainClient() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                AppContext.SetSwitch(
                    "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                serverAddress = "http://localhost:5002";
            }

            channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions
            {
                HttpHandler = new GrpcWebHandler(new HttpClientHandler())
            });

            bcservice = new BChainServiceClient(channel);

        }


        /// <summary>
        /// Getting all blocks from rpc
        /// </summary>
        /// <returns></returns>
        public async Task ShowAllBlocks()
        {
            try
            {
                // Execute rpc
                // Request is empty object so it is just created on the same line
                var response =  await bcservice.GetBlocksAsync(new EmptyRequest());

                if (response != null && response.Blocks != null && response.Blocks.Count > 0)
                {
                     // Console.WriteLine(block.ToString());
                     Helper.DoShowBlockchain(response.Blocks);
                }
                else
                {
                    Console.WriteLine("There are no blocks");
                }
            }
            catch (RpcException rpcException)
            {
                Console.WriteLine("There was an error communicating with gRPC server");
                Console.WriteLine($"Code: {rpcException.StatusCode}, Status: {rpcException.Status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        internal Task ShowGenesisBlock()
        {
            throw new NotImplementedException();
        }

        internal Task ShowLastBlock()
        {
            throw new NotImplementedException();
        }

        internal Task SendCoin()
        {
            throw new NotImplementedException();
        }

        internal Task ShowHistory()
        {
            throw new NotImplementedException();
        }

        internal Task CreateBlock()
        {
            throw new NotImplementedException();
        }

        internal Task ShowBalance()
        {
            throw new NotImplementedException();
        }

    }
}
