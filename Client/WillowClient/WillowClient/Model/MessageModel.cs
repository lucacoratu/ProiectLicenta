using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public enum MessageOwner {
        CurrentUser,
        OtherUser
    }
    public class MessageModel
    {
        public MessageOwner Owner { get; set; }
        public string TimeStamp { get; set; }
        public string Text { get; set; }
        public string SenderName { get; set; }
    }
}
