using SSLTcpLib.Client;

namespace SSLTcpLib.Senders {
    public class ByteSender {
        private SSLTcpClient _objClient;

        public ByteSender(SSLTcpClient pClient) {
            _objClient = pClient;
        }

        public void Send(byte[] pData) {
            _objClient.Send(pData);
        }
    }
}
