﻿using log4net;
using SSLTcpLib.Client;
using SSLTcpLib.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

/**
 *
 * Make the server cert by running:
 *   makecert -r -pe -n “CN=SslServer” -ss my -sr currentuser -sky exchange server.cer
 */

namespace Server {
    public class Program {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(Program));
        List<SSLTcpClient> _lstClients = new List<SSLTcpClient>();

        public static Thread _objSenderThread;

        static void Main(string[] args) {
            SSLTcpServer objServer = new SSLTcpServer(IPAddress.Any, 51510, System.Convert.ToBase64String(System.IO.File.ReadAllBytes(@"server.pfx")), "");
            objServer.clientConnected += objServer_clientConnected;
            objServer.clientDisconnected += objServer_clientDisconnected;
            objServer.Start();
            Console.ReadLine();
        }

        static void objServer_clientDisconnected(SSLTcpClient pClient) {
            pClient.disconnected -= objServer_clientDisconnected;
            if (_objSenderThread != null) {
                _objSenderThread.Abort();
                _objSenderThread = null;
            }
            Log.Debug("Client Disconnected");
        }

        static void objServer_clientConnected(SSLTcpClient pClient) {
            Log.Debug("Client Connected");

            //log the data we receive
            pClient.dataReceived += delegate (SSLTcpClient client, byte[] pData) {
                Log.Debug(System.Text.Encoding.UTF8.GetString(pData));
            };

            _objSenderThread = new Thread(() => SendData(pClient));
            _objSenderThread.Start();
        }

        private static void SendData(SSLTcpClient pClient) {
            SSLTcpLib.Senders.ByteSender objSender = new SSLTcpLib.Senders.ByteSender(pClient);

            //Start sending data
            try {
                //for (int i = 0; i < 10; i++) {
                    objSender.Send(System.IO.File.ReadAllBytes(@"C:\projects\data.bin"));
                  //  System.Threading.Thread.Sleep(1000);
               // } 
            } catch (Exception ex) {
                Log.Error("Sending failed, stopping loop");
                Log.Error(ex);
            }
        }
    }
}
