using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Database.Model {
    [Table("participant_key")]
    public class ParticipantKey {
        [Column("participantId")]
        public int ParticipantId { get; set; }
        [Column("keyId")]
        public int KeyId { get; set; }
    }
}
