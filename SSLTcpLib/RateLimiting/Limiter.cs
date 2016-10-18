using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SSLTcpLib.RateLimiting {
    internal static class Limiter {
        /// <summary>
        /// Cacluate the average over this time frame
        /// </summary>
        private static int avrgtime = 5;

        /// <summary>
        /// The max number of bytes/s
        /// </summary>
        private static long _lngDownloadMax = 0; // 1000000; // 1MB/s
        private static long _lngUploadMax = 0; //1000000; // 1MB/s

        private static List<NetworkTrafic> _lstDownload = new List<NetworkTrafic>();
        private static List<NetworkTrafic> _lstUpload = new List<NetworkTrafic>();

        public static void DownloadAddAndWait(long pAmount) {
            if (_lngDownloadMax > 0 && pAmount > 0) {
                _lstDownload.RemoveAll(t => t.Time < Environment.TickCount - (avrgtime * 1000));
                _lstDownload.Add(new NetworkTrafic(Environment.TickCount, pAmount));
                var total = _lstDownload.Sum(t => t.Amount);
                var current = total / avrgtime;
                if (current > _lngDownloadMax) {
                    var tosleep = current - _lngDownloadMax;
                    Thread.Sleep((int)(current - _lngDownloadMax));
                }
            }
        }

        public static void UploadAddAndWait(long pAmount) {
            if (_lngUploadMax > 0 && pAmount > 0) {
                _lstUpload.RemoveAll(t => t.Time < Environment.TickCount - (avrgtime * 1000));
                _lstUpload.Add(new NetworkTrafic(Environment.TickCount, pAmount));
                var total = _lstUpload.Sum(t => t.Amount);
                var current = total / avrgtime;
                if (current > _lngUploadMax) {
                    var tosleep = current - _lngUploadMax;
                    Thread.Sleep((int)(current - _lngUploadMax));
                }
            }
        }

        private class NetworkTrafic {
            public long Time { get; private set; }
            public long Amount { get; private set; }

            public NetworkTrafic(long pTime, long pAmount) {
                Time = pTime;
                Amount = pAmount;
            }
        }
    }
}
