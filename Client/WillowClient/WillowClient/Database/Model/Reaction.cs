using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Database.Model {
    [Table("reactions")]
    public class Reaction {
        [Column("id"), PrimaryKey]
        public int Id { get; set; }

        [Column("senderId")]
        public int SenderId { get; set; }
        [Column("emoji")]
        public string Emoji { get; set; }
        [Column("reactionDate")]
        public DateTime ReactionDate { get; set; }
    }
}
