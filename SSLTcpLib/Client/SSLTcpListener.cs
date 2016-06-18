using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSLTcpLib.Client {
    internal class SSLTcpListener {
        private SSLTcpClient _objClient;

        public event DataTransfer dataReceived;
        public SSLTcpListener(SSLTcpClient pClient) {
            _objClient = pClient;
        }

        /**
         * Reads from the stream untill a frame has been fetched
         * a frame looks like this
         *   StartByte: 7E (2 bytes)
         *   Lenght: xxxx (4 bytes, so max 2gb data)
         *   Data: number of bytes specified as the length
         *   CRC32: 4bytes representing the CRC for the data
         *   EndByte: 81 (2 bytes)
         */
        public async void Run() {
            while (true) {
                //Start reading from the stream till we get a new frame
                //7E signals a frame start, anything else is ignore
                //when receiving something else it could be considered a protocal error
                //can discard the data or throw an exception
                //right now we ignore any data send untill we get a frame start byte
                byte[] arrStarByte;
                do {
                    arrStarByte = await ReadBytes(1);
                } while (BitConverter.ToUInt16(arrStarByte, 0) != 0x7e);

                //Received frame start, now read the length => 32bit (2gb) is the max lenght
                byte[] arrFrameLength = (await ReadBytes(4)).Reverse<byte>().ToArray<byte>();
                int length = BitConverter.ToInt32(arrFrameLength, 0);

                //Now go get the data
                byte[] data = await ReadBytes(length);
                uint crc = Hash.CRC32.Compute(data);

                //After the data we get the checksum(crc32)
                byte[] arrCheckSum = (await ReadBytes(4)).Reverse<byte>().ToArray<byte>();
                uint remote_crc = BitConverter.ToUInt32(arrCheckSum, 0);
                if (crc != remote_crc) {
                    throw new Exception("Package CRC Failed!");
                }

                //Last 2 bytes should be the frame end, = 0x81 (inverse of 0x7e)
                byte[] arrEndByte = await ReadBytes(1);
                if (BitConverter.ToInt16(arrEndByte, 0) != 0x81) {
                    throw new Exceptions.ProtocolException();
                }

                if(dataReceived != null) {
                    dataReceived(data);
                }
            }
        }
        
        private async Task<byte[]> ReadBytes(int pAmount) {
            byte[] buffer;
            if(pAmount == 1) {
                buffer = new byte[2];
            } else {
                 buffer = new byte[pAmount];
            }
            int read = 0, offset = 0, toRead = pAmount;
            while (toRead > 0 && (read = await _objClient.SslStream.ReadAsync(buffer, offset, toRead)) > 0) {
                toRead -= read;
                offset += read;
            }
            if (toRead > 0) throw new EndOfStreamException();
            return buffer;
        }
    }
}
