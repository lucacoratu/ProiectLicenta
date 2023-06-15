using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class CallFriendModel
    {
        public int caller {get; set;}
        public int callee { get; set; }
        public string option { get; set; }
        public int roomId { get; set; }
    }
}
