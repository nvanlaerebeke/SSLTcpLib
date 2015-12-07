using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

using log4net;
using SSLTcpLib;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

[assembly: log4net.Config.XmlConfigurator(Watch=true)]

/**
 *
 * Make the server cert by running:
 *   makecert -r -pe -n “CN=SslServer” -ss my -sr currentuser -sky exchange server.cer
 * 
 * 
 */

namespace Server {
    public class Program {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(Program));
        List<SSLTcpClient> _lstClients = new List<SSLTcpClient>();

        static void Main(string[] args) {
            SSLTcpServer objServer = new SSLTcpServer(IPAddress.Any, 51510, @"server.pfx", "vandaag");
            objServer.clientConnected += objServer_clientConnected;
            objServer.clientDisconnected += objServer_clientDisconnected;
            objServer.Start();
            Console.ReadLine();
        }

        static void objServer_clientDisconnected(SSLTcpClient pClient) {
            pClient.disconnected -= objServer_clientDisconnected;
            Log.Debug("Client Disconnected");
        }

        static void objServer_clientConnected(SSLTcpClient pClient) {
            Log.Debug("Client Connected");

            //log the data we receive
            pClient.dataReceived += delegate(SSLTcpClient Client, byte[] pData) {
                Log.Debug(System.Text.Encoding.UTF8.GetString(pData));
            };

            var t = new Thread(() => SendData(pClient));
            t.Start();
        }

        private static void SendData(SSLTcpClient pClient) {
            //Start sending data
            bool result;
            do {
                result = pClient.Send("This is the server");
                System.Threading.Thread.Sleep(1000);
            } while (result);
            Log.Debug("Sending failed, stopping loop");
        }
    }
}
