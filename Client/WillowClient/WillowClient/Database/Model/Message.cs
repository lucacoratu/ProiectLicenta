using SQLite;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Database.Model {
    [Table("messages")]
    public class Message {
        [Column("id"), PrimaryKey]
        public int Id { get; set; }

        [Column("owner")]
        public int Owner { get; set; }
        [Column("timestamp")]
        public DateTime TimeStamp { get; set; }

        [Column("text")]
        public string Text { get; set; }

        [Column("senderName")]
        public string SenderName { get; set; }

        //This column is used for storing the type of message
        [Column("type")]
        public string Type { get; set; }

        //This column is used for ephemeral public key
        [Column("ephemeralPublic")]
        public string EphemeralPublic { get; set; }

        //This column is used for identity public key
        [Column("identityPublic")]
        public string IdentityPublic { get; set; }

    }
}
