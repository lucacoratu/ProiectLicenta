using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public static class Constants
    {
#if ANDROID
        //public static string serverURL = "http://10.10.20.219:8080";
        //public static string wsServerUrl = "ws://10.10.20.219:8087/";
        //public static string chatServerUrl = "http://10.10.20.219:8087/";
        public static string serverURL = "http://10.0.2.2:8080";
        public static string wsServerUrl = "ws://10.0.2.2:8087/";
        public static string chatServerUrl = "http://10.0.2.2:8087/";
#else
        public static string serverURL = "http://localhost:8080";
        public static string wsServerUrl = "ws://localhost:8087/";
        public static string chatServerUrl = "http://localhost:8087/";
#endif
    }
}
