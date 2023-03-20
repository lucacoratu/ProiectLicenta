using SQLite;
using SQLitePCL;
using System;
using System.Collections.Generic;
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
    }
}
