using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace SSLTcpLib.Client {
    public class SSLTcpClient : IDisposable {
        private SslStream SslStream { get; set; }
        private System.Collections.Concurrent.BlockingCollection<byte[]> _colQueue;
        private Thread _objSenderThread;
        private KeepAliveMonitor _objKeepAliveMonitor;

        /**
         * Events
         */
        public event ConnectionHandler connected;
        public event ConnectionHandler disconnected;
        public event DataTransfer dataReceived;


        #region Constructor & Connecting
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

            if (connected != null) {
                connected(this);
            }
            Start();
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
            } catch (AuthenticationException) {
                objClient.Close();
            }

            if (connected != null) {
                connected(this);
            }

            Start();
        }

        private void Start() {
            _objKeepAliveMonitor = new KeepAliveMonitor();
            _objKeepAliveMonitor.keepAliveSendTimeoutReceived += delegate (object sender, EventArgs e) {
                Console.WriteLine("Sending Keepalive");
                SslStream.WriteByte(0xff);
            };
            _objKeepAliveMonitor.shutdownReceived += delegate (object sender, EventArgs e) {
                Dispose();
            };

            _objKeepAliveMonitor.Start();
            StartListener();
            StartSenderQueue();
        }
        #endregion

        #region Listener & Sender Threads
        private void StartSenderQueue() {
            _colQueue = new System.Collections.Concurrent.BlockingCollection<byte[]>();
            _objSenderThread = new Thread(() => {
                Thread.CurrentThread.Name = "SenderQueue";
                //Console.WriteLine("Send Loop");
                while (true) {
                    SendBytes(_colQueue.Take());
                    //Console.WriteLine("Bytes Send");
                }
            });
            _objSenderThread.Start();
        }

        private void StartListener() {
            Thread objThread = new Thread(new ThreadStart(delegate () {
                try {
                    while (true) {
                        //Start reading from the stream till we get a new frame
                        //7E signals a frame start, 0xff a keepalive, anything else is ignore
                        //when receiving something else it could be considered a protocal error
                        //can discard the data or throw an exception
                        //right now we ignore any data send until we get a frame start byte
                        //Console.WriteLine("Start listening for next message...");
                        byte[] arrStarByte;
                        do {
                            arrStarByte = ReadBytes(1);
                            Console.WriteLine("Data received");
                            if (BitConverter.ToUInt16(arrStarByte, 0) == 0xff) {
                                Console.WriteLine("KeepAlive");
                                _objKeepAliveMonitor.DataReceivedStart();
                                _objKeepAliveMonitor.DataReceivedStop();
                            }
                        } while (BitConverter.ToUInt16(arrStarByte, 0) != 0x7e);

                        //reset the keepalive timer, we have received data
                        _objKeepAliveMonitor.DataReceivedStart();

                        //Received frame start, now read the length => 32bit (2gb) is the max lenght
                        byte[] arrFrameLength = (ReadBytes(4)).Reverse<byte>().ToArray<byte>();
                        int length = BitConverter.ToInt32(arrFrameLength, 0);

                        //Now go get the data
                        byte[] data = ReadBytes(length);
                        uint crc = Hash.CRC32.Compute(data);

                        //After the data we get the checksum(crc32)
                        byte[] arrCheckSum = (ReadBytes(4)).Reverse<byte>().ToArray<byte>();
                        uint remote_crc = BitConverter.ToUInt32(arrCheckSum, 0);
                        if (crc != remote_crc) {
                            throw new Exception("Package CRC Failed!");
                        }

                        //Last 2 bytes should be the frame end, = 0x81 (inverse of 0x7e)
                        byte[] arrEndByte = ReadBytes(1);
                        if (BitConverter.ToInt16(arrEndByte, 0) != 0x81) {
                            throw new Exceptions.ProtocolException();
                        }

                        if (dataReceived != null) {
                            dataReceived(this, data);
                        }

                        //Signal end of receiving data
                        _objKeepAliveMonitor.DataReceivedStop();
                    }
                } catch (Exception ex) {
                    Console.WriteLine("Listener Broken: " + ex.Message);
                    Dispose();
                }
            }));
            objThread.Start();
        }

        #endregion

        #region reading and sending bytes from stream
        private byte[] ReadBytes(int pAmount) {
            byte[] buffer;
            if (pAmount == 1) {
                buffer = new byte[2];
            } else {
                buffer = new byte[pAmount];
            }
            int read = 0, offset = 0, toRead = pAmount;
            while (toRead > 0 && (read = SslStream.Read(buffer, offset, toRead)) > 0) {
                toRead -= read;
                offset += read;
            }
            if (toRead > 0) throw new EndOfStreamException();
            return buffer;
        }
        
        /**
         * Send data(bytes) to the other side
         * Uses the PackagesHelper to 'package' the data
         */
        private void SendBytes(byte[] pData) {
            try {
                _objKeepAliveMonitor.DataSendStart();
                SslStream.Write(PackageHelper.Create(pData));
                _objKeepAliveMonitor.DataSendStop();
            } catch (Exception ex) {
                Console.WriteLine("Failed sending bytes: " + ex.Message);
                Dispose();
            }
        }

        public void Send(byte[] pData) {
            _colQueue.Add(pData);
        }

        #endregion

        /**
         * Shutdown
         */
        public void Dispose() {
            if (_objKeepAliveMonitor != null) {
                _objKeepAliveMonitor.Shutdown();
            }
            if (SslStream != null) {
                SslStream.Close();
            }
            if (disconnected != null) {
                disconnected(this);
            }
        }
    }
}
