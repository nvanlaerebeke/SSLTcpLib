using SSLTcpLib.Client;

namespace SSLTcpLib.Senders {
    public class TextSender {
        SSLTcpClient _objClient;

        public TextSender(SSLTcpClient pClient) {
            _objClient = pClient;
        }

        public void Send(string pData) {
            _objClient.Send(System.Text.Encoding.UTF8.GetBytes(pData));
        }

    }
}
