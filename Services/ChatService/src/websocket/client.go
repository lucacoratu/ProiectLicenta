package websocket

import (
	"fmt"
	"log"

	"github.com/gorilla/websocket"
)

/*
 * This structure will define a client that connected to the chat service.
 * Each client will have a unique id, a websocket connection that will be used to send and receive messages
 * The client structure will also have a pointer to the pool structure which will be used for conccurency
 */
type Client struct {
	Id     int64
	Status string
	Conn   *websocket.Conn
	Pool   *Pool
}

/*
 * This structure will define a message that can be sent/received on the websocket
 * TO DO ... support multiple types of messages (group etc.)
 * The type variable will be used to determine if the websocket message is text or binary as it will have different values based on that
 * The body will be the payload that the other users it is delivered to should receive
 */
type Message struct {
	C    *Client
	Type int    `json:"type"`
	Body string `json:"body"`
}

type ResponseMessage struct {
	Type int    `json:"type"`
	Body string `json:"body"`
}

type SetAccountIDMessage struct {
	SetID int64 `json:"setAccountId"`
}

type UpdateProfilePicture struct {
	Id       int    `json:"id"`
	NewPhoto string `json:"newPhoto"`
}

type CallAccount struct {
	Caller int    `json:"caller"`
	Callee int    `json:"callee"`
	Option string `json:"option"`
	RoomId int    `json:"roomId"`
}

type CalledAccountNotification struct {
	Caller int `json:"caller"`
	Callee int `json:"callee"`
}

type ChangeStatusMessage struct {
	Text      string `json:"text"`
	AccountId int    `json:"accountId"`
	NewStatus string `json:"newStatus"`
}

type AcceptFriendRequestMessage struct {
	AccountId int `json:"accountId"`
	FriendId  int `json:"friendId"`
	RoomId    int `json:"roomid"`
}

type PrivateMessage struct {
	RoomID      int64  `json:"roomID"`
	Data        string `json:"data"`
	MessageType string `json:"messageType"`
	//Timestamp  int64  `json:"timestamp"`
}

type PrivateMessageResponse struct {
	MessageID   int64  `json:"id"`
	RoomID      int64  `json:"roomID"`
	Data        string `json:"data"`
	MessageType string `json:"messageType"`
	SenderID    int64  `json:"senderId"`
}

type GroupMessage struct {
	RoomID      int64  `json:"roomID"`
	Data        string `json:"data"`
	MessageType string `json:"messageType"`
	SenderID    int64  `json:"senderId"`
	IsGroup     bool   `json:"isGroup"`
}

type ChatMessage struct {
	RoomID      int64  `json:"roomID"`
	Data        string `json:"data"`
	MessageType string `json:"messageType"`
}

type CreateGroupMessage struct {
	CreatorId    int64   `json:"creatorID"`
	GroupName    string  `json:"groupName"`
	Participants []int64 `json:"participants"`
}

type CreateGroupResponse struct {
	RoomId       int64   `json:"roomId"`
	CreatorId    int64   `json:"creatorID"`
	GroupName    string  `json:"groupName"`
	Participants []int64 `json:"participants"`
}

type GetGroupsMessage struct {
}

/*
 * This function will wait for a message to be sent by the client and based on the message type different functions from the pool will be called
 */
func (c *Client) Read() {
	//Unregister a client when it disconnects from the server (this function will be called after the infinite loop)
	defer func() {
		c.Pool.Unregister <- c
		c.Conn.Close()
	}()

	//Check if a message is received from the client
	for {
		//read the message from the client (here is where the message type is specified)
		messageType, p, err := c.Conn.ReadMessage()
		if err != nil {
			log.Println(err)
			return
		}
		//Create the message structure based on the message received from the client
		message := Message{Type: messageType, Body: string(p), C: c}
		//Broadcast the message to all the other users (TO DO ... create private conversations and groups)
		c.Pool.Broadcast <- message
		//Log the message receiv from the client
		fmt.Printf("Message Received: %+v\n", message)
	}
}
