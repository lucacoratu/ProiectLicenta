using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EndToEndWillow {
    public class MessageModel {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string MessageData { get; set; }
        //This is the initiator public key base64 encoded
        public string InitiatorEphemeralPublicKey { get; set; }

        //This is the initiator identity public key base64 encoded
        public string InitatorIdentityPublicKey { get; set; }
        public string Timestamp { get; set; }
    }
}
