using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSLTcpLib {
    public class HexHelper {
        public static string byteToHex(byte[] pData) {
            StringBuilder hex = new StringBuilder(pData.Length * 2);
            foreach (byte b in pData) {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        public static byte[] hexToByte(string pHex) {
            return Enumerable.Range(0, pHex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(pHex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static string GetHexString(UInt64 pValue, int pLength = 2) {
            return string.Format("{0:X" + pLength + "}", pValue);
        }
    }
}
