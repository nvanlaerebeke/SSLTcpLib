using log4net;
using SSLTcpLib.Client;
using SSLTcpLib.Senders;
using System;
using System.Net;
using System.Threading;

namespace Client {
    class MultiClient {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(MultiClient));

        public static void Start() {
            int count = 1;

            Log.Debug(string.Format("Starting {0} clients...", count.ToString()));

            for (int i = 0; i < count; i++) {
                Log.Debug(string.Format("Starting client {0}", (i + 1).ToString()));

                SSLTcpClient objClient = new SSLTcpClient();
                TextSender objSender = new TextSender(objClient);
                //when connected, start sending strings
                objClient.connected += delegate (SSLTcpClient pClient) {
                    //var t = new Thread(() => SendData(objSender, i.ToString()));
                    //t.Start();
                };
                
                objClient.disconnected += delegate (SSLTcpClient pClient) {
                    Log.Debug("Client disconnected");
                };

                //when receiving data, log it
                objClient.dataReceived += delegate (SSLTcpClient pClient, byte[] pData) {
                    //Log.Debug(System.Text.Encoding.UTF8.GetString(pData));
                    Log.Debug("Data received");
                };

                //connect the client
                try {
                    objClient.ConnectAsync(IPAddress.Parse("127.0.0.1"), 51510, @"nmua000001.der", "vandaag");
                } catch (Exception ex) {
                    Log.Debug("Connect failed");
                    Log.Error(ex);
                }
            }
        }

        private static void SendData(TextSender pSender, string pName) {
            try {
                do {
                    pSender.Send("This is client " + pName);
                    System.Threading.Thread.Sleep(1000);
                } while (true);
            } catch (Exception ex) {
                Log.Debug("Failed sending, stopping loop");
                Log.Debug(ex);
            }
        }
    }
}
