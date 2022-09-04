package database

import (
	"database/sql"
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
 * This function will insert a new account in the database using the data passed in the parameter structure.
 * If an error occurs during the insertion of the account in the database then it will be returned (when the
 * Account structure enters this function it is supposed to be correct)
 * Else the account will be inserted in the database and nil will be returned
 */
func (conn *Connection) InsertAccount(acc *data.Account) error {
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
