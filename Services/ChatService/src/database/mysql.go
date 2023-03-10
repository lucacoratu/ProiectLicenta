package database

import (
	"database/sql"
	"errors"
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

/*
 * This function will create a new room and return the id of the room that it inserted
 * An error will also be returned if the room could not be created
 */
func (conn *MysqlConnection) CreatePrivateRoom() (int64, error) {
	//Prepare the statement that will be executed when creating a new room
	res, err := conn.db.Exec("INSERT INTO rooms (isGroup) VALUES (false)")
	if err != nil {
		conn.logger.Info("Error occured while creating the room", err.Error())
		return -1, err
	}
	//Return the id of the room that was created
	return res.LastInsertId()
}

/*
 * This function will insert a userId into a room. It will return an error if the insert statement fails
 */
func (conn *MysqlConnection) InsertUserIntoRoom(userId int64, roomId int64) error {
	//TO DO ... check if the room exists
	res, err := conn.db.Exec("INSERT INTO user_room (UserId, RoomID) VALUES (?, ?)", userId, roomId)
	//Check if an error occured during the execution of the insert statement
	if err != nil {
		//An error occured
		conn.logger.Error("Error occured while inserting user into room", err.Error())
		return err
	}
	//Get the number of rows affected by the insert statement
	numberRows, err := res.RowsAffected()
	//Check if an error occured during the fetch of the number of affected rows
	if err != nil {
		//An error occured
		conn.logger.Error("Error occured while fetching the number of affected rows during insert of user into room", err.Error())
		return err
	}
	//Check if only 1 row was affected by the insert
	if numberRows != 1 {
		conn.logger.Error("Zero or more than 1 row was affected by the userid insert into romm")
		return errors.New("more than one (or none) row was affected, insert userid into room")
	}
	//Everything went ok
	return nil
}

/*
 * This function will get all the private rooms the user is in from the database
 * In order to get the rooms where the user is registered in from the mysql database you have to
 * extract the data from (users_rooms table which will have (user_id, room_id) rows and the rooms table which will have room id and details)
 */
func GetUserPrivateRooms(userId int64) (data.Rooms, error) {
	//Prepare the select statement which will get all the rooms the user with userID is into
	return nil, nil
}

/*
 * This function will get all the roomIDs for the private rooms of an account Id specified
 */
func (conn *MysqlConnection) GetUserPrivateRoomsIds(accountID int64) ([]int64, error) {
	//Prepare the select statement to get all the private rooms of the account
	stmtSelect, err := conn.db.Prepare("SELECT user_room.RoomID FROM user_room INNER JOIN rooms ON rooms.ID = user_room.RoomID WHERE isGroup = 0 AND user_room.UserID = ?")
	//Check if an error occured when preparing the select statement
	if err != nil {
		//An error occured
		conn.logger.Error("Error occured when preparing the select statement for getting account private rooms", err.Error())
		return make([]int64, 0), nil
	}
	//Execute the select statement
	rows, err := stmtSelect.Query(accountID)
	//Check if an error occured during the execution of the select statement
	if err != nil {
		//An error occured
		conn.logger.Error("Error occured during the execution of the select statement", err.Error())
		return make([]int64, 0), nil
	}

	//Get all the private rooms ids
	ids := make([]int64, 0)
	for rows.Next() {
		//Scan the id in the row
		var id int64 = 0
		err = rows.Scan(&id)
		if err != nil {
			conn.logger.Error("Error occured during the row fetch", err.Error())
			break
		}
		ids = append(ids, id)
	}

	//Return the ids found
	return ids, nil
}

/*
 * This function will get the roomId of a private conversation between 2 accounts
 */
func (conn *MysqlConnection) GetRoomId(accountId int64, friendId int64) (int64, error) {
	//Get the account private room ids
	ids, err := conn.GetUserPrivateRoomsIds(accountId)
	if err != nil {
		return 0, err
	}

	//Get the other user that is in the private conversation, if the other userid is the one we are looking for then return the room id
	for _, roomId := range ids {
		id, err := conn.GetPrivateRoomUser(accountId, roomId)
		if err != nil {
			return 0, err
		}
		if id == friendId {
			return roomId, nil
		}
	}

	//The private room was not found
	return 0, errors.New("room not found")
}

/*
 * This function will insert a message into the database. This function will need the room id, typeName, senderId and the data of the message
 * This function will return nil if everything was ok or an error != nil if something happened during the insertion of the message
 */
func (conn *MysqlConnection) InsertMessageIntoRoom(roomId int64, typeName string, senderID int64, data string) error {
	//Prepare the insert message statement
	stmtInsert, err := conn.db.Prepare("INSERT INTO messages (RoomID, TypeID, Data, UserID) VALUES (?, (SELECT ID FROM messagetypes WHERE TypeName = ?), ?, ?)")
	//Check if an error occured when preparing the insert statement of the message into the database
	if err != nil {
		//An error occured
		conn.logger.Error("Error occured when preparing the insert message statement", err.Error())
		return err
	}
	//Execute the insert statement with the data received from the client
	_, err = stmtInsert.Exec(roomId, typeName, data, senderID)
	//Check if an error occured when inserting the message into the database
	if err != nil {
		//An error occured
		conn.logger.Error("Error occured when executing the insert message statement", err.Error())
		return err
	}
	//Check if there was only 1 row affected (TO DO...)
	return nil
}

/*
 * This function will get the second userId from a private conversation. It needs the sender user's id and the room id
 * It will return nil for error if the select statement was succesful, else it will return a non nil error
 */
func (conn *MysqlConnection) GetPrivateRoomUser(senderID int64, roomId int64) (int64, error) {
	//Prepare the select statement to extract the second account id from the private room
	stmtSelect, err := conn.db.Prepare("SELECT user_room.UserID from user_room INNER JOIN rooms ON rooms.ID = user_room.RoomID WHERE user_room.UserID != ? AND rooms.ID = ? AND rooms.IsGroup = 0")
	//Check if an error occured when preparing the select statement
	if err != nil {
		//An error occured when preparing the select statement
		conn.logger.Error("Error occured when preparing the select statement for other person id (private room)", err.Error())
		return 0, nil
	}
	//Execute the select statement
	res := stmtSelect.QueryRow(senderID, roomId)
	//Check if an error occured during the execution of the select statement
	if res.Err() != nil {
		//An error occured when executing the select statement
		conn.logger.Error("Error occured when executing the select statement for the other person id (private room)", err.Error())
		return 0, nil
	}

	//Extract the second account id
	var accountID int64
	err = res.Scan(&accountID)
	if err != nil && err == sql.ErrNoRows {
		//The account id could not be extracted
		return 0, errors.New("not found")
	}
	if err != nil {
		//Another error occured
		conn.logger.Error("Error occured when executing the select statement for the other person id (private room)", res.Err().Error())
		return 0, err
	}

	//The other account id has been found so return it
	return accountID, nil
}

/*
 * This function will get all the participants in a room, if an error occurs then an error will be returned
 */
func (conn *MysqlConnection) GetRoomParticipants(senderId int64, roomId int64) ([]int64, error) {
	//Prepare the select statement to extract the second account id from the private room
	conn.logger.Debug("GetRoomParticipants SenderId", senderId, "RoomID", roomId)
	stmtSelect, err := conn.db.Prepare("SELECT user_room.UserID from user_room INNER JOIN rooms ON rooms.ID = user_room.RoomID WHERE user_room.UserID != ? AND rooms.ID = ?")
	//Check if an error occured when preparing the select statement
	if err != nil {
		//An error occured when preparing the select statement
		conn.logger.Error("Error occured when preparing the select statement for other person id (private room)", err.Error())
		return make([]int64, 0), nil
	}
	//Execute the select statement
	rows, err := stmtSelect.Query(senderId, roomId)
	//Check if an error occured during the execution of the select statement
	if err != nil {
		//An error occured when executing the select statement
		conn.logger.Error("Error occured when executing the select statement for the other person id (private room)", err.Error())
		return make([]int64, 0), nil
	}

	//Extract the participants ids
	accIds := make([]int64, 0)
	for rows.Next() {
		var accountID int64
		err = rows.Scan(&accountID)
		if err != nil {
			//Another error occured
			conn.logger.Error("Error occured when executing the select statement for the other person id (private room)", err.Error())
			return make([]int64, 0), err
		}
		accIds = append(accIds, accountID)
	}

	//The other account id has been found so return it
	return accIds, nil
}

/*
 * This function will get all the reactions of a message in a certain room
 * It will return a list of reactions if no error occured and nil for error
 * It will return an empty list and the error if something fails
 */
func (conn *MysqlConnection) GetMessageReactions(messageId int64, roomId int64) ([]data.Reaction, error) {
	//Prepare the statement for getting all the reactions of a message
	stmtReactions, err := conn.db.Prepare("SELECT ID,Emoji,UserId,ReactionDate FROM reactions INNER JOIN message_reaction ON message_reaction.ReactionId = reactions.ID WHERE message_reaction.MessageId = ?")
	//Check if an error occured during the statement preparation
	if err != nil {
		conn.logger.Error("Error occured when preparing the select for reactions", err.Error())
		return make([]data.Reaction, 0), err
	}
	//Execute the statement
	rows, err := stmtReactions.Query(messageId)
	//Check if an error occured
	if err != nil {
		conn.logger.Error("Error occured when executing the select statement for reactions", err.Error())
		return make([]data.Reaction, 0), err
	}

	//Get all the reactions of the
	reactions := make([]data.Reaction, 0)
	for rows.Next() {
		reaction := data.Reaction{}
		rows.Scan(&reaction.Id, &reaction.Emoji, &reaction.SenderId, &reaction.ReactionDate)
		reactions = append(reactions, reaction)
	}

	return reactions, nil
}

/*
 * This function will extract all the messages from the database of a certain room that is received as a parameter
 * If an error occurs then an error != nil will be returned, else nil will be returned
 */
func (conn *MysqlConnection) GetHistory(roomId int64) (data.Messages, error) {
	//Prepare the select statement which will get all the messages
	stmtSelect, err := conn.db.Prepare("SELECT ID,TypeID,Data,SendDate,UserID FROM messages WHERE RoomID = ?")
	//Check if an error occured while getting the chat history
	if err != nil {
		//An error occured during the select statement
		conn.logger.Error("Error occured during history select preparation", err.Error())
		return make(data.Messages, 0), nil
	}
	//Execute the select statement
	rows, err := stmtSelect.Query(roomId)
	//Check if an error occured when executing the select statement
	if err != nil {
		//An error occured during the execution of the select statement
		conn.logger.Error("Error occured during the history select execution", err.Error())
		return make(data.Messages, 0), err
	}

	//Get all the data from the rows
	messages := make(data.Messages, 0)
	for rows.Next() {
		message := data.Message{}
		err := rows.Scan(&message.Id, &message.TypeID, &message.Data, &message.SendDate, &message.UserId)
		if err != nil {
			conn.logger.Error("Error occured during history data fetch", err.Error())
			return make(data.Messages, 0), err
		}
		//For every message get the reactions
		//Get the reactions for the message
		reactions, err := conn.GetMessageReactions(message.Id, 0)
		//Check if an error occured
		if err != nil {
			conn.logger.Error("Error occcured when getting the message reactions, message id = ", message.Id, err.Error())
			return make(data.Messages, 0), err
		}
		message.Reactions = append(message.Reactions, reactions...)
		messages = append(messages, message)
	}

	//Return the data
	return messages, nil
}

/*
 * This function will create a new room and return the id of the room that it inserted
 * An error will also be returned if the room could not be created
 */
func (conn *MysqlConnection) CreateGroup(groupName string, creatorID int64) (int64, error) {
	//Prepare the statement that will be executed when creating a new room
	res, err := conn.db.Exec("INSERT INTO rooms (isGroup, groupName, CreatorId) VALUES (true, ?, ?)", groupName, creatorID)
	if err != nil {
		conn.logger.Info("Error occured while creating the room", err.Error())
		return -1, err
	}
	//Return the id of the room that was created
	return res.LastInsertId()
}

/*
 * This function will return all the groups that an account has. If an error occurs then an error will be returned
 */
func (conn *MysqlConnection) GetUserGroups(accountID int64) (data.GetGroups, error) {
	//Prepare the statement that will get all the groups of an account
	stmtSelect, err := conn.db.Prepare("SELECT rooms.ID, rooms.groupName, rooms.CreatorId, rooms.creationDate, rooms.groupPicture FROM rooms INNER JOIN user_room ON user_room.RoomID = rooms.ID WHERE user_room.UserID = ? AND rooms.isGroup = true")
	//Check if an error occured while inserting into the database
	if err != nil {
		conn.logger.Error("Error occured while preparing the get groups select statment", err.Error())
		return make(data.GetGroups, 0), err
	}
	rows, err := stmtSelect.Query(accountID)
	if err != nil {
		conn.logger.Error("Error occured while executing the get groups select statement", err.Error())
		return make(data.GetGroups, 0), err
	}

	returnData := make(data.GetGroups, 0)
	for rows.Next() {
		aux := data.GetGroup{}
		err := rows.Scan(&aux.RoomId, &aux.GroupName, &aux.CreatorId, &aux.CreationDate, &aux.GroupPictureUrl)
		if err != nil {
			conn.logger.Error("Error occured while fetching get groups data", err.Error())
			break
		}
		//Add the data into the return array
		returnData = append(returnData, aux)
	}

	//Get all the participants in the room
	for index, retData := range returnData {
		participants, err := conn.GetRoomParticipants(accountID, retData.RoomId)
		conn.logger.Debug("Participants", participants)
		if err != nil {
			conn.logger.Error("Error occured when getting the room participants")
			break
		}
		retData.Participants = make([]int64, 0)
		retData.Participants = append(retData.Participants, participants...)
		conn.logger.Debug(retData.Participants)
		returnData[index] = retData
		conn.logger.Debug(returnData[index])
	}

	return returnData, nil
}

/*
 * This function will get the last message from a room
 */
func (conn *MysqlConnection) GetLastMessageFromRoom(roomID int64) (string, string, int, error) {
	//Prepare the select statment which will extract the last message in the room
	stmtSelect, err := conn.db.Prepare("SELECT messages.Data, messages.SendDate, messages.UserID FROM rooms INNER JOIN messages ON messages.RoomID = rooms.ID WHERE rooms.ID = ? AND messages.SendDate = (SELECT MAX(messages.SendDate) FROM messages WHERE messages.RoomID = ?);")
	//Check if an error occured when preparing the select statement
	if err != nil {
		conn.logger.Error("Error occured while preparing the select statment for last message", err.Error())
		return "", "", 0, err
	}
	row := stmtSelect.QueryRow(roomID, roomID)
	//Check if an error occured during the execution of the select statement
	if row.Err() != nil {
		//An error occured when executing the select statement
		conn.logger.Error("Error occured when executing the select statement for the other person id (private room)", row.Err().Error())
		return "", "", 0, row.Err()
	}

	//Extract the second account id
	var lastMessage string
	var lastMessageTimestamp string
	var userId int
	err = row.Scan(&lastMessage, &lastMessageTimestamp, &userId)
	if err != nil && err == sql.ErrNoRows {
		//The account id could not be extracted
		return "", "", 0, errors.New("not found")
	}
	if err != nil {
		//Another error occured
		conn.logger.Error("Error occured when executing the select statement for the other person id (private room)", row.Err().Error())
		return "", "", 0, err
	}

	//The other account id has been found so return it
	return lastMessage, lastMessageTimestamp, userId, nil
}

/*
 * This function will get all the groups that contain 2 users (common groups of the 2 accounts)
 */
func (conn *MysqlConnection) GetCommonGroups(idFirst int64, idSecond int64) (data.CommonGroups, error) {
	//Get the groups of the first user
	getGroupsFirst, err := conn.GetUserGroups(idFirst)
	//Check if an error occured
	if err != nil {
		return nil, err
	}
	//Get the groups of the second user
	getGroupsSecond, err := conn.GetUserGroups(idSecond)
	if err != nil {
		return nil, err
	}

	//Find all the common groups of the 2 users
	commonGroups := make(data.CommonGroups, 0)
	for _, groupFirst := range getGroupsFirst {
		for _, groupSecond := range getGroupsSecond {
			if groupFirst.RoomId == groupSecond.RoomId {
				commonGroups = append(commonGroups, data.CommonGroup{GroupName: groupFirst.GroupName, RoomId: groupFirst.RoomId, CreatorId: groupFirst.CreatorId, CreationDate: groupFirst.CreationDate, Participants: groupFirst.Participants, ParticipantNames: groupFirst.ParticipantNames, GroupPictureUrl: groupFirst.GroupPictureUrl})
				break
			}
		}
	}

	return commonGroups, nil
}

/*
 * This function will update the group image
 */
func (conn *MysqlConnection) UpdateGroupPicture(roomId int64, newPicture string) (bool, error) {
	//Prepare the update statement
	stmtUpdate, err := conn.db.Prepare("UPDATE rooms SET groupPicture = ? WHERE ID = ?")
	//Check if an error occured
	if err != nil {
		conn.logger.Error("Error occured when preparing the update statement for group picture", err.Error())
		return false, err
	}

	_, err = stmtUpdate.Exec(newPicture, roomId)
	//Check if an error occured
	if err != nil {
		conn.logger.Error("Error occured when executing the update statement for group picture", err.Error())
		return false, err
	}

	return true, nil
}

/*
 * This function will check if the user already reacted to the message
 * It will return false if the user did not react to the message
 * Else it will return true
 * If an error occurs during the statements inside the function then the error return parameter will not be nil
 */
func (conn *MysqlConnection) CheckUserReactedToMessage(messageId int64, userId int64) (bool, error) {
	//Prepare the statement which will get the number of user reactions to a specific message
	stmtNumberReactions, err := conn.db.Prepare("SELECT COUNT(message_reaction.ReactionId) FROM reactions INNER JOIN message_reaction ON message_reaction.ReactionId = reactions.ID WHERE reactions.UserID = ? AND message_reaction.MessageId = ?")
	//Check if an error occured during the preparation of the sql statement
	if err != nil {
		conn.logger.Error("Error occured during the preparation of sql statment for checking if user reacted to message", err.Error())
		return false, err
	}
	//Execute the statement
	row := stmtNumberReactions.QueryRow(userId, messageId)
	//Check if an error occured when executing the sql statement
	if row.Err() != nil {
		conn.logger.Error("Error occured during the execution of sql statement for checking if user reacted to message", row.Err().Error())
		return false, row.Err()
	}

	//Get the number of reactions of a user to a message
	var numberReactions int64 = 0
	err = row.Scan(&numberReactions)
	//Check if an error occured during the extraction of data
	if err != nil {
		conn.logger.Error("Error ocured when scanning for the number of reactions to a message by a user", err.Error())
		return false, err
	}
	conn.logger.Debug("Number reactions", numberReactions)
	//Check if the number of reactions is 0
	if numberReactions != 0 {
		//The user already reacted to this message
		return true, nil
	}
	//The user did not react to the message
	return false, nil
}

/*
 * This function will insert a new reaction in the reactions table
 */
func (conn *MysqlConnection) InsertReaction(emojiReaction string, senderId int64) (int64, error) {
	//Prepare the insert statement
	stmtInsert, err := conn.db.Prepare("INSERT INTO reactions(Emoji, UserId) VALUES (?,?)")
	//Check if an error occured during the insert statement preparation
	if err != nil {
		conn.logger.Error("Error occured during the insert reaction preparation", err.Error())
		return 0, err
	}
	//Execute the sql statement
	res, err := stmtInsert.Exec(emojiReaction, senderId)
	//Check if an error occured
	if err != nil {
		conn.logger.Error("Error occured when executing the reaction insert statement", err.Error())
		return 0, err
	}
	//Return the values
	return res.LastInsertId()
}

/*
 * This function will create an entry in message_reaction table which will tie a reaction to a message
 * If the insert was successful then true will be returned and nil for error
 * Else an error will be returned
 */
func (conn *MysqlConnection) TieReactionToMessage(messageId int64, reactionId int64) (bool, error) {
	//Prepare the statement for inserting the entry
	stmtInsert, err := conn.db.Prepare("INSERT INTO message_reaction(MessageId,ReactionId) VALUES(?,?)")
	//Check if an error occured during statement preparation
	if err != nil {
		conn.logger.Error("Error occured when preparing the statement for message_reaction insertion", err.Error())
		return false, err
	}
	//Execute the statement with the values received as a parameter
	_, err = stmtInsert.Exec(messageId, reactionId)
	if err != nil {
		conn.logger.Error("Error occured when executing the insert statement in message_reaction", err.Error())
		return false, err
	}
	return true, nil
}

/*
 * This function will insert a new reaction to a specific message, but first it will check if the user already reacted to this message
 * If the user did not react to the message then the reaction will be inserted and associated with a specific message
 */
func (conn *MysqlConnection) AddMessageReaction(sendReact data.SendReact) (int64, error) {
	//Check if the user reacted to the message
	conn.logger.Debug("Add message reaction called")
	alreadyReacted, err := conn.CheckUserReactedToMessage(sendReact.MessageId, sendReact.SenderId)
	if err != nil {
		return 0, err
	}
	if alreadyReacted {
		return 0, errors.New("user already reacted to the message")
	}

	//Insert the reaction in the reactions table
	reactionId, err := conn.InsertReaction(sendReact.EmojiReaction, sendReact.SenderId)
	//Check if an error occured during the insert reaction function
	if err != nil {
		conn.logger.Error("Error occured when inserting the reaction", err.Error())
		return 0, errors.New("could not insert the reaction")
	}

	//Assign the reaction to the message
	_, err = conn.TieReactionToMessage(sendReact.MessageId, reactionId)
	if err != nil {
		return 0, err
	}

	return reactionId, nil
}
