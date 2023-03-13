using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Database.Model {
    [Table("Account")]
    public class Account {
        [PrimaryKey]
        [Column("id")]
        public int Id { get; set; }
        //Keep the username and the password of the account if the remember me checkbox is checked
        [Column("username")]
        public string Username { get; set; }
        [Column("password")]
        public string Password { get; set; }
        //Keep track if the user selected remember me
        [Column("rememberMe")]
        public bool RememberMe { get; set; }
    }
}
