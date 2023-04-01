using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Database.Model {
    [Table("friends")]
    public class Friend {
        //The id of the friend
        //It should not modify after caching
        [Column("id"), PrimaryKey]
        public int Id { get; set; }

        //The display name of the friend
        //It should not modify after caching??
        [Column("displayName")]
        public string DisplayName { get; set; }

        //The about of the friend
        [Column("about")]
        public string About { get; set; }

        //This column is determining the date the account befriended this one
        //It should not modify after caching
        [Column("befriendDate")]
        public string BefriendDate { get; set; }

        //This column is representing the date when this friend was last online
        //It will modify regularly
        [Column("lastOnline")]
        public DateTime LastOnline { get; set; }

        //This column is representing the current status of the friend
        //It will modify regularly
        [Column("status")]
        public string Status { get; set; }

        //This column is determining the date when the friend created the account
        //It should not be modified after caching
        [Column("joinDate")]
        public DateTime JoinDate { get; set; }

        //This column is for identifying the room where messages can be sent to this user
        //It should not be modified after caching
        [Column("roomId")]
        public int RoomID { get; set; }

        //This column is for storing the last message for quicker load of the main page
        //It will modify often
        [Column("lastMessage")]
        public string LastMessage { get;set; }

        //This column is for storing the last message timestamp
        //It will modify often
        [Column("lastMessageTimestamp")]
        public string LastMessageTimestamp { get; set; }

        //This column is for storing the number of new messages in the friend conversation
        [Column("numberNewMessages")]
        public string NumberNewMessages { get; set; }

        //This column is for storing the profile picture url of the friend
        [Column("profilePictureUrl")]
        public string ProfilePictureUrl { get; set; }
        //This column is for storing the public key of the friend
        [Column("identityPubKey")]
        public string IdentityPublicKey { get; set; }
        //This column is for storing the pre signed public key of the friend
        [Column("preSignedPubKey")]
        public string PreSignedPublicKey { get; set; }
        //This column is for storing the master secret between the accounts
        [Column("masterSecret"), AllowNull]
        public string MasterSecret { get; set; }
        //This column is for storing the root key between the accounts
        [Column("rootKey"), AllowNull]
        public string RootKey { get ; set; }
        //This column is for storing the chain key between the accounts
        [Column("chainKey"), AllowNull]
        public string ChainKey { get; set; }
        //This column is for storing the ephemeral private key of the current user generated for the friend
        [Column("ephemeralPrivate"), AllowNull]
        public string EphemeralPrivateKey { get; set; }
        //This column is for storing the ephemeral public key of the current user generated for the friend
        [Column("ephemeralPublic"), AllowNull]
        public string EphemeralPublicKey { get; set; }
        //This column is for storing the ephemeral secret which will change when a message is received from the use
        [Column("ephemeralSecret"), AllowNull]
        public string EphemeralSecret { get; set; }
    }
}
