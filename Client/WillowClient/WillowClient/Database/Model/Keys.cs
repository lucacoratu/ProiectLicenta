using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Database.Model {
    [Table("keys")]
    public class Keys {
        //This is the id of the key
        [Column("keyId"), PrimaryKey, AutoIncrement]
        public int KeyId { get; set; }

        //This is the room id for the key
        [Column("roomId")]
        public int RoomId { get; set; }

        //==========KEYS USED TO SEND THE GROUP CHAINKEY==========\\
        //This column is for storing the public key of the participant
        [Column("identityPubKey"), AllowNull]
        public string IdentityPublicKey { get; set; }
        //This column is for storing the pre signed public key of the participant
        [Column("preSignedPubKey"), AllowNull]
        public string PreSignedPublicKey { get; set; }
        //This column is for storing the master secret between the accounts
        [Column("masterSecret"), AllowNull]
        public string MasterSecret { get; set; }
        //This column is for storing the root key between the accounts
        [Column("userRootKey"), AllowNull]
        public string UserRootKey { get; set; }
        //This column is for storing the chain key between the accounts
        [Column("userChainKey"), AllowNull]
        public string UserChainKey { get; set; }
        //This column is for storing the ephemeral private key of the current user generated for the participant
        [Column("ephemeralPrivate"), AllowNull]
        public string EphemeralPrivateKey { get; set; }
        //This column is for storing the ephemeral public key of the current user generated for the participant
        [Column("ephemeralPublic"), AllowNull]
        public string EphemeralPublicKey { get; set; }
        //This column is for storing the ephemeral secret which will change when a message is received from the user
        [Column("ephemeralSecret"), AllowNull]
        public string EphemeralSecret { get; set; }
        //============================================================\\

        //==========KEYS USED TO ENCRYPT THE GROUP MESSAGE==========\\
        //This column if for storing the chain key of the participant which is used to derive the message key
        [Column("groupChainKey"), AllowNull]
        public string GroupChainKey { get; set; }

        //This column if for storing the signature public key of the participant which is used to verify the messages
        [Column("signaturePublicKey"), AllowNull]
        public string GroupSignaturePublicKey { get; set; }
        //============================================================\\
    }
}
