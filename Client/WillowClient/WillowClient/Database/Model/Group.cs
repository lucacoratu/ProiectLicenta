using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Database.Model {
    [Table("groups")]
    public class Group {
        //This is the room id for the group
        [Column("roomId"),PrimaryKey]
        public int RoomId { get; set; }
        //This is the id of the user who created the group
        [Column("creatorId")]
        public int CreatorId { get; set; }
        //This is the name of the group
        [Column("groupName")]
        public string GroupName { get; set; }
        //This is the last meesage sent in the group
        [Column("lastMessage")]
        public string LastMessage {get; set;}
        //This is the timestamp when the last message was send in the group
        [Column("lastMessageTimestamp")]
        public string LastMessageTimestamp { get; set; }
        //This is the name of the sender of last message in the group
        [Column("lastMessageSender")]
        public int LastMessageSender { get; set; }
        //This is url where the group picture can be found
        [Column("groupPictureUrl")]
        public string GroupPictureUrl { get; set; }
        //This is the number of new messages the user has in the group
        [Column("numberNewMessages")]
        public string NumberNewMessages { get; set; }
    }
}
