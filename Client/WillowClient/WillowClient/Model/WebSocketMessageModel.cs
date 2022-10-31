using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class WebSocketMessageModel
    {
        public int Type { get; set; }
        public string Body { get; set; }
    }
}
