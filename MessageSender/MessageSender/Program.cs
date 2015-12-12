using SSLTcpLib;
using System;
using System.Net;

namespace MessageSender {
    class Program {
        static void Main(string[] args) {
            string ip = args[0];
            string port = args[1];
            string cert = args[2];
            string password = args[3];
            string message = args[4];
            
            StartClient(IPAddress.Parse(ip), Int32.Parse(port), cert, password, message);
            Console.ReadLine();
        }

        public static void StartClient(IPAddress pIP, int pPort, string pCertPath, string pCertPassword, string pMessage) { 
            SSLTcpClient objClient = new SSLTcpClient();
            objClient.connected += delegate(SSLTcpClient pClient) {
                bool success = pClient.Send(pMessage);
                pClient.Dispose();
                Console.WriteLine("Exiting");
                Environment.Exit((success) ? 0 : 1);
            };
            bool connected = objClient.ConnectAsync(pIP, pPort, pCertPath, pCertPassword);
            if (!connected) {
                Environment.Exit(1);
            }
        }
    }
}
