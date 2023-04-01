package database

import "willow/chatservice/data"

/*
 * This will be the interface that will define the functions that can be called to interact with the database
 * This is needed because during the testing of the application mysql will be used because the cassandra nodes are expensive
 * In the rest of the code this interface will be used so when the exchange to cassandra has to be made, only the initialization
 * should be modified
 */
type IConnection interface {
	InitializeConnection() error
	TestConnection() error
	CloseConnection() error

	//Chat functionality functions
	CreatePrivateRoom() (int64, error)
	InsertUserIntoRoom(userId int64, roomId int64) error
	InsertMessageIntoRoom(roomId int64, typeName string, senderID int64, data string, identityPublicKey string, ephemeralPublicKey string) (int64, error)
	GetPrivateRoomUser(senderID int64, roomId int64) (int64, error)
	GetRoomId(accountId int64, friendId int64) (int64, error)
	GetHistory(roomId int64) (data.Messages, error)
	GetHistoryWithId(roomId int64, messageId int64) (data.Messages, error)
	GetRoomParticipants(senderId int64, roomId int64) ([]int64, error)
	CreateGroup(groupName string, creatorID int64) (int64, error)
	GetUserGroups(accountID int64) (data.GetGroups, error)
	GetLastMessageFromRoom(roomID int64) (string, string, int, error)
	GetCommonGroups(idFirst int64, idSecond int64) (data.CommonGroups, error)
	UpdateGroupPicture(roomId int64, newPicture string) (bool, error)
	AddMessageReaction(sendReact data.SendReact) (int64, error)
}
