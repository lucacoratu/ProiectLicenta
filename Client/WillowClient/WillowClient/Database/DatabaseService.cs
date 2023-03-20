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
                    BefriendDate = DateTime.Now,
                    JoinDate = DateTime.Parse(friend.JoinDate),
                    DisplayName = friend.DisplayName,
                    LastOnline = DateTime.Parse(friend.LastOnline),
                    Status = friend.Status,
                    RoomID = friend.RoomID,
                    LastMessage = friend.LastMessage,
                    LastMessageTimestamp = DateTime.Parse(friend.LastMessageTimestamp),
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

        //Get the cached friends from the database
        public async Task<List<Friend>> GetLocalFriends() {
            await this.InitDatabase();
            return await this.databaseConnection.Table<Friend>().ToListAsync();
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
