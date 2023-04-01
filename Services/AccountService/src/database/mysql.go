package database

import (
	"database/sql"
	"errors"
	"time"
	"willow/accountservice/data"
	"willow/accountservice/logging"

	_ "github.com/go-sql-driver/mysql"
)

/*
 * The Connection structure will hold the handle for the mysql database (this handle does not guarantee that the connection has been made)
 * The structure will also have a reference to the server logger so that messages can be logged if anything happens
 */
type Connection struct {
	l  logging.ILogger
	db *sql.DB
}

/*
 * This function will create a new Connection structure that will be used throughout the server execution
 * It will not initialize the mysql database handle, for that the InitializeConnection function should be called
 * using the returned object
 */
func NewConnection(l logging.ILogger) *Connection {
	return &Connection{l: l, db: nil}
}

/*
 * This function will initialize the connection handle to the database, it will also set max connection lifetime,
 * max idle connections and max open connections (the handle can have multiple connections at the same time)
 * If an error occurs then it will be returned from the function
 * Else the function will return nil
 */
func (conn *Connection) InitializeConnection() error {
	dbUser := "root"
	dbPassword := "" //Password not required
	dbHost := "localhost"
	dbName := "willow"
	dbConn, err := sql.Open("mysql", dbUser+":"+dbPassword+"@tcp("+dbHost+":3306)/"+dbName)
	//dbConn, err := sql.Open("mysql", "root:@tcp(127.0.0.1:3306)/test")
	if err != nil {
		conn.l.Error(err.Error())
		return err
	}
	conn.db = dbConn

	conn.db.SetConnMaxLifetime(time.Minute * 3)
	conn.db.SetMaxIdleConns(10)
	conn.db.SetMaxOpenConns(10)

	return err
}

/*
 * This function will check the connection to the mysql server
 * If an error occurs then it will be returned
 * Else nil will be returned
 */
func (conn *Connection) TestConnection() error {
	return conn.db.Ping()
}

/*
 * This function will close the connection to the mysql server
 * If an error occurs during the closing process then it will be returned
 * Else nil will be returned
 */
func (conn *Connection) CloseConnection() error {
	conn.l.Info("Closed the connection to the database")
	return conn.db.Close()
}

//--------------------Functions for handling database insert, update, delete, select--------------------

/*
 * This function checks if the username passed as an argument already exists in the database
 * It will return an error if the string already exists
 * Else it will return nil
 */
func (conn *Connection) checkUsernameExists(username string) (bool, error) {
	//Prepare the statement that will check if the username exists
	stmtCheck, err := conn.db.Prepare("SELECT Username FROM accounts WHERE Username = ?")

	if err != nil {
		conn.l.Error("Error occured when preparing the select for username checking")
		return true, err
	}
	//Close the statement at the end of the function
	defer stmtCheck.Close()

	//Execute the statement to determine if the username already exists
	res := stmtCheck.QueryRow(username)
	if res.Err() != nil {
		//An error occured during the select statement
		conn.l.Error("Error occured when executing select for username check ", err.Error())
		return true, res.Err()
	}

	//Check if any rows are returned from the statement using scan
	var selUsername string
	err = res.Scan(&selUsername)
	if err != nil && err != sql.ErrNoRows {
		//Another error than the one we are looking for occured
		conn.l.Error("Error occured when fetching the rows in the select ", err.Error())
		return true, err
	}

	if selUsername != "" {
		//Username already exists in the database
		return true, nil
	}

	//There are no rows returned from the database which means that the username does not exist
	return false, nil
}

/*
 * This function will check if the email already exists in the database
 * If an error occurs during the operations with the database then an error will be returned
 * Else it will check if there are any rows in the database and based on that will return
 * true if the email exists or false if the email does not exist
 */
