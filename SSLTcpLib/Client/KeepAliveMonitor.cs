using System;

namespace SSLTcpLib.Client {
    /**
     * Helper class for keepalives
     * - send keepalive event is triggered after not sending data for 30 seconds
     * - shutdown event is triggered after not receiving any data for 45 seconds
     * 
     * This will make it so that we have a 15 second window for the keepalive that the client sends to arive
     */
    public class KeepAliveMonitor {
        public event EventHandler keepAliveSendTimeoutReceived;
        public event EventHandler shutdownReceived;

        private System.Timers.Timer _objKeepAliveTimerListener;
        private System.Timers.Timer _objKeepAliveTimerSender;
        
        public KeepAliveMonitor() { }

        public void Start() {
            _objKeepAliveTimerListener = new System.Timers.Timer(45000);
            _objKeepAliveTimerListener.Elapsed += delegate (object sender, System.Timers.ElapsedEventArgs e) {
                _objKeepAliveTimerListener.Stop();
                if (shutdownReceived != null) {
                    shutdownReceived(this, new EventArgs());
                }
            };

            _objKeepAliveTimerSender = new System.Timers.Timer(30000);
            _objKeepAliveTimerSender.Elapsed += delegate (object sender, System.Timers.ElapsedEventArgs e) {
                if(keepAliveSendTimeoutReceived != null) {
                    keepAliveSendTimeoutReceived(this, new EventArgs());
                }
            };

            _objKeepAliveTimerListener.Start();
            _objKeepAliveTimerSender.Start();
        }

        public void Stop() {
            _objKeepAliveTimerListener.Stop();
        }

        #region Signals the start and stop of receiving & sending data
        public void DataReceivedStart() {
            _objKeepAliveTimerListener.Stop();
        }

        public void DataReceivedStop() {
            _objKeepAliveTimerListener.Start();
        }

        public void DataSendStart() {
            _objKeepAliveTimerSender.Stop();
        }

        public void DataSendStop() {
            _objKeepAliveTimerSender.Start();
        }
        #endregion

        public void Shutdown() {
            _objKeepAliveTimerListener.Stop();
            _objKeepAliveTimerSender.Stop();
            _objKeepAliveTimerListener.Dispose();
            _objKeepAliveTimerSender.Dispose();
        }
    }
}
