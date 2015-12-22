using System.Collections.Generic;
using System.Net;
using SSLTcpLib;
using log4net;
using System.Threading;
using System.Threading.Tasks;
using System;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

/**
 *
 * Make the server cert by running:
 *   makecert -r -pe -n “CN=SslClient” -ss my -sr currentuser -sky exchange client.cer
 *
 * Or use openssl (nomadesk-ca)
 * 
 */

namespace Client {
    class Program {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args) {
            int count = 1;

            Log.Debug(string.Format("Starting {0} clients...", count.ToString()));

            for (int i = 0; i < count; i++) {
                Log.Debug(string.Format("Starting client {0}", (i + 1).ToString()));

                SSLTcpClient objClient = new SSLTcpClient();

                //when connected, start sending strings
                objClient.connected += delegate(SSLTcpClient pClient) {
                    var t = new Thread(() => SendData(pClient, i.ToString()));
                    t.Start();
                };

                objClient.disconnected += delegate(SSLTcpClient pClient) {
                    Log.Debug("Client disconnected");
                };

                //when receiving data, log it
                objClient.dataReceived += delegate(SSLTcpClient pClient, byte[] pData) {
                    Log.Debug(System.Text.Encoding.UTF8.GetString(pData));
                };

                //connect the client
                bool result = objClient.ConnectAsync(IPAddress.Parse("127.0.0.1"), 51510, @"nmua000001.der", "vandaag");
                if (!result) {
                    Log.Debug("Connect failed");
                }
            }

            while (true) {
                System.Threading.Thread.Sleep(1000);
            }
        }

        private static void SendData(SSLTcpClient pClient, string pName) {
            bool result;
            do {
                result = pClient.Send("This is client " + pName);
                System.Threading.Thread.Sleep(1000);
            } while (result);
            Log.Debug("Failed sending, stopping loop");
        }
    }
}