func (conn *Connection) checkEmailExists(email string) (bool, error) {
	//Prepare the statement that will check if the email exists
	stmtCheck, err := conn.db.Prepare("SELECT Email FROM accounts WHERE Email = ?")
	if err != nil {
		conn.l.Error("Error occured when fetching the rows in the select!")
		return true, err
	}
	//Close the statement object when the function finishes execution
	defer stmtCheck.Close()

	//Execute the statement to check if the email aready exists
	res := stmtCheck.QueryRow(email)
	if res.Err() != nil {
		//An error occured when executing the select statement
		conn.l.Error("Error occured when executing select for email check ", res.Err())
		return true, res.Err()
	}
	var selEmail string
	err = res.Scan(&selEmail)
	if err != nil && err != sql.ErrNoRows {
		//An error other than the expected one occured
		conn.l.Error("Error occured when fetching the rows in the select ", err.Error())
		return true, err
	}

	if selEmail != "" {
		//Username already exists in the database
		return true, nil
	}

	//There are no rows returned from the database which means that the email does not exist
	return false, nil
}

/*
 * This function will insert a new account in the database using the data passed in the parameter structure.
 * If an error occurs during the insertion of the account in the database then it will be returned (when the
 * Account structure enters this function it is supposed to be correct)
 * Else the account will be inserted in the database and nil will be returned
 */
func (conn *Connection) InsertAccount(acc *data.Account) error {
	//Check if the username already exists in the database
	res, err := conn.checkUsernameExists(acc.Username)
	if err != nil {
		return err
	}

	//If res is true then the username already exists
	if res {
		//Username already exists in the database
		return errors.New("username already exists")
	}

	//Check if the email already exists in the database
	res, err = conn.checkEmailExists(acc.Email)
	if err != nil {
		return err
	}

	//If the res is true it means that the email already exists
	if res {
		//Email already exists in the database
		return errors.New("email already exists")
	}

	//Prepare the query that will be executed with the values from the acc structure
	stmtInsert, err := conn.db.Prepare("INSERT INTO accounts (Username, DisplayName, Email, PasswordHash, Salt, IdentityPublicKey, PreSignedPublicKey) VALUES (?, ?, ?, ?, ?, ?, ?)")
	if err != nil {
		conn.l.Error("Error occured when preparing the insert account query", err.Error())
		return err
	}
	//Close the statment at the end of the function
	defer stmtInsert.Close()

	//Execute the query with the data in the structure
	_, err = stmtInsert.Exec(acc.Username, acc.DisplayName, acc.Email, acc.PasswordHash, acc.Salt, acc.IdentityPublicKey, acc.PreSignedPublicKey)
	if err != nil {
		conn.l.Error("Error occured when executing the insert account query", err.Error())
		return err
	}

	return nil
}

/*
 * This function will get the salt the user with the username specified in the parameter
 * If an error occurs during the retrieving of the salt it will be returned and the salt stirng will be empty
 * If no errors occur the the erorr will be nil and the salt will be returned
 */
func (conn *Connection) GetSalt(username string) (string, error) {
	//Prepare the select statement for extracting the salt
	stmtSalt, err := conn.db.Prepare("SELECT salt FROM accounts where Username = ?")
	if err != nil {
		//An error occured when preparing the statement for salt selection
		conn.l.Error("Error occured during select for salt ", err.Error())
		return "", err
	}
	//Close the statment for salt when the function finishes
	defer stmtSalt.Close()

	//Execute the query for selecting the salt with the username
	row := stmtSalt.QueryRow(username)
	if row.Err() != nil {
		//An error occured during the select statement
		conn.l.Error("Error occured during select for salt ", err.Error())
		return "", row.Err()
	}

	//Extract the salt
	var salt string
	err = row.Scan(&salt)
	if err != nil && err == sql.ErrNoRows {
		//The salt could not be retrieved which means the username does not exist
		return "", errors.New("invalid credentials")
	}
	if err != nil {
		//Another error occured
		conn.l.Error("Error occured when executing the select for salt ", row.Err().Error())
		return "", err
	}

	//The salt can be extract so return it
	return salt, nil
}

/*
 * This function will check if the username and password specified by the user
 * can be found in the database. If they are found it means that the user logged in succesfully
 * If they cannot be found then the credentials are invalid
 * The function will return an account if the login was successful
 * Else it will return nil and an error
 */
