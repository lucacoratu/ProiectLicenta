package database

import (
	"database/sql"
	"time"
	"errors"
	"willow/chatservice/data"
	"willow/chatservice/logging"

	_ "github.com/go-sql-driver/mysql"
)

/*
 * The Connection structure will hold the handle for the mysql database (this handle does not guarantee that the connection has been made)
 * The structure will also have a reference to the server logger so that messages can be logged if anything happens
 */
type MysqlConnection struct {
	logger logging.ILogger
	db     *sql.DB
	svc    *data.Configuration
}

/*
 * This function will create a new Connection structure that will be used throughout the server execution
 * It will not initialize the mysql database handle, for that the InitializeConnection function should be called
 * using the returned object
 */
func NewConnection(l logging.ILogger, conf *data.Configuration) *MysqlConnection {
	return &MysqlConnection{logger: l, db: nil, svc: conf}
}

/*
 * This function will initialize the connection handle to the database, it will also set max connection lifetime,
 * max idle connections and max open connections (the handle can have multiple connections at the same time)
 * If an error occurs then it will be returned from the function
 * Else the function will return nil
 */
func (conn *MysqlConnection) InitializeConnection() error {
	dbUser := "root"
	dbPassword := "" //Password not required
	dbHost := "localhost"
	dbName := "chatdb"
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
func (conn *MysqlConnection) TestConnection() error {
	return conn.db.Ping()
}

/*
 * This function will close the connection to the mysql server
 * If an error occurs during the closing process then it will be returned
 * Else nil will be returned
 */
func (conn *MysqlConnection) CloseConnection() error {
	conn.logger.Info("Closed the connection to the database")
	return conn.db.Close()
}

/*
 * This function will create a new room and return the id of the room that it inserted
 * An error will also be returned if the room could not be created
 */
func (conn *MysqlConnection) CreatePrivateRoom() (int64, error) {
	//Prepare the statement that will be executed when creating a new room
	res, err := conn.db.Exec("INSERT INTO rooms (isGroup) VALUES (false)")
	if err != nil{
		conn.logger.Info("Error occured while creating the room", err.Error())
		return -1, err
	}
	//Return the id of the room that was created
	return res.LastInsertId()
}

/*
 * This function will insert a userId into a room. It will return an error if the insert statement fails
 */
func (conn *MysqlConnection) InsertUserIntoRoom(userId int64, roomId int64) error{
	//TO DO ... check if the room exists
	res, err := conn.db.Exec("INSERT INTO user_room (UserId, RoomID) VALUES (?, ?)", userId, roomId)
	//Check if an error occured during the execution of the insert statement
	if err != nil{
		//An error occured
		conn.logger.Error("Error occured while inserting user into room", err.Error())
		return err
	}
	//Get the number of rows affected by the insert statement
	numberRows, err := res.RowsAffected()
	//Check if an error occured during the fetch of the number of affected rows
	if err != nil{
		//An error occured
		conn.logger.Error("Error occured while fetching the number of affected rows during insert of user into room", err.Error())
		return err
	}
	//Check if only 1 row was affected by the insert
	if numberRows != 1{
		conn.logger.Error("Zero or more than 1 row was affected by the userid insert into romm")
		return errors.New("more than one (or none) row was affected, insert userid into room")
	}
	//Everything went ok
	return nil
}

/*
 * This function will get all the rooms the user is in from the database
 * In order to get the rooms where the user is registered in from the mysql database you have to
 * extract the data from (users_rooms table which will have (user_id, room_id) rows and the rooms table which will have room id and details)  
 */