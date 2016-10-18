using System.Collections.Generic;
using System.Net;
using SSLTcpLib;
using log4net;
using System.Threading;
using System.Threading.Tasks;
using System;
using SSLTcpLib.Senders;
using SSLTcpLib.Client;
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

/**
 *
 * Make the server cert by running:
 *   makecert -r -pe -n “CN=SslClient” -ss my -sr currentuser -sky exchange client.cer
 *
 * Or use openssl (nomadesk-ca)
 * 
 */

namespace Client {
    class Program {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args) {
            MultiClient.Start();

            while (true) {
                System.Threading.Thread.Sleep(1000);
            }
        }

       
    }
}