func (conn *Connection) LoginIntoAccount(username string, passwordHash string) (*data.Account, error) {
	//Prepare the statement that will check if the credentials are correct
	stmtSelect, err := conn.db.Prepare("SELECT accounts.ID, accounts.Username, accounts.DisplayName, accounts.Email, accounts.JoinDate, accounts.LastOnline, accountstatus.Status, accounts.ProfilePictureUrl, accounts.About FROM accounts INNER JOIN accountstatus ON accountstatus.ID = accounts.Status WHERE accounts.Username = ? and accounts.PasswordHash = ?")
	if err != nil {
		//An error occured during the preparation of the select query
		conn.l.Error("Error occured when preparing select for login ", err.Error())
		return nil, err
	}
	//Close the statement after the function finishes
	defer stmtSelect.Close()

	//Execute the select statment
	res := stmtSelect.QueryRow(username, passwordHash)
	//Check if an error occured during the select statment
	if res.Err() != nil {
		//An error occured during the select statment
		conn.l.Error("Error occured when executing select for login ", res.Err().Error())
		return nil, res.Err()
	}

	//The select statement was successful, now retieve the data
	acc := &data.Account{}
	err = res.Scan(&acc.ID, &acc.Username, &acc.DisplayName, &acc.Email, &acc.JoinDate, &acc.LastOnline, &acc.Status, &acc.ProfilePictureUrl, &acc.About)
	//Check if an error occured during data retrieve
	if err != nil && err == sql.ErrNoRows {
		//An error occured but it is sql.ErrNoRows which means that the login failed (invalid credentials)
		return nil, errors.New("invalid credentials")
	}
	if err != nil {
		//Another error occured
		conn.l.Error("Error occured when fetching data for login ", err.Error())
		return nil, err
	}

	//The user logged in successfully
	return acc, nil
}

/*
 * This function will get the public identity key of the account
 */
func (conn *Connection) GetAccountIdentityPublicKey(username string) (string, error) {
	//Prepare the select statement for getting the public key
	stmtSelect, err := conn.db.Prepare("SELECT IdentityPublicKey FROM accounts where Username = ?")
	//Check if an error occured when preparing the sql statment for getting the identity public key
	if err != nil {
		conn.l.Error("Error occured when preparing the select statement for identity public key", err.Error())
		return "", err
	}
	//Execute the select statement
	res := stmtSelect.QueryRow(username)
	//Check if an error occured when executing the select statement
	if res.Err() != nil {
		conn.l.Error("Error occured when executing the select statement for identity public key", err.Error())
		return "", err
	}
	//Get the public key from the result
	var identityPublic string = ""
	err = res.Scan(&identityPublic)
	//Check if an error occured when getting the data
	if err != nil {
		conn.l.Error("Error occured when scanning the data for identity public key", err.Error())
		return "", err
	}
	return identityPublic, err
}

/*
 * This function will validate if the jwt data represents and account from the database
 * It will check that the ID, Email and DisplayName can be found on an account
 * If there is no such account return an error, else return nil
 */

func (conn *Connection) ValidateJWTPayload(ID int, DisplayName string) error {
	//Prepare the query that will be executed
	sqlStmt, err := conn.db.Prepare("SELECT Username FROM accounts WHERE ID = ? AND DisplayName = ?")
	if err != nil {
		conn.l.Error("Error occured when preparing select for jwt validation", err.Error())
		return err
	}
	defer sqlStmt.Close()

	res := sqlStmt.QueryRow(ID, DisplayName)
	if res.Err() != nil {
		//An error occured during the select statement
		conn.l.Error("Error occured when executing select for jwt validation ", res.Err().Error())
		return res.Err()
	}

	//Check if any rows are returned from the statement using scan
	var selUsername string
	err = res.Scan(&selUsername)
	if err != nil && err != sql.ErrNoRows {
		//Another error than the one we are looking for occured
		conn.l.Error("Error occured when fetching the rows in the jwt validation select ", err.Error())
		return err
	}

	if selUsername == "" {
		return errors.New("account does not exist")
	}

	return nil
}

/*
 * This function will update the status of the account with the new status specified
 * If there are no errors and the status has been update then nil will be returned
 * Else an error will be returned
 */
