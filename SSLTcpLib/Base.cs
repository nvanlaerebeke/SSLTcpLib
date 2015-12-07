using log4net;

namespace SSLTcpLib {
    public class Base { 
        protected static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    }
}
