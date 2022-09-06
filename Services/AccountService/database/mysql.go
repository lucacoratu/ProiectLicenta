package database

import (
	"database/sql"
	"errors"
	"log"
	"time"
	"willow/accountservice/data"

	_ "github.com/go-sql-driver/mysql"
)

/*
 * The Connection structure will hold the handle for the mysql database (this handle does not guarantee that the connection has been made)
 * The structure will also have a reference to the server logger so that messages can be logged if anything happens
 */
type Connection struct {
	l  *log.Logger
	db *sql.DB
}

/*
 * This function will create a new Connection structure that will be used throughout the server execution
 * It will not initialize the mysql database handle, for that the InitializeConnection function should be called
 * using the returned object
 */
func NewConnection(l *log.Logger) *Connection {
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
	dbName := "accountsdb"
	dbConn, err := sql.Open("mysql", dbUser+":"+dbPassword+"@tcp("+dbHost+":3306)/"+dbName)
	//dbConn, err := sql.Open("mysql", "root:@tcp(127.0.0.1:3306)/test")
	if err != nil {
		conn.l.Println(err.Error())
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
	conn.l.Println("Closed the connection to the database")
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
		conn.l.Println("Error occured when preparing the select for username checking")
		return true, err
	}
	//Close the statement at the end of the function
	defer stmtCheck.Close()

	//Execute the statement to determine if the username already exists
	res := stmtCheck.QueryRow(username)
	if res.Err() != nil {
		//An error occured during the select statement
		conn.l.Println("Error occured when executing select for username check ", err.Error())
		return true, res.Err()
	}

	//Check if any rows are returned from the statement using scan
	var selUsername string
	err = res.Scan(&selUsername)
	if err != nil && err != sql.ErrNoRows {
		//Another error than the one we are looking for occured
		conn.l.Print("Error occured when fetching the rows in the select ", err.Error())
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
		conn.l.Println("Error occured when fetching the rows in the select!")
		return true, err
	}
	//Close the statement object when the function finishes execution
	defer stmtCheck.Close()

	//Execute the statement to check if the email aready exists
	res := stmtCheck.QueryRow(email)
	if res.Err() != nil {
		//An error occured when executing the select statement
		conn.l.Println("Error occured when executing select for email check ", res.Err())
		return true, res.Err()
	}
	var selEmail string
	err = res.Scan(&selEmail)
	if err != nil && err != sql.ErrNoRows {
		//An error other than the expected one occured
		conn.l.Print("Error occured when fetching the rows in the select ", err.Error())
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
	stmtInsert, err := conn.db.Prepare("INSERT INTO accounts (Username, DisplayName, Email, PasswordHash, Salt) VALUES (?, ?, ?, ?, ?)")
	if err != nil {
		conn.l.Println("Error occured when preparing the insert account query")
		return err
	}
	//Close the statment at the end of the function
	defer stmtInsert.Close()

	//Execute the query with the data in the structure
	_, err = stmtInsert.Exec(acc.Username, acc.DisplayName, acc.Email, acc.PasswordHash, acc.Salt)
	if err != nil {
		conn.l.Println("Error occured when executing the insert account query")
		return err
	}

	return nil
}