func (conn *Connection) UpdateStatus(ID int, status string) error {
	//Prepare the update query
	updStmt, err := conn.db.Prepare("UPDATE accounts SET Status = (SELECT ID FROM accountstatus WHERE Status = ?) WHERE ID = ?")
	if err != nil {
		conn.l.Error("Error occured during the preparation of the update status query", err.Error())
		return err
	}

	res, err := updStmt.Exec(status, ID)
	if err != nil {
		conn.l.Error("Error occured during execution of update status query", err.Error())
		return err
	}

	rowsAffected, err := res.RowsAffected()
	if err != nil {
		conn.l.Error("Error occured during fetching the number of rows affected of update status query", err.Error())
		return err
	}

	if rowsAffected > 1 {
		conn.l.Error("Number of rows affected during status update is not 1")
		return errors.New("number of rows affected during status update is not 1")
	}

	//If the new status is offline then update last online with current date
	if status == "Offline" {
		updLastOnline, err := conn.db.Prepare("UPDATE accounts SET LastOnline = current_timestamp() WHERE ID = ?")
		if err != nil {
			conn.l.Error("Error occured during the preparation of the update status query", err.Error())
			return err
		}

		res, err := updLastOnline.Exec(ID)
		if err != nil {
			conn.l.Error("Error occured during execution of update status query", err.Error())
			return err
		}

		rowsAffected, err := res.RowsAffected()
		if err != nil {
			conn.l.Error("Error occured during fetching the number of rows affected of update status query", err.Error())
			return err
		}

		if rowsAffected > 1 {
			conn.l.Error("Number of rows affected during status update is not 1")
			return errors.New("number of rows affected during status update is not 1")
		}
	}

	return nil
}

/*
 * This function will get the details of an account (DisplayName, Status, Last-Online)
 */
func (conn *Connection) GetAccountDetails(accountID int64) (*data.Account, error) {
	//Prepare the statement that will select the account details from the database
	stmtSelect, err := conn.db.Prepare("SELECT accounts.ID, accounts.DisplayName, accounts.JoinDate, accounts.LastOnline, accountstatus.Status, accounts.ProfilePictureUrl, accounts.About, accounts.IdentityPublicKey, accounts.PreSignedPublicKey FROM accounts INNER JOIN accountstatus ON accountstatus.ID = accounts.Status WHERE accounts.ID = ?")
	//Check if an error occured during the preparation of the select statement
	if err != nil {
		//An error occured during the preparation of the select statment
		conn.l.Error("Error occured during the select statment preparation", err.Error())
		return nil, err
	}
	defer stmtSelect.Close()

	//Add the query parameter
	res := stmtSelect.QueryRow(accountID)
	if res.Err() != nil {
		//An error occured during the select statement
		conn.l.Error("Error occured when executing select for jwt validation ", res.Err().Error())
		return nil, res.Err()
	}

	//Check if any rows are returned from the statement using scan
	account := &data.Account{}
	err = res.Scan(&account.ID, &account.DisplayName, &account.JoinDate, &account.LastOnline, &account.Status, &account.ProfilePictureUrl, &account.About, &account.IdentityPublicKey, &account.PreSignedPublicKey)
	if err != nil && err != sql.ErrNoRows {
		//Another error than the one we are looking for occured
		conn.l.Error("Error occured when fetching the rows in the jwt validation select ", err.Error())
		return nil, err
	}

	if account.DisplayName == "" {
		return nil, errors.New("account does not exist")
	}

	//conn.l.Info(*account)
	return account, nil
}

/*
 * This function will insert a new bug report into the database
 */
func (conn *Connection) InsertBugReport(bugReport data.AddBugReport) error {
	//Prepare the statement for inserting the bug report into the database
	sqlStmt, err := conn.db.Prepare("INSERT INTO bugreports(CategoryId, Description, ReportedBy) VALUES ( (SELECT Id FROM bugreportcategories where Name = ?), ?, ?)")
	//Check if there was any error when preparing the statement
	if err != nil {
		conn.l.Error("Error occured when preparing the statement for bug report insert", err.Error())
		return err
	}

	//Execute the statement which the values received from the user
	_, err = sqlStmt.Exec(bugReport.Category, bugReport.Description, bugReport.AccountId)
	//Check if an error occured when executing the insert query
	if err != nil {
		conn.l.Error("Error occured when executing the query for bug report insert")
		return err
	}

	return nil
}

