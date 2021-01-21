using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using GrpcService;
using static GrpcService.BChainService;

namespace DesktopWallet
{
    public class GrpcClient
    {

        private readonly string serverAddress = "https://51.15.211.115:8080";
        private readonly GrpcChannel channel;
        private readonly BChainServiceClient bcservice;

        public GrpcClient() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                AppContext.SetSwitch(
                    "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                serverAddress = "http://51.15.211.115:8080";
            }

            channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions
            {
                HttpHandler = new GrpcWebHandler(new HttpClientHandler())
            });

            bcservice = new BChainServiceClient(channel);

        }

       

        internal async Task GetBalance(string address)
        {
            try
            {
                var balance = await bcservice.GetBalanceAsync(new AccountRequest {
                    Address = address
                });
              
             }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }
}
