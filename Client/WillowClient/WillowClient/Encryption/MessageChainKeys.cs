using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Encryption
{
    public class MessageChainKeys
    {
        public byte[] ChainKey { get; set; }
        public byte[] MessageKey { get; set; }
    }
}
