//#define LOCAL_ANDROID
//#define LOCAL_ANDROID_HOME
#define LOCAL_ANDROID_MOTOC
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
#if LOCAL_ANDROID
        public static string serverURL = "http://172.20.10.8:8080";
        public static string wsServerUrl = "ws://172.20.10.8:8087/";
        public static string chatServerUrl = "http://172.20.10.8:8087/";
        public static string signalingServerURL = "https://172.20.10.8:8090/";
        public static string signalingWsUrl = "wss://";
#elif LOCAL_ANDROID_HOME
        public static string serverURL = "http://192.168.0.107:8080";
        public static string wsServerUrl = "ws://192.168.0.107:8087/";
        public static string chatServerUrl = "http://192.168.0.107:8087/";
        public static string signalingServerURL = "https://192.168.0.107:8090/";
        public static string signalingWsUrl = "wss://192.168.0.107:8090/notifications";
#elif LOCAL_ANDROID_MOTOC
        public static string serverURL = "http://192.168.137.1:8080";
        public static string wsServerUrl = "ws://192.168.137.1:8087/";
        public static string chatServerUrl = "http://192.168.137.1:8087/";
        public static string signalingServerURL = "https://192.168.137.1:8090/";
        public static string signalingWsUrl = "wss://192.168.137.1:8090/notifications";
#else
        public static string serverURL = "http://10.0.2.2:8080";
        public static string wsServerUrl = "ws://10.0.2.2:8087/";
        public static string chatServerUrl = "http://10.0.2.2:8087/";
        public static string signalingServerURL = "https://10.0.2.2:8090/";
        public static string signalingWsUrl = "wss://10.0.2.2:8090/notifications";
#endif
#else
        public static string serverURL = "http://localhost:8080";
        public static string wsServerUrl = "ws://localhost:8087/";
        public static string chatServerUrl = "http://localhost:8087/";
        public static string signalingServerURL = "https://localhost:8090/";
        public static string signalingWsUrl = "wss://localhost:8090/notifications";
#endif
        //public static string defaultProfilePicture = "https://raw.githubusercontent.com/jamesmontemagno/app-monkeys/master/baboon.jpg";
        public static string defaultProfilePicture = "default_profile_icon.png";
        //public static string defaultGroupPicture = "https://raw.githubusercontent.com/jamesmontemagno/app-monkeys/master/baboon.jpg";
        public static string defaultGroupPicture = "default_group.png";

        //Strings used for sqlite3 local database
        public const string databaseFilename = "willow.db3";

        public const SQLite.SQLiteOpenFlags databaseFlags = 
            // open the database in read/write mode
            SQLite.SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLite.SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLite.SQLiteOpenFlags.SharedCache;

        public static string databasePath => Path.Combine(FileSystem.AppDataDirectory, databaseFilename);

        //Secure storage keys for the identity key and the pre signed key (private)
        public static string identityKey = "identityPrivate";

        public static string preSignedPrivate = "preSignedPrivate";
    }
}
