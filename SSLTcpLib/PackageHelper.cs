using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace SSLTcpLib {
    class PackageHelper {
        public static byte[] Create(byte[] pBytes) {
            List<byte> objBytes = new List<byte>();
            //Start byte is 0x007E
            objBytes.Add((byte)0x7e);
            
            //2nd param is data lenght => 4bytes (32bit), maximum transmission is 2gb
            objBytes.AddRange(BitConverter.GetBytes(pBytes.Length).Reverse<byte>());

            //add the data
            objBytes.AddRange(pBytes);

            //calculate the checksum
            uint hash = Hash.CRC32.Compute(pBytes);
            objBytes.AddRange(BitConverter.GetBytes(hash).Reverse<byte>());

            //end byte is 0x81 
            objBytes.Add((byte)0x81);
            return objBytes.ToArray();
        }

        public static byte[] Read(TcpClient pClient) {
            return new byte[0];
        }
    }
}
