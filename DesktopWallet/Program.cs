using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using static GrpcService.BChainService;

namespace DesktopWallet
{
    class Program
    {
        static async Task Main(string[] args)
        {

            var serverAddress = "https://localhost:5002";
            GrpcChannel channel;
            BChainServiceClient bcservice;


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



            // DbAccess.Initialize();
            // show wallet

            _ = new ConsoleWallet(bcservice);
        }

    }


}
