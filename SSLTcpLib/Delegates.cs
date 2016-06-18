using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSLTcpLib.Client;

namespace SSLTcpLib {
    public delegate void ConnectionHandler(SSLTcpClient pClient);
    public delegate void DataTransfer(byte[] pData);
}
