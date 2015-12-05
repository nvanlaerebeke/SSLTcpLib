using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class AsyncEchoServer
    {

        Dictionary<string, TcpClient> _dicClients;

        private int _listeningPort;
        public AsyncEchoServer(int port)
        {
            _listeningPort = port;
        }
        ///<summary>
        /// Start listening for connection
        /// </summary>
        public async void Start()
        {
            _dicClients = new Dictionary<string, TcpClient>();
            IPAddress ipAddre = IPAddress.Loopback;
            TcpListener listener = new TcpListener(ipAddre, _listeningPort);
            listener.Start();
            LogMessage("Server is running");
            LogMessage("Listening on port " + _listeningPort);

            while (true)
            {
                LogMessage("Waiting for connections...");
                try
                {
                    var tcpClient = await listener.AcceptTcpClientAsync();
                    HandleConnectionAsync(tcpClient);
                }
                catch (Exception exp)
                {
                    LogMessage(exp.ToString());
                }

            }

        }
        ///<summary>
        /// Process Individual client
        /// </summary>
        ///
        ///
        private async void HandleConnectionAsync(TcpClient tcpClient)
        {
            string strName = "";
            string clientInfo = tcpClient.Client.RemoteEndPoint.ToString();
            LogMessage(string.Format("Got connection request from {0}", clientInfo));
            try
            {
                using (var networkStream = tcpClient.GetStream())
                using (var reader = new StreamReader(networkStream))
                using (var writer = new StreamWriter(networkStream))
                {
                    writer.AutoFlush = true;
                    while (true)
                    {
                        strName = await reader.ReadLineAsync();
                        if (String.IsNullOrEmpty(strName)) { break; }

                        _dicClients.Add(strName, tcpClient);
                        LogMessage(strName + " Connected");
                        await writer.WriteLineAsync(strName + ": OK");
                    }
                }
            }
            catch (Exception exp)
            {
                _dicClients.Remove(strName);
                LogMessage(exp.Message);
            }
            finally
            {
                //LogMessage(string.Format("Closing the client connection - {0}", clientInfo));
                //tcpClient.Close();
            }
        }

        private void LogMessage(string message, [CallerMemberName]string callername = "")
        {
            System.Console.WriteLine("[{0}] - Thread-{1}- {2}", callername, Thread.CurrentThread.ManagedThreadId, message);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("GetEnvironmentVariables: ");
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                Console.WriteLine("  {0} = {1}", de.Key, de.Value);
            }

            AsyncEchoServer async = new AsyncEchoServer(51510);
            async.Start();
            Console.ReadLine();
        }
    }
}