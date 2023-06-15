using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Database.Model {
    [Table("keyvalue")]
    public class KeyValue {
        [Column("key"),PrimaryKey]
        public string Key { get; set; }
        [Column("value")]
        public string Value { get; set; }
        [Column("hash")]
        public byte[] Hash { get; set; }
        [Column("expirationDate")]
        public DateTime ExpirationDate { get; set; } 
    }
}
