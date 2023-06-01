using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Database.Model
{
    [Table("participants")]
    public class Participant
    {
        //This is the id of the participant (user id)
        [Column("id"),PrimaryKey]
        public int Id { get; set; }
        //This is the name of the participant
        [Column("name")]
        public string Name { get; set; }
    }
}
