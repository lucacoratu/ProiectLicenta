using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model {
    public static class Globals {
        public static string Session { get; set; }
        public static ECDiffieHellman identityKey { get; set; }
        public static ECDiffieHellman preSignedKey { get; set; }
    }
}
