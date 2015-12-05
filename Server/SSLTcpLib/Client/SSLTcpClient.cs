using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SSLTcpLib {
    public sealed partial class SSLTcpClient : IDisposable {
        /**
         * Private fields
         */
        private StreamReader _reader;
        private StreamWriter _writer;

        /**
         * Events
         */
        public ConnectionHandler connected;
        public ConnectionHandler disconnected;
        public DataTransfer dataReceived;

        /**
         * Constructors
         */
        public SSLTcpClient() { }
        public SSLTcpClient(TcpClient pClient) {
            init(pClient);
        }

        /**
         * Connect the TcpClient
         */
        public async void ConnectAsync(IPAddress pIP, int pPort) {
            TcpClient objClient = new TcpClient();
            await objClient.ConnectAsync(pIP, pPort);

            init(objClient);
        }

        /**
         * Initialization
         */
        private void init(TcpClient pClient) {
            NetworkStream stream = pClient.GetStream();

            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream);
            _writer.AutoFlush = true;

            Thread objThread = new Thread(new ThreadStart(RunListener));
            objThread.Start();

            if (connected != null) {
                connected(this);
            }
        }


        /**
         * Reading
         */
        private async void RunListener() {
            while (true) {
                var dataFromServer = await _reader.ReadLineAsync();
                if (!String.IsNullOrEmpty(dataFromServer) && dataReceived != null) {
                    dataReceived(this, dataFromServer);
                }
            }
        }

        /**
         * Writing 
         */
        public async void Send(string pData) {
            await _writer.WriteLineAsync(pData);
        }


        /**
         * Shutdown
         */
        public void Dispose() {
            _reader.Close();
            _writer.Close();
            if (disconnected != null) {
                disconnected(this);
            }
        }
    }
}
