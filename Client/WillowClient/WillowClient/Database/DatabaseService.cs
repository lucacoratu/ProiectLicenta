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
    }
}
