using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class SendPrivateMessageModel
    {
        public int roomId { get; set; }
        public string data { get; set; }
        public string messageType { get; set; }
    }
}
