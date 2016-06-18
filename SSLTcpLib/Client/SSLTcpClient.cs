using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace SSLTcpLib.Client {
    public class SSLTcpClient : IDisposable {
        /**
         * Public fields
         */
        public SslStream SslStream { get; private set; }

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
        public SSLTcpClient(TcpClient pClient, X509Certificate2 pCert) {
            SslStream = new SslStream(
                pClient.GetStream(),
                false,
                new RemoteCertificateValidationCallback(
                    delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
                        return true;
                    }
                ),
                new LocalCertificateSelectionCallback(
                    delegate (object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers) {
                        return new X509Certificate2(pCert);
                    }
                )
            );

            try {
                SslStream.AuthenticateAsServer(pCert, true, SslProtocols.Tls, true);
            } catch (AuthenticationException) {
                pClient.Close();
                return;
            }

            StartListener();

            if (connected != null) {
                connected(this);
            }

            
        }

        private void StartListener() {
            Thread objThread = new Thread(new ThreadStart(delegate () {
                try {
                    SSLTcpListener objListener = new SSLTcpListener(this);
                    objListener.dataReceived += delegate (byte[] pData) {
                        if (dataReceived != null) {
                            dataReceived(pData);
                        }
                    };
                    objListener.Run();
                } catch (Exception ex) {
                    Dispose();
                    throw ex;
                }
            }));
            objThread.Start();
        }

        /**
         * Connect the TcpClient
         */
        public void ConnectAsync(IPAddress pIP, int pPort, string pX509CertificatePath, string pX509CertificatePassword) {
            TcpClient objClient = new TcpClient();
            if (!objClient.ConnectAsync(pIP, pPort).Wait(1000)) {
                throw new Exception("Connect failed");
            };

            X509Certificate2 clientCertificate;
            X509Certificate2Collection clientCertificatecollection = new X509Certificate2Collection();
            try {
                clientCertificate = new X509Certificate2(pX509CertificatePath, pX509CertificatePassword);
                clientCertificatecollection.Add(clientCertificate);
            } catch (CryptographicException ex) {
                objClient.Close();
                throw ex;
            }

            SslStream = new SslStream(
                objClient.GetStream(),
                false,
                new RemoteCertificateValidationCallback(
                    delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
                        return true;
                    }
                ),
                new LocalCertificateSelectionCallback(
                    delegate (object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers) {
                        var cert = new X509Certificate2(pX509CertificatePath, pX509CertificatePassword);
                        return cert;
                    }
                )
            );

            try {
                SslStream.AuthenticateAsClient(pIP.ToString(), clientCertificatecollection, SslProtocols.Tls, false);
            } catch (AuthenticationException ex)  {
                objClient.Close();
                throw ex;
            }

            StartListener();

            if (connected != null) {
                connected(this);
            }
        }

        /**
         * Send data(bytes) to the other side
         * Uses the PackagesHelper to 'package' the data
         */
        public void Send(byte[] pData) {
            try {
                SslStream.Write(PackageHelper.Create(pData));
            } catch (Exception ex) {
                Dispose();
                throw ex;
            }
        }

        /**
         * Shutdown
         */
        public void Dispose() {
            SslStream.Close();
            if (disconnected != null) {
                disconnected(this);
            }
        }
    }
}
