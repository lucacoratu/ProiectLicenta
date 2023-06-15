using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Database.Model
{
    [Table("group_participant")]
    public class GroupParticipant
    {
        //This is the id of the group (roomId)
        [Column("groupId")]
        public int GroupId { get; set; }
        //This is the id of the participant in the group
        [Column("participantId")]
        public int ParticipantId { get; set; }
    }
}
