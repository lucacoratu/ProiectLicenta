using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using WillowClient.Model;
using WillowClient.Database.Model;

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

            //Create the friend_message table where corelations between room and messages will be cached
            _ = await this.databaseConnection.CreateTableAsync<RoomMessage>();

            //Create the messages table where all messages will be cached
            _ = await this.databaseConnection.CreateTableAsync<Message>();

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
            return true;
        }

        public async Task<bool> SaveMessageInTheDatabase(PrivateMessageModel message, int roomId ,string senderName) {
            //Insert the message in the corresponding table
            Message msgDb = new Message { Id = message.Id, EphemeralPublic = message.EphemeralPublicKey, IdentityPublic = message.IdentityPublicKey, Owner = message.SenderId, SenderName = senderName, Text = message.Data, TimeStamp = DateTime.Now };
            _ = await this.databaseConnection.InsertAsync(msgDb);
            //Insert the room and message corelation
            RoomMessage roomMessage = new RoomMessage { MessageId = message.Id, RoomId = roomId };
            _ = await this.databaseConnection.InsertAsync(roomMessage);
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
    }
}
