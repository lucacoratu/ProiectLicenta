using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Database.Model {
    [Table("friends")]
    public class Friend {
        //The id of the friend
        //It should not modify after caching
        [Column("id"), PrimaryKey]
        public int Id { get; set; }

        //The display name of the friend
        //It should not modify after caching??
        [Column("displayName")]
        public string DisplayName { get; set; }

        //This column is determining the date the account befriended this one
        //It should not modify after caching
        [Column("befriendDate")]
        public DateTime BefriendDate { get; set; }

        //This column is representing the date when this friend was last online
        //It will modify regularly
        [Column("lastOnline")]
        public DateTime LastOnline { get; set; }

        //This column is representing the current status of the friend
        //It will modify regularly
        [Column("status")]
        public string Status { get; set; }

        //This column is determining the date when the friend created the account
        //It should not be modified after caching
        [Column("joinDate")]
        public DateTime JoinDate { get; set; }

        //This column is for identifying the room where messages can be sent to this user
        //It should not be modified after caching
        [Column("roomId")]
        public int RoomID { get; set; }

        //This column is for storing the last message for quicker load of the main page
        //It will modify often
        [Column("lastMessage")]
        public string LastMessage { get;set; }

        //This column is for storing the last message timestamp
        //It will modify often
        [Column("lastMessageTimestamp")]
        public DateTime LastMessageTimestamp { get; set; }

        //This column is for storing the profile picture url of the friend
        [Column("profilePictureUrl")]
        public string ProfilePictureUrl { get; set; }
    }
}
