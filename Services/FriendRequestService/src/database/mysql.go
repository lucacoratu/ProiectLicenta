package database

import (
	"database/sql"
	"errors"
	"time"
	"willow/friendrequestservice/data"
	"willow/friendrequestservice/logging"

	_ "github.com/go-sql-driver/mysql"
)

/*
 * The Connection structure will hold the handle for the mysql database (this handle does not guarantee that the connection has been made)
 * The structure will also have a reference to the server logger so that messages can be logged if anything happens
 */
type Connection struct {
	logger logging.ILogger
	db     *sql.DB
}

/*
 * This function will create a new Connection structure that will be used throughout the server execution
 * It will not initialize the mysql database handle, for that the InitializeConnection function should be called
 * using the returned object
 */
func NewConnection(l logging.ILogger) *Connection {
	return &Connection{logger: l, db: nil}
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
	dbName := "friendrequestsdb"
	dbConn, err := sql.Open("mysql", dbUser+":"+dbPassword+"@tcp("+dbHost+":3306)/"+dbName)
	//dbConn, err := sql.Open("mysql", "root:@tcp(127.0.0.1:3306)/test")
	if err != nil {
		conn.logger.Error(err.Error())
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
	conn.logger.Info("Closed the connection to the database")
	return conn.db.Close()
}

/*
 * This function will add a friend request into the database
 * If the insertion failed then it will return an error
 * Else nil will be returned
 */
func (conn *Connection) AddFriendRequest(accID int, senderID int) error {
	//TO DO... Check if the friend request does not exist already

	//Prepare the statement for inserting the friend request
	stmtInsert, err := conn.db.Prepare("INSERT INTO FriendRequests(AccountID, SenderID) VALUES(?, ?)")
	//Check if an error occured during the preparation of the statement
	if err != nil {
		//An error occured during statement preparation
		conn.logger.Error("Friend request insert statement prepare", err.Error())
		return err
	}
	defer stmtInsert.Close()
	//Execute the statement with the accID and the senderID
	res, err := stmtInsert.Exec(accID, senderID)
	//Check if an error occured during the execution of the statement
	if err != nil {
		//An error occured during statment execution
		conn.logger.Error("Friend request insert statement execute", err.Error())
		return err
	}

	//Check if there was only one row affected
	rowsAff, err := res.RowsAffected()
	//Check if an error occured during the fetching of rows affected
	if err != nil {
		//An error occured during rows affected fetch
		conn.logger.Error("Friend request insert statement execute", err.Error())
		return err
	}

	//Check rows affected is equal to 1
	if rowsAff != 1 {
		conn.logger.Error("Insert friend request failed, 0 or multiple rows affected ", rowsAff)
		return errors.New("insert error (0 or multiple rows)")
	}

	//The insert statment was successful
	return nil
}

/*
 * This function will delete a friend request from the database
 * If an error occurs during the deletion process then an error will be returned
 * Else nil will be returned
 */
func (conn *Connection) DeleteFriendRequest(accID int, senderID int) error {
	//Prepare the statment for deletion
	stmtDelete, err := conn.db.Prepare("DELETE FROM FriendRequests WHERE AccountID = ? AND SenderID = ?")
	//Check if an error occured during statment preparation
	if err != nil {
		//An error occured during statement preparation
		conn.logger.Error("Friend request delete statment prepare", err.Error())
		return err
	}
	defer stmtDelete.Close()
	//Execute the delete statement
	res, err := stmtDelete.Exec(accID, senderID)
	//Check if an error occured during statement execution
	if err != nil {
		conn.logger.Error("Friend request delete statement execute", err.Error())
		return err
	}
	//Get the number of rows affected
	rowsAff, err := res.RowsAffected()
	//Check if an error occured during rows affected fetch
	if err != nil {
		//An error occured during rows affected fetch
		conn.logger.Error("Friend request delete rows affected fetch", err.Error())
		return err
	}
	//Check that only one row was affected
	if rowsAff != 1 {
		//The number of rows affected is not equal to one (something happended)
		conn.logger.Error("Friend request delete rows affected not equal to 1", rowsAff)
		return errors.New("rows affected not equal to 1")
	}
	//The delete was succesful
	return nil
}

/*
 * This function will get all the friend requests of an user
 * It will return a list of friendrequests and nil for the error, if there are no errors
 * Else it will return nil and an error
 */
func (conn *Connection) ViewFriendRequests(accID int) (data.ViewFriendRequests, error) {
	//Prepare the select statement for the friend requests
	stmtSelect, err := conn.db.Prepare("SELECT AccountID, SenderID, RequestDate FROM FriendRequests WHERE AccountID = ?")
	//Check if an error occured during select statmenet preparation
	if err != nil {
		conn.logger.Error("Friend request select statment prepare", err.Error())
		return nil, err
	}
	defer stmtSelect.Close()
	//Execute the select statment
	rows, err := stmtSelect.Query(accID)
	//Check if an error occured during the select statement
	if err != nil {
		conn.logger.Error("Friend request select statment execute", err.Error())
		return nil, err
	}
	//Extract the data from the rows
	friendRequests := make(data.ViewFriendRequests, 0)
	for rows.Next() {
		friendRequest := new(data.ViewFriendRequest)
		err = rows.Scan(&friendRequest.AccID, &friendRequest.SenderID, &friendRequest.RequestDate)
		if err != nil {
			conn.logger.Error("Friend request select statment scan", err.Error())
			return nil, err
		}
		friendRequests = append(friendRequests, *friendRequest)
	}

	return friendRequests, nil
}
