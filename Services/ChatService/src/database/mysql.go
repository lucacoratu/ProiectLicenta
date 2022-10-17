package database

import (
	"database/sql"
	"time"
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