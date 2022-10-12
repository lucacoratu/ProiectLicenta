package database

import (
	"database/sql"
	"errors"
	"time"
	"willow/friendservice/data"
	"willow/friendservice/logging"

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
	dbName := "friendsdb"
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
 * This function willcheck if the 2 accounts that want to be friends already are friends
 * This can be done by checking if the following cases can be found in the database (accountID, friendID) or (friendID, accountID)
 * If the friendship already exists then true will be returned and nil for the error
 * If the friendship does not exist then false will be returned and nil for the error
 * If an error occurs during the check then true and not nil (for the error) will be returned
 */
func (conn *Connection) checkFriendshipExists(accountID int, friendID int) (bool, error) {
	//Prepare the statement that will check if the friendship exists
	stmtSelect, err := conn.db.Prepare("SELECT * FROM Friendships WHERE (AccountID = ? AND FriendID = ?) OR (FriendID = ? AND AccountID = ?)")
	//Check if an error occured during the preparation of the statement
	if err != nil {
		//An error occured during statement preparation
		conn.logger.Error("Error occured during friendship check statement preparation", err.Error())
		return true, err
	}
	defer stmtSelect.Close()
	//Execute the query
	rows, err := stmtSelect.Query(accountID, friendID, accountID, friendID)
	//Check if an error occured during statement execution
	if err != nil {
		//An error occured during statement execution
		conn.logger.Error("Error occured during friendship check statement execution", err.Error())
		return true, err
	}
	//Check if there are any rows returned from the query (if there are rows returned then the friendship already exists)
	if rows.Next() {
		//The friendship already exists
		conn.logger.Error("Friendship already exists")
		return true, nil
	}

	//The friendship does not already exist
	return false, nil
}

/*
 * This function will add a new friend in the database, it requires the account id and the friend ID
 * If the insert statement succeded then nil will be returned
 * If an error occurs during the insertion of the friendship in the database then an error will be returned
 */
func (conn *Connection) AddFriend(accID int, friendID int) error {
	//Check if the friendship already exists in the database
	flag, err := conn.checkFriendshipExists(accID, friendID)
	if err != nil {
		//An error occured during the checkFriendshipExists function
		return err
	}
	//If the flag is true then the friendship already exists in the database
	if flag {
		return errors.New("friendship already exists")
	}
	//The friendship does not exist so add it in the database
	//Prepare the statement for inserting the friendship in the database
	stmtInsert, err := conn.db.Prepare("INSERT INTO Friendships (AccountID, FriendID) VALUEs (?, ?)")
	//Check if an error occured during the statement preparation
	if err != nil {
		conn.logger.Error("Error occured during insert friendship statement preparation", err.Error())
		return err
	}
	defer stmtInsert.Close()
	//Execute the insert statement
	res, err := stmtInsert.Exec(accID, friendID)
	//Check if an error occured during the execution of the insert statement execution
	if err != nil {
		conn.logger.Error("Error occured during insert friendship statment execution", err.Error())
		return err
	}
	//Check that only one row was inserted in the database (if there are more than 1 rows then something bad happened)
	rowsAffected, err := res.RowsAffected()
	//Check if an error occured during the execution of RowsAffected function
	if err != nil {
		conn.logger.Error("Error occured during execution of RowsAffected function", err.Error())
		return err
	}
	//Check if the rows affected value is 1
	if rowsAffected != 1 {
		conn.logger.Error("Friendship insert, more than 1 row has been affected")
		return errors.New("friendship insert, more than 1 row affected")
	}
	//The friendship has been added succesfully
	return nil
}

/*
 * This function will get all the friendships that an account has
 * If there are no friendships for the account then an empty array will be returned
 */
func (conn *Connection) ViewFriendships(accID int) (data.Friends, error) {
	//Prepare the select statement for the friendships
	stmtSelect, err := conn.db.Prepare("SELECT AccountID, FriendID, BefriendDate FROM Friendships WHERE AccountID = ? OR FriendID = ?")
	//Check if an error occured during select statmenet preparation
	if err != nil {
		conn.logger.Error("Friendships select statment prepare", err.Error())
		return nil, err
	}
	defer stmtSelect.Close()
	//Execute the select statment
	rows, err := stmtSelect.Query(accID, accID)
	//Check if an error occured during the select statement
	if err != nil {
		conn.logger.Error("Friendships select statment execute", err.Error())
		return nil, err
	}
	//Extract the data from the rows
	friendRequests := make(data.Friends, 0)
	for rows.Next() {
		friendRequest := new(data.Friend)
		err = rows.Scan(&friendRequest.AccountID, &friendRequest.FriendID, &friendRequest.BefriendDate)
		if err != nil {
			conn.logger.Error("Friendships select statment scan", err.Error())
			return nil, err
		}
		friendRequests = append(friendRequests, *friendRequest)
	}

	return friendRequests, nil
}
