using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using grpcservice.Protos;


namespace DesktopWallet
{
    class Program
    {
        static void Main(string[] args)
        {
            var serverAddress = "https://localhost:5002";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {

                // The following statement allows you to call insecure services. To be used only in development environments.
                AppContext.SetSwitch(
                    "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                serverAddress = "http://localhost:5002";
            }
            //var channel = GrpcChannel.ForAddress(serverAddress);


            var channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions
            {
                HttpHandler = new GrpcWebHandler(new HttpClientHandler())
            });


            // show menu
            Menu.DisplayMenu(channel);
        }

    }


}
