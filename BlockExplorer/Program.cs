using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using UbudKusCoin.Services;
using static UbudKusCoin.Services.BChainService;

namespace Main
{
    class Program
    {
        static void Main(string[] args)
        {
            // var serverAddress = "https://localhost:5002";
            // if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            // {
            //     AppContext.SetSwitch(
            //         "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            //     serverAddress = "http://localhost:5002";
            // }

            // GrpcChannel channel = GrpcChannel.ForAddress(serverAddress);

            //GrpcChannel channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions
            //{
            //    HttpHandler = new GrpcWebHandler(new HttpClientHandler())
            //});

                       
            var serverAddress = "http://localhost:5002";
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            GrpcChannel channel = GrpcChannel.ForAddress(serverAddress);
            BChainServiceClient bcservice = new BChainServiceClient(channel);
            _ = new ConsoleExplorer(bcservice);
        }
    }
}
