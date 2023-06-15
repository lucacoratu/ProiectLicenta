using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Database.Model {
    [Table("friend_message")]
    public class RoomMessage {
        [Column("roomId")]
        public int RoomId{ get; set; }

        [Column("messageId")]
        public int MessageId { get; set; }
    }
}
