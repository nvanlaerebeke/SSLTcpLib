using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace SSLTcpLib {
    public sealed partial class SSLTcpClient : IDisposable {
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
                    delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
                        return true;
                    }
                ),
                new LocalCertificateSelectionCallback(
                    delegate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers) {
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

            Thread objThread = new Thread(new ThreadStart(RunListener));
            objThread.Start();

            if (connected != null) {
                connected(this);
            }
        }

        /**
         * Connect the TcpClient
         */
        public bool ConnectAsync(IPAddress pIP, int pPort, string pX509CertificatePath, string pX509CertificatePassword) {
            TcpClient objClient = new TcpClient();
            try {
                if(!objClient.ConnectAsync(pIP, pPort).Wait(1000)) {
                    throw new Exception("Connect failed");
                };
            } catch (Exception) {
                return false;
            }
            X509Certificate2 clientCertificate;
            X509Certificate2Collection clientCertificatecollection = new X509Certificate2Collection();
            try {
                clientCertificate = new X509Certificate2(pX509CertificatePath, pX509CertificatePassword);
                clientCertificatecollection.Add(clientCertificate);
             } catch(CryptographicException) {
                objClient.Close();
                return false;
            }

            SslStream = new SslStream(
                objClient.GetStream(), 
                false, 
                new RemoteCertificateValidationCallback(
                    delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { 
                        return true;
                    }
                ), 
                new LocalCertificateSelectionCallback(
                    delegate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers) {
                        var cert = new X509Certificate2(pX509CertificatePath, pX509CertificatePassword);
                        return cert;
                    }
                )
            );

            try {
                SslStream.AuthenticateAsClient(pIP.ToString(), clientCertificatecollection, SslProtocols.Tls, false);
            } catch (AuthenticationException) {
                objClient.Close();
                return false;
            }

            Thread objThread = new Thread(new ThreadStart(RunListener));
            objThread.Start();

            if (connected != null) {
                connected(this);
            }
            return true;
        }

        /**
         * Reading
         */
        private async void RunListener() {
            try {
                while (true) {
                    byte[] bytes = new byte[8];
                    await SslStream.ReadAsync(bytes, 0, (int)bytes.Length);

                    int bufLenght = BitConverter.ToInt32(bytes, 0);
                    if (bufLenght > 0) {
                        byte[] buffer = new byte[bufLenght];
                        SslStream.Read(buffer, 0, bufLenght);

                        if (dataReceived != null) {
                            dataReceived(this, buffer);
                        }
                    }
                }
            } catch (Exception) {
                Dispose();
            }
        }

        /**
         * Writing 
         */
        public bool Send(byte[] pData) {
            try {
                byte[] lenght = BitConverter.GetBytes(pData.Length);
                Array.Resize(ref lenght, 8);

                SslStream.Write(lenght);
                if (!SslStream.WriteAsync(pData, 0, pData.Length).Wait(1000)) {
                    throw new Exception("Send timed out");
                }
            } catch (Exception) {
                Dispose();
                return false;
            }
            return true;
        }

        public bool Send(string pData) {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(pData);
            return Send(bytes);
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
