using System.Collections.Generic;
using System.Net;
using SSLTcpLib;
using log4net;
using System.Threading;
using System.Threading.Tasks;
using System;
using SSLTcpLib.Senders;
using SSLTcpLib.Client;
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
                TextSender objSender = new TextSender(objClient);
                //when connected, start sending strings
                objClient.connected += delegate(SSLTcpClient pClient) {
                    var t = new Thread(() => SendData(objSender, i.ToString()));
                    t.Start();
                };

                objClient.disconnected += delegate(SSLTcpClient pClient) {
                    Log.Debug("Client disconnected");
                };

                //when receiving data, log it
                objClient.dataReceived += delegate(byte[] pData) {
                    Log.Debug(System.Text.Encoding.UTF8.GetString(pData));
                };

                //connect the client
                try {
                    objClient.ConnectAsync(IPAddress.Parse("127.0.0.1"), 51510, @"nmua000001.der", "vandaag");
                } catch(Exception ex) {
                    Log.Debug("Connect failed");
                    Log.Error(ex);
                }
            }

            while (true) {
                System.Threading.Thread.Sleep(1000);
            }
        }

        private static void SendData(TextSender pSender, string pName) {
            try {
                do {
                    pSender.Send("This is client " + pName);
                    System.Threading.Thread.Sleep(1000);
                } while (true);
            } catch(Exception ex) {
                Log.Debug("Failed sending, stopping loop");
                Log.Debug(ex);
            }
        }
    }
}