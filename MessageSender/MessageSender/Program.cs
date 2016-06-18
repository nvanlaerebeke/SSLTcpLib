using SSLTcpLib;
using System;
using System.Net;
using System.Threading.Tasks;

namespace MessageSender {
    class Program {
        static SSLTcpClient objClient;
        static bool _wait = true;

        static void Main(string[] args) {
            for (int i = 0; i < args.Length; i++) {
                args[i] = args[i].Trim();
            }
            string ip = args[0];
            string port = args[1];
            string cert = args[2];
            string password = args[3];
            string message = args[4];

            StartClient(IPAddress.Parse(ip), Int32.Parse(port), cert, password, message);

            //Exit after 5 seconds
            Task.Run(() => {
                System.Threading.Thread.Sleep(10000);
                _wait = false;
                Environment.Exit(1);
            });

            while (_wait) {
                System.Threading.Thread.Sleep(100);
            }
        }

        public static void StartClient(IPAddress pIP, int pPort, string pCertPath, string pCertPassword, string pMessage) {
            objClient = new SSLTcpClient();
            objClient.connected += delegate(SSLTcpClient pClient) {
                bool success = pClient.Send(pMessage);
                if (!success) {
                    pClient.Dispose();
                    Console.WriteLine("Sending message failed, exiting");
                    _wait = false;
                    Environment.Exit((success) ? 0 : 1);
                }
            };
            objClient.dataReceived += delegate(SSLTcpClient pClient, byte[] pData) {
                var strResponse = System.Text.Encoding.UTF8.GetString(pData); 
                Console.WriteLine(strResponse);
                _wait = false;
                Environment.Exit(0);
            };
            bool connected = objClient.ConnectAsync(pIP, pPort, pCertPath, pCertPassword);
            if (!connected) {
                Console.WriteLine("Connect failed, exiting");
                _wait = false;
                Environment.Exit(1);
            }
        }
    }
}