/*
 * This function will return all the categories available for a bug report
 */
func (conn *Connection) GetAllCategories() (data.ReportCategories, error) {
	//Prepare the select statement for the categories of bug reports
	sqlStmt, err := conn.db.Prepare("SELECT Name FROM bugreportcategories")
	//Check if an error occured
	if err != nil {
		conn.l.Error("Error occured when preparing the statement for bug report categories", err.Error())
		return nil, err
	}
	rows, err := sqlStmt.Query()
	//Check if an error occured
	if err != nil {
		conn.l.Error("Error occured when executing the statement for bug report categories", err.Error())
		return nil, err
	}

	//Get all the categories into an array
	returnArr := make(data.ReportCategories, 0)
	for rows.Next() {
		var name string
		_ = rows.Scan(&name)
		returnArr = append(returnArr, data.ReportCategory{Name: name})
	}

	return returnArr, nil
}

/*
 * This function will update the profile picture URL to the new value after the file has been uploaded on the server
 */
func (conn *Connection) UpdateProfilePictureURL(accountId int64, url string) error {
	//Prepare the statement for updating the profile picture url
	conn.l.Debug("New url: ", url)
	stmtUpdate, err := conn.db.Prepare("UPDATE accounts SET ProfilePictureUrl = ? WHERE ID = ?")
	if err != nil {
		conn.l.Error("Error occured when updating the profile picture URL", err.Error())
		return err
	}
	//Execute the update query
	_, err = stmtUpdate.Exec(url, accountId)
	if err != nil {
		conn.l.Error("Error occured when executing the update profile picture URL query", err.Error())
		return err
	}

	return nil
}

/*
 * This function will update the about text of the account specified by the account id
 */
func (conn *Connection) UpdateAbout(accountId int64, newAbout string) (bool, error) {
	//Prepare the update statement for updating the about field
	stmtUpdate, err := conn.db.Prepare("UPDATE accounts SET About = ? WHERE ID = ?")
	//Check if an error occured during the preparation of update
	if err != nil {
		conn.l.Error("Error occured when preparing the update statement", err.Error())
		return false, err
	}
	//Execute the update
	_, err = stmtUpdate.Exec(newAbout, accountId)
	//Check if an error occured when executing the update statement
	if err != nil {
		conn.l.Error("Error occured when executing the update statement", err.Error())
		return false, err
	}
	//Return that everything went ok
	return true, nil
}

/*
 * This function will get all users from the database
 */
func (conn *Connection) GetUsers(accountId int64) (data.Accounts, error) {
	//Prepare the select statement to get all the users
	stmtSelect, err := conn.db.Prepare("SELECT accounts.ID, accounts.DisplayName, accounts.JoinDate, accountstatus.Status, accounts.ProfilePictureUrl, accounts.About FROM accounts INNER JOIN accountstatus ON accountstatus.ID = accounts.Status WHERE accounts.ID != ?")
	//Check if an error occured when preparing the statement
	if err != nil {
		conn.l.Error("Error occured when preparing the select statement for getting users", err.Error())
		return make(data.Accounts, 0), err
	}
	//Execute the select statement
	rows, err := stmtSelect.Query(accountId)
	//Check if an error occured when executing the select statement
	if err != nil {
		conn.l.Error("Error occured when executing the select statement for getting users", err.Error())
		return make(data.Accounts, 0), err
	}
	//Get all the users from the rows
	users := make(data.Accounts, 0)
	for rows.Next() {
		user := data.Account{}
		err = rows.Scan(&user.ID, &user.DisplayName, &user.JoinDate, &user.Status, &user.ProfilePictureUrl, &user.About)
		//Check if an error occured when fetching data from the row
		if err != nil {
			conn.l.Error("Error occurred when fetching data from row", err.Error())
			return make(data.Accounts, 0), err
		}
		users = append(users, user)
	}
	return users, nil
}
