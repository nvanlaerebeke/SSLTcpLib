using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace SSLTcpLib {
    public delegate void ConnectionHandler(SSLTcpClient pClient);
    public delegate void DataTransfer(SSLTcpClient pClient, byte[] pData);
    
    public class SSLTcpServer : Base {
        private X509Certificate2 serverCertificate = null;

        private IPAddress IPAddress { get; set; }
        private int Port { get; set; }

        public event ConnectionHandler clientConnected;
        public event ConnectionHandler clientDisconnected;

        public SSLTcpServer(IPAddress pIPAddress, int pPort, string pX509CertificatePath, string pX509CertificatePassword) {
            IPAddress = pIPAddress;
            Port = pPort;
            serverCertificate = new X509Certificate2(pX509CertificatePath);
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
                    Log.Debug("Unable to accept connection");
                }

            }
        }

        private void HandleConnection(TcpClient pTcpClient) {
            if (clientConnected != null) {
                SSLTcpClient objClient = new SSLTcpClient(pTcpClient, serverCertificate);
                objClient.disconnected += objClient_disconnected;
                clientConnected(objClient);
            }
        }

        void objClient_disconnected(SSLTcpClient pClient) {
            pClient.disconnected -= objClient_disconnected;
            if (clientDisconnected != null) {
                clientDisconnected(pClient);
            }
        }
    }
}
