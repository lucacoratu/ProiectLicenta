using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Database.Model {

    [Table("message_reaction")]
    public class MessageReaction {
        [Column("messageId")]
        public int MessageId { get; set; }

        [Column("reactionId")]
        public int ReactionId { get;set; } 
    }   
}
