using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using WillowClient.Model;
using WillowClient.Database.Model;
using System.Net.WebSockets;

namespace WillowClient.Database {
    public class DatabaseService {
        //The connection to the database
        private SQLiteAsyncConnection databaseConnection;

        //The constructor of the class
        public DatabaseService() {

        }

        //This function will create all the necessary tables for the application
        //if they do not already exist
        async Task CreateTables() {
            //Check if the database connection has been succesfully created
            if (this.databaseConnection is null)
                return;

            //Create the account table where the user preferences of the account will be stored
            _ = await this.databaseConnection.CreateTableAsync<Account>();

            //Create the friends table where the account friends will be cached
            _ = await this.databaseConnection.CreateTableAsync<Friend>();

            //Create the groups table where all the groups of the account will be cached
            _ = await this.databaseConnection.CreateTableAsync<Group>();

            //Create the table where asociations between group and participants will be cached
            _ = await this.databaseConnection.CreateTableAsync<GroupParticipant>();

            //Create the table where all the participants will be stored
            _ = await this.databaseConnection.CreateTableAsync<Participant>();

            //Create the friend_message table where corelations between room and messages will be cached
            _ = await this.databaseConnection.CreateTableAsync<RoomMessage>();

            //Create the messages table where all messages will be cached
            _ = await this.databaseConnection.CreateTableAsync<Message>();

            //Create the message_reaction table where corelations between messages and reactions will be stored
            _ = await this.databaseConnection.CreateTableAsync<MessageReaction>();

            //Create the reactions tables where the reactions will be stored
            _ = await this.databaseConnection.CreateTableAsync<Reaction>();

            //Create the table where attachment information will be stored
            _ = await this.databaseConnection.CreateTableAsync<MessageAttachmentDetails>();

            //Create the key value table where the cached data will be stored like in a redis database
            _ = await this.databaseConnection.CreateTableAsync<KeyValue>();
        }

        //Initialize the database if it doesn't exist on the device
        //Create all the tables and relationships between them when the application start
        async Task InitDatabase() {
            //The connection has not been already initialized
            if (this.databaseConnection is not null)
                return;

            //Open the database connection
            this.databaseConnection = new SQLiteAsyncConnection(Constants.databasePath, Constants.databaseFlags);
            //Create the tables if they do not already exist
            await this.CreateTables();
        }

        //Public functions for handling the data in the database
        //This function will search in the local database if any account has been saved (a login has been done from the device)
        public async Task<bool> HasAccountRemembered() {
            await this.InitDatabase();

            //Get the details of all the accounts that are inserted in the account table
            var numberAccounts = await this.databaseConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Account");
           
            //There is no account saved in the local database
            if (numberAccounts == 0)
                return false;

            //Get if the account has remember me checked
            var hasRememberMeChecked = await this.databaseConnection.ExecuteScalarAsync<bool>("SELECT rememberMe FROM Account");

            return hasRememberMeChecked;
        }

        //Get the account to login
        public async Task<Account> GetAccount() {
            await this.InitDatabase();

            //Get all the accounts in the database
            var account = await this.databaseConnection.Table<Account>().Take(1).ToListAsync();

            return account[0];
        }

        //Save an account into the database after success login if remember me is selected
        public async Task<bool> SaveAccount(int id, string username, string password, bool rememberMe) {
            Account account = new Account { Id = id, Username = username, Password = password, RememberMe = rememberMe };
            var numberInserted = await this.databaseConnection.InsertOrReplaceAsync(account);
            if (numberInserted != 1)
                return false;
            return true;
        }

        //Cache friends in the local database for quicker loading
        public async Task<bool> SaveFriends(List<FriendModel> listFriends) {
            foreach(var friend in listFriends) {
                Friend f = new Friend {
                    Id = friend.FriendId,
                    BefriendDate = "",
                    JoinDate = DateTime.Parse(friend.JoinDate),
                    DisplayName = friend.DisplayName,
                    LastOnline = DateTime.Parse(friend.LastOnline),
                    Status = friend.Status,
                    RoomID = friend.RoomID,
                    LastMessage = friend.LastMessage,
                    LastMessageTimestamp = friend.LastMessageTimestamp,
                };
                //Save the model in the database
                _ = await this.databaseConnection.InsertOrReplaceAsync(f);
            }
            return true;
        }

