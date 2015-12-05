using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

using log4net;
using SSLTcpLib;
using System.Collections.Generic;

[assembly: log4net.Config.XmlConfigurator(Watch=true)]

namespace Server {
    public class Program {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(Program));
        List<SSLTcpClient> _lstClients = new List<SSLTcpClient>();

        static void Main(string[] args) {
            SSLTcpServer objServer = new SSLTcpServer(IPAddress.Any, 51510);
            objServer.clientConnected += objServer_clientConnected;
            objServer.clientDisconnected += objServer_clientDisconnected;
            objServer.Start();
            Console.ReadLine();
        }

        static void objServer_clientDisconnected(SSLTcpClient pClient) {
            Log.Debug("Client Disconnected");
        }


        static void objServer_clientConnected(SSLTcpClient pClient) {
            Log.Debug("Client Connected");
            
            //log the data we receive
            pClient.dataReceived += delegate(SSLTcpClient Client, string pData) {
                Log.Debug(pData);
            };

            var t = new Thread(() => SendData(pClient));
            t.Start();
        }


        private static void SendData(SSLTcpClient pClient) {
            //Start sending data
            while (true) {
                System.Threading.Thread.Sleep(1000);
                pClient.Send("This is the server");
            }
        }
    }
}
