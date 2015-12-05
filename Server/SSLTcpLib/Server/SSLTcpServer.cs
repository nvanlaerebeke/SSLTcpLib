using System;
using System.Net;
using System.Net.Sockets;
using log4net;

namespace SSLTcpLib {
    public delegate void ConnectionHandler(SSLTcpClient pClient);
    public delegate void DataTransfer(SSLTcpClient pClient, string pData);
    
    public class SSLTcpServer : Base {
        private IPAddress IPAddress { get; set; }
        private int Port { get; set; }

        public event ConnectionHandler clientConnected;
        public event ConnectionHandler clientDisconnected;

        public SSLTcpServer(IPAddress pIPAddress, int pPort) {
            IPAddress = pIPAddress;
            Port = pPort;
        }

        public async void Start() {
            Log.Debug("Server is starting...");
            TcpListener listener = new TcpListener(IPAddress, Port);
            listener.Start();
            
            Log.Debug(String.Format("Listening on IP {0}:{1}", IPAddress.ToString(), Port));
            
            while (true) {
                try {
                    var tcpClient = await listener.AcceptTcpClientAsync();
                    HandleConnection(tcpClient);
                } catch (Exception exp) {
                    Log.Debug(exp.ToString());
                }

            }

        }

        private void HandleConnection(TcpClient pTcpClient) {
            if (clientConnected != null) {
                SSLTcpClient objClient = new SSLTcpClient(pTcpClient);
                objClient.disconnected += objClient_disconnected;
                clientConnected(objClient);
            }
        }

        void objClient_disconnected(SSLTcpClient pClient) {
            if (clientDisconnected != null) {
                clientDisconnected(pClient);
            }
        }
    }
}
