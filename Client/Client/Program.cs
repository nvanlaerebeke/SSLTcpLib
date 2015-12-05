using System.Collections.Generic;
using System.Net;
using SSLTcpLib;
using log4net;
using System.Threading;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace Client {
    class Program {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args) {
            int count = 5;
            
            Log.Debug(string.Format("Starting {0} clients...", count.ToString()));

            for (int i = 0; i < count; i++) {
                Log.Debug(string.Format("Starting client {0}", (i + 1).ToString()));

                SSLTcpClient objClient = new SSLTcpClient();

                //when connected, start sending strings
                objClient.connected += delegate(SSLTcpClient pClient) {
                    var t = new Thread(() => SendData(pClient, i.ToString()));
                    t.Start();
                };

                //when receiving data, log it
                objClient.dataReceived += delegate(SSLTcpClient pClient, string pData) {
                    Log.Debug(pData);
                };

                //connect the client
                objClient.ConnectAsync(IPAddress.Parse("127.0.0.1"), 51510);
            }

            while (true) { }
        }

        private static void SendData(SSLTcpClient pClient, string pName) {
            while (true) {
                System.Threading.Thread.Sleep(1000);
                pClient.Send("This is client " + pName);
            }
        }
    }
}