        //Save or update the local friends
        public async Task<bool> UpdateLocalFriends(List<Friend> listLocalFriends) {
            foreach(var localFriend in listLocalFriends) {
                _ = await this.databaseConnection.InsertOrReplaceAsync(localFriend);
            }
            return true;
        }

        public async Task<bool> UpdateLocalFriends(List<FriendModel> listRemoteFriends) {
            await InitDatabase();
            List<Friend> friends = new List<Friend>();
            foreach(var remoteFriend in listRemoteFriends) {
                //Compute master secret
                //Compute root key and chain key
                friends.Add(new Friend { Id = remoteFriend.FriendId, BefriendDate = remoteFriend.BefriendDate, DisplayName = remoteFriend.DisplayName, JoinDate = DateTime.Parse(remoteFriend.JoinDate), LastMessage = remoteFriend.LastMessage, RoomID = remoteFriend.RoomID, LastMessageTimestamp = remoteFriend.LastMessageTimestamp, LastOnline = DateTime.Parse(remoteFriend.LastOnline), ProfilePictureUrl = remoteFriend.ProfilePictureUrl, Status = remoteFriend.Status, IdentityPublicKey = remoteFriend.IdentityPublicKey, PreSignedPublicKey = remoteFriend.PreSignedPublicKey });
            }
            _ = await this.databaseConnection.InsertAllAsync(friends);
            return true;
        }

        public async Task<bool> DeleteLocalFriends() {
            _ = await this.databaseConnection.DeleteAllAsync<Friend>();
            return true;
        }

        public async Task<bool> DeleteMessages() {
            _ = await this.databaseConnection.DeleteAllAsync<Message>();
            _ = await this.databaseConnection.DeleteAllAsync<RoomMessage>();
            _ = await this.databaseConnection.DeleteAllAsync<MessageAttachmentDetails>();
            return true;
        }

        public async Task<bool> SaveMessageInTheDatabase(PrivateMessageModel message, int roomId ,string senderName) {
            //Insert the message in the corresponding table
            Message msgDb = new Message { Id = message.Id, EphemeralPublic = message.EphemeralPublicKey, IdentityPublic = message.IdentityPublicKey, Owner = message.SenderId, SenderName = senderName, Text = message.Data, TimeStamp = DateTime.Now, Type = message.MessageType };
            _ = await this.databaseConnection.InsertAsync(msgDb);
            //Insert the room and message corelation
            RoomMessage roomMessage = new RoomMessage { MessageId = message.Id, RoomId = roomId };
            _ = await this.databaseConnection.InsertAsync(roomMessage);
            return true;
        }

        public async Task<Message> GetMessage(int messageId) {
            var message = await this.databaseConnection.Table<Message>().Where(msg => msg.Id == messageId).FirstOrDefaultAsync();
            return message;
        }

        public async Task<bool> SaveMessageAttachment(AttachmentModel attachment, string localFilepath, long fileSize) {
            MessageAttachmentDetails msgAttDet = new MessageAttachmentDetails { MessageId = -1, AttachmentSize = fileSize, BlobUuid = attachment.BlobUuid, Downloaded = true, LocalFilepath = localFilepath };
            _ = await this.databaseConnection.InsertAsync(msgAttDet);
            return true;
        }

        public async Task<bool> SaveUndownloadedAttachment(AttachmentModel attachment, int messageId) {
            MessageAttachmentDetails msgAttDet = new MessageAttachmentDetails { MessageId = messageId, AttachmentSize = 0, BlobUuid = attachment.BlobUuid, Downloaded = false, LocalFilepath = "" };
            _ = await this.databaseConnection.InsertAsync(msgAttDet);
            return true;
        }

        public async Task<bool> UpdateAttachment(MessageAttachmentDetails attachment) {
            _ = await this.databaseConnection.UpdateAsync(attachment);
            return true;
        }

        public async Task<MessageAttachmentDetails> GetAttachment(string blobUuid) {
            var attachment = await this.databaseConnection.Table<MessageAttachmentDetails>().Where(attach => attach.BlobUuid == blobUuid).FirstOrDefaultAsync();
            return attachment;
        }

        public async Task<MessageAttachmentDetails> GetAttachment(int messageId) {
            var attachment = await this.databaseConnection.Table<MessageAttachmentDetails>().Where(attach => attach.MessageId == messageId).FirstOrDefaultAsync();
            return attachment;
        }

        public async Task<bool> UpdateMessageIdForAttachment(string blobUuid, int messageId) {
            MessageAttachmentDetails att = await this.databaseConnection.Table<MessageAttachmentDetails>().Where(att => att.BlobUuid == blobUuid).FirstOrDefaultAsync();
            att.MessageId = messageId;
            await this.databaseConnection.UpdateAsync(att);
            return true;
        }

        public async Task<Message> GetLastMessageInRoom(int roomId) {
            try {
                var highestIdRoomMessage = await this.databaseConnection.Table<RoomMessage>().Where(roomMessage => roomMessage.RoomId == roomId).OrderByDescending(roomMessage => roomMessage.MessageId).ElementAtAsync(0);
                var message = await this.databaseConnection.Table<Message>().Where(msg => msg.Id == highestIdRoomMessage.MessageId).FirstOrDefaultAsync();
                return message;
            } catch(Exception ex) {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public async Task<Reaction> GetLastReaction(int roomId) {
            try {
                Reaction lastReaction = null;
                var roomMessages = await this.databaseConnection.Table<RoomMessage>().Where(roomMessage => roomMessage.RoomId == roomId).OrderByDescending(roomMessage => roomMessage.MessageId).ToListAsync();
                foreach(var roomMessage in roomMessages) {
                    var message = await this.databaseConnection.Table<Message>().Where(msg => msg.Id == roomMessage.MessageId).FirstOrDefaultAsync();
                    var messageReaction = await this.databaseConnection.Table<MessageReaction>().Where(messageReaction => messageReaction.MessageId == roomMessage.MessageId).FirstOrDefaultAsync();
                    if (messageReaction == null)
                        continue;
                    lastReaction = await this.databaseConnection.Table<Reaction>().Where(reaction => reaction.Id == messageReaction.ReactionId).FirstOrDefaultAsync();
                }
                //var message = await this.databaseConnection.Table<Message>().Where(msg => msg.Id == highestIdRoomMessage.MessageId).FirstOrDefaultAsync();
                //var highestMessageReaction = await this.databaseConnection.Table<MessageReaction>().Where(messageReaction => messageReaction.MessageId == message.Id).OrderByDescending(messageReaction => messageReaction.ReactionId).ElementAtAsync(0);
                //var lastReaction = await this.databaseConnection.Table<Reaction>().Where(reaction => reaction.Id == highestMessageReaction.ReactionId).FirstOrDefaultAsync();
                //lastReaction = await this.databaseConnection.Table<Reaction>().OrderByDescending(reaction => reaction.Id).ElementAtAsync(0);
                return lastReaction;
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public async IAsyncEnumerable<Reaction> GetMessageReactions(int messageId) {
            var reactionMessages = await this.databaseConnection.Table<MessageReaction>().Where(reactionMessage => reactionMessage.MessageId == messageId).ToListAsync();
            List<Reaction> reactions = new List<Reaction>();
            foreach(var reactionMessage in reactionMessages) {
                yield return await this.databaseConnection.Table<Reaction>().Where(reactionDb => reactionDb.Id == reactionMessage.ReactionId).FirstOrDefaultAsync();
            }
        }

        public async Task<int> GetNumberReactionsOfMessage(int messageId) {
            return await this.databaseConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM message_reaction WHERE messageId = " + messageId.ToString());
        }

        public async Task<bool> SaveReaction(int messageId, ReactionModel reaction) {
            MessageReaction messageReaction = new MessageReaction { MessageId = messageId, ReactionId = reaction.Id };
            _ = await this.databaseConnection.InsertAsync(messageReaction);
            Reaction newReaction = new Reaction { Id = reaction.Id, Emoji = reaction.Emoji, ReactionDate = DateTime.Parse(reaction.ReactionDate), SenderId = reaction.SenderId };
            _ = await this.databaseConnection.InsertAsync(reaction);
            return true;
        }

        public async Task<bool> SaveReaction(HistoryReactionModel reaction) {
            MessageReaction messageReaction = new MessageReaction { MessageId = reaction.MessageId, ReactionId = reaction.ReactionId };
            _ = await this.databaseConnection.InsertAsync(messageReaction);
            Reaction newReaction = new Reaction { Id = reaction.ReactionId, Emoji = reaction.Emoji, ReactionDate = DateTime.Parse(reaction.ReactionDate), SenderId = reaction.SenderId };
            _ = await this.databaseConnection.InsertAsync(newReaction);
            return true;
        }

        public async Task<bool> SaveReaction(SendReactionModel reaction) {
            MessageReaction messageReaction = new MessageReaction { MessageId = reaction.messageId, ReactionId = reaction.reactionId };
            _ = await this.databaseConnection.InsertAsync(messageReaction);
            Reaction newReaction = new Reaction { Id = reaction.reactionId, Emoji = reaction.emojiReaction, ReactionDate = DateTime.Now, SenderId = reaction.senderId };
            _ = await this.databaseConnection.InsertAsync(newReaction);
            return true;
        }

        public async Task<bool> DeleteAllReactions() {
            _ = await this.databaseConnection.DeleteAllAsync<MessageReaction>();
            _ = await this.databaseConnection.DeleteAllAsync<Reaction>();
            return true;
        }

        public async Task<int> GetNumberMessagesInRoom(int roomId) {
            int numberMessages = await this.databaseConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM friend_message WHERE roomId = " + roomId.ToString());
            return numberMessages;
        }

        public async Task<bool> SaveMasterKeyForFriend(int friendId, byte[] masterKey) {
            var friend = await this.databaseConnection.Table<Friend>().Where(friend => friend.Id == friendId).FirstOrDefaultAsync();
            friend.MasterSecret = System.Convert.ToBase64String(masterKey);
            _ = await this.databaseConnection.UpdateAsync(friend);
            return true;
        }

        public async Task<bool> SaveKeysForFriend(int friendId, byte[] masterSecret, byte[] chainKey, byte[] rootKey, string ephemeralPrivate, string ephemeralPublic) {
            var friend = await this.databaseConnection.Table<Friend>().Where(friend => friend.Id == friendId).FirstOrDefaultAsync();
            friend.MasterSecret = System.Convert.ToBase64String(masterSecret);
            friend.ChainKey = System.Convert.ToBase64String(chainKey);
            friend.RootKey = System.Convert.ToBase64String(rootKey);
            friend.EphemeralPrivateKey = ephemeralPrivate;
            friend.EphemeralPublicKey = ephemeralPublic;
            _ = await this.databaseConnection.UpdateAsync(friend);
            return true;
        }

        public async Task<bool> SaveEphemeralRootChainForFriend(int friendId, byte[] ephemeralSecret, byte[] chainKey, byte[] rootKey, string ephemeralPrivate, string ephemeralPublic) {
            var friend = await this.databaseConnection.Table<Friend>().Where(friend => friend.Id == friendId).FirstOrDefaultAsync();
            friend.EphemeralSecret = System.Convert.ToBase64String(ephemeralSecret);
            friend.ChainKey = System.Convert.ToBase64String(chainKey);
            friend.RootKey = System.Convert.ToBase64String(rootKey);
            friend.EphemeralPrivateKey = ephemeralPrivate;
            friend.EphemeralPublicKey = ephemeralPublic;
            _ = await this.databaseConnection.UpdateAsync(friend);
            return true;
        }

        public async Task<bool> SaveEphemeralRootChainForFriend(int friendId, byte[] ephemeralSecret, byte[] chainKey, byte[] rootKey) {
            var friend = await this.databaseConnection.Table<Friend>().Where(friend => friend.Id == friendId).FirstOrDefaultAsync();
            friend.EphemeralSecret = System.Convert.ToBase64String(ephemeralSecret);
            friend.ChainKey = System.Convert.ToBase64String(chainKey);
            friend.RootKey = System.Convert.ToBase64String(rootKey);
            _ = await this.databaseConnection.UpdateAsync(friend);
            return true;
        }

        public async Task<byte[]> GetChainKeyForFriend(int friendId) {
            var friend = await this.databaseConnection.Table<Friend>().Where(friend => friend.Id == friendId).FirstOrDefaultAsync();
            return System.Convert.FromBase64String(friend.ChainKey);
        }

        public async Task<byte[]> GetRootKeyForFriend(int friendId) {
            var friend = await this.databaseConnection.Table<Friend>().Where(friend => friend.Id == friendId).FirstOrDefaultAsync();
            return System.Convert.FromBase64String(friend.RootKey);
        }

        public async Task<string> GetEphemeralPrivateKey(int friendId) {
            var friend = await this.databaseConnection.Table<Friend>().Where(friend => friend.Id == friendId).FirstOrDefaultAsync();
            return friend.EphemeralPrivateKey;
        }

        public async Task<string> GetEphemeralPublicKey(int friendId) {
            var friend = await this.databaseConnection.Table<Friend>().Where(friend => friend.Id == friendId).FirstOrDefaultAsync();
            return friend.EphemeralPublicKey;
        }

        public async Task<bool> UpdateChainKeyForFriend(int friendId, byte[] newChainKey) {
            var friend = await this.databaseConnection.Table<Friend>().Where(friend => friend.Id == friendId).FirstOrDefaultAsync();
            friend.ChainKey = System.Convert.ToBase64String(newChainKey);
            _ = await this.databaseConnection.UpdateAsync(friend);
            return true;
        }

        //Get the cached friends from the database
        public async Task<List<Friend>> GetLocalFriends() {
            await this.InitDatabase();
            return await this.databaseConnection.Table<Friend>().ToListAsync();
        }

        //Update the friend profile picture
        public async Task<bool> UpdateFriendProfilePicture(int friendId, string profilePicture) {
            var friend = await this.databaseConnection.Table<Friend>().Where(friend => friend.Id == friendId).FirstOrDefaultAsync();
            friend.ProfilePictureUrl = profilePicture;
            _ = await this.databaseConnection.UpdateAsync(friend);
            return true;
        }

        //Update the friend room id
        public async Task<bool> UpdateFriendRoomId(int friendId, int roomId) {
            var friend = await this.databaseConnection.Table<Friend>().Where(friend => friend.Id == friendId).FirstOrDefaultAsync();
            friend.RoomID = roomId;
            _ = await this.databaseConnection.UpdateAsync(friend);
            return true;
        }

        //Get the number of new messages of a friend
        public async Task<int> GetNumberNewMessagesForFriend(int friendId) {
            var friend = await this.databaseConnection.Table<Friend>().Where(friend => friend.Id == friendId).FirstOrDefaultAsync();
            return friend.NumberNewMessages == null ? 0 : int.Parse(friend.NumberNewMessages);
        }

        //Get the local messages from the database
        public async IAsyncEnumerable<Message> GetLocalMessagesInRoom(int roomId) {
            //int countMessages = await this.databaseConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM friend_message WHERE roomId = " +  roomId.ToString());
            //int skipCount = 0;
            //if (countMessages > 20)
            //    skipCount = countMessages - 20;
            var roomMessage = await this.databaseConnection.Table<RoomMessage>().Where(roomMessage => roomMessage.RoomId == roomId).Skip(0).ToListAsync();
            foreach(var messageRoom in roomMessage) {
                var message = await this.databaseConnection.Table<Message>().Where(message => message.Id == messageRoom.MessageId).FirstOrDefaultAsync();
                yield return message;
            }
        }

        //Save the group in the local database
        //public async bool SaveGroup(GroupModel group) {

        //}

        //Get all the groups in the database
        public async Task<List<GroupModel>> GetLocalGroups() {
            await this.InitDatabase();
            try {
                List<GroupModel> groups = new List<GroupModel>();
                var localGroups = await this.databaseConnection.Table<Group>().ToListAsync();
                foreach (var localGroup in localGroups) {
                    //Get the participants of the group
                    GroupModel groupToAdd = new GroupModel();
                    groupToAdd.Participants = new();
                    groupToAdd.ParticipantNames = new();
                    var groupParticipants = await this.databaseConnection.Table<GroupParticipant>().Where(group => group.GroupId == localGroup.RoomId).ToListAsync();
                    foreach (var groupParticipant in groupParticipants) {
                        var participant = await this.databaseConnection.Table<Participant>().Where(participant => participant.Id == groupParticipant.ParticipantId).FirstOrDefaultAsync();
                        groupToAdd.Participants.Add(participant.Id);
                        groupToAdd.ParticipantNames.Add(participant.Name);
                    }
                    groupToAdd.RoomId = localGroup.RoomId;
                    groupToAdd.NumberNewMessages = localGroup.NumberNewMessages;
                    groupToAdd.CreatorId = localGroup.CreatorId;
                    groupToAdd.GroupName = localGroup.GroupName;
                    groupToAdd.GroupPictureUrl = localGroup.GroupPictureUrl;

                    groups.Add(groupToAdd);
                }
                return groups;
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public async Task<bool> UpdateLocalGroups(List<GroupModel> remoteGroups) {
            foreach (var group in remoteGroups) {
                //Insert the group in the database
                Group newGroup = new Group { CreatorId = group.CreatorId, GroupName = group.GroupName, GroupPictureUrl = group.GroupPictureUrl, LastMessage = group.LastMessage, LastMessageSender = group.LastMessageSender, LastMessageTimestamp = group.LastMessageTimestamp, NumberNewMessages = "0", RoomId = group.RoomId };
                _ = await this.databaseConnection.InsertAsync(newGroup);
                //Add the participants in the group
                for (int i = 0; i < group.Participants.Count; i++) {
                    //Add in the participants table if the user is not already registered in other group
                    Participant participant = new Participant { Id = group.Participants[i], Name = group.ParticipantNames[i] };
                    _ = await this.databaseConnection.InsertOrReplaceAsync(participant);
                    //Add the association between the group and the participant
                    GroupParticipant groupParticipant = new GroupParticipant { GroupId = group.RoomId, ParticipantId = group.Participants[i] };
                    _ = await this.databaseConnection.InsertAsync(groupParticipant);
                }
            }
            return true;
        }

        public async Task<bool> SaveGroup(GroupModel group) {
            //Insert the group in the database
            Group newGroup = new Group { CreatorId = group.CreatorId, GroupName = group.GroupName, GroupPictureUrl = group.GroupPictureUrl, LastMessage = group.LastMessage, LastMessageSender = group.LastMessageSender, LastMessageTimestamp = group.LastMessageTimestamp, NumberNewMessages = "0", RoomId = group.RoomId };
            _ = await this.databaseConnection.InsertAsync(newGroup);
            //Add the participants in the group
            for (int i = 0; i < group.Participants.Count; i++) {
                //Add in the participants table if the user is not already registered in other group
                Participant participant = new Participant { Id = group.Participants[i], Name = group.ParticipantNames[i] };
                _ = await this.databaseConnection.InsertOrReplaceAsync(participant);
                //Add the association between the group and the participant
                GroupParticipant groupParticipant = new GroupParticipant { GroupId = group.RoomId, ParticipantId = group.Participants[i] };
                _ = await this.databaseConnection.InsertAsync(groupParticipant);
            }
            return true;
        }

        //Get the number of new messages in a group
        public async Task<int> GetNumberNewMessagesForGroup(int roomId) {
            var group = await this.databaseConnection.Table<Group>().Where(group => group.RoomId == roomId).FirstOrDefaultAsync();
            return group.NumberNewMessages == null ? 0 : int.Parse(group.NumberNewMessages);
        }

        //Get the number of messages send by the user in the group
        public async Task<int> GetNumberUserSentMessages(int roomId, int accountId) {
            var roomMessages = await this.databaseConnection.Table<RoomMessage>().Where(roomMessage => roomMessage.RoomId == roomId).ToListAsync();
            int count = 0;
            foreach(var roomMessage in roomMessages) {
                var message = await this.databaseConnection.Table<Message>().Where(message => message.Id == roomMessage.MessageId).FirstOrDefaultAsync();
                if(message.Owner == accountId)
                    count++;
            }
            return count;
        }

        //Save the new messages for a group in the database
        public async Task<bool> UpdateNewMessagesForGroup(int roomId, int newNumberMessages) {
            var group = await this.databaseConnection.Table<Group>().Where(group => group.RoomId == roomId).FirstOrDefaultAsync();
            group.NumberNewMessages = newNumberMessages.ToString();
            _ = await this.databaseConnection.UpdateAsync(group);
            return true;
        }

        //Get the participant name from id
        public async Task<string> GetParticipantName(int participantId) {
            var participant = await this.databaseConnection.Table<Participant>().Where(participant => participant.Id == participantId).FirstOrDefaultAsync();
            return participant == null ? null : participant.Name;
        }

        //Get the participants of the group
        public async Task<List<Participant>> GetGroupParticipants(int roomId) {
            return await this.databaseConnection.Table<Participant>().ToListAsync();
        }

        //Get participant keys for the group
        public async Task<Keys> GetParticipantKeysForGroup(int roomId, int participantId) {
            var participantKeys = await this.databaseConnection.Table<ParticipantKey>().Where(participantKey => participantKey.ParticipantId == participantId).ToListAsync();
            foreach(var participantKey in participantKeys) {
                var key = await this.databaseConnection.Table<Keys>().Where(key => key.KeyId == participantKey.KeyId).FirstOrDefaultAsync();
                if (key.RoomId == roomId)
                    return key;
            }
            return null;
        }

        //Save the new messages in the database
        public async Task<bool> UpdateNewMessagesForFriend(int friendId, int newNumberMessages) {
            var friend = await this.databaseConnection.Table<Friend>().Where(friend => friend.Id == friendId).FirstOrDefaultAsync();
            friend.NumberNewMessages = newNumberMessages.ToString();
            _ = await this.databaseConnection.UpdateAsync(friend);
            return true;
        }

        //Save key value data in the corresponding table
        public async Task<bool> SaveKeyValueData(string key, string value, DateTime expirationDate) {
            //Check if the values are correct
            if (key == null || value == null)
                return false;

            //Check if the date is not before the current date
            if (expirationDate <= DateTime.Now)
                return false;

            await this.InitDatabase();

            _ = await this.databaseConnection.InsertAsync(new KeyValue { Key = key, Value = value, ExpirationDate = expirationDate});
            return true;
        }

        //Save key value data with default expiration date current timestamp plus one day
        public async Task<bool> SaveKeyValueData(string key, string value) {
            //Check if the values are correct
            if (key == null || value == null)
                return false;

            DateTime expirationDate = DateTime.Now.AddDays(1);
            await this.InitDatabase();

            _ = await this.databaseConnection.InsertOrReplaceAsync(new KeyValue { Key = key, Value = value, ExpirationDate = expirationDate });
            return true;
        }

        //Get the cached entry with the specified key
        public async Task<string> GetCachedEntry(string key) {
            if(key == null)
                return null;

            await this.InitDatabase();
            int count = await this.databaseConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM keyvalue");
            if (count == 0)
                return null;
            try {
                KeyValue result = await this.databaseConnection.GetAsync<KeyValue>(key);
                return result.Value;
            } catch(Exception ex) {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        //Delete cached data that expired
        public async Task<bool> DeleteExpiredCachedData() {
            await this.InitDatabase();

            //Get all the cached entries in the database
            var cachedData = await this.databaseConnection.Table<KeyValue>().ToListAsync();
            foreach(KeyValue kv in cachedData) {
                _ = await this.databaseConnection.DeleteAsync(kv);
            }

            //Return the result
            return true;
        }

        //Delete all groups
        public async Task<bool> DeleteAllGroups() {
            await this.InitDatabase();
            _ = await this.databaseConnection.DeleteAllAsync<Participant>();
            _ = await this.databaseConnection.DeleteAllAsync<GroupParticipant>();
            _ = await this.databaseConnection.DeleteAllAsync<Group>();
            return true;
        }
    }
}
