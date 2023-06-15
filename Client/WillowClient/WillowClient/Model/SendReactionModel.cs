using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model {
    public class SendReactionModel {
        public int reactionId { get; set; }
        public int messageId { get; set; }
        public string emojiReaction { get; set; }
        public int senderId { get; set; }
        public int roomId { get; set; }
    }
}
