package websocket

import (
	"encoding/json"
	"fmt"
	"willow/signalingservice/logging"
)

/*
 * This structure will handle concurrent connections using channels
 * Each channel will have a particular functionality such as:
 * - The Register channel will handle a new connection to the chat service
 * - The Unregister channel will handle a client disconnecting from the chat service
 * - Clients is a map of client connections to the state of the connection (true for online)
 * - Broadcast channel will be used to broadcast the message to all the connected clients of the chat service
 */
type Pool struct {
	Register   chan *Client
	Unregister chan *Client
	Clients    map[*Client]bool
	Broadcast  chan Message
	logger     logging.ILogger
}

/*
 * This function will create a new pool that can then be used when starting the chat service
 */
func NewPool(l logging.ILogger) *Pool {
	return &Pool{
		Register:   make(chan *Client),
		Unregister: make(chan *Client),
		Clients:    make(map[*Client]bool),
		Broadcast:  make(chan Message),
		logger:     l,
	}
}

/*
 * This function will handle the register of a client (the client connected to the websocket)
 */
func (pool *Pool) ClientRegistered(c *Client) {
	pool.logger.Debug("Client connected to websocket")
}

func (pool *Pool) ClientUnregistered(c *Client) {
	pool.logger.Debug("Client disconnected from websocket")

}

/*
 * This function will handle when a message is recevied from a client
 */
func (pool *Pool) MessageReceived(message Message) {
	//Log that a message has been received on the websocket
	pool.logger.Info("Message received on the websocket")
	//Check what type of message it is
	//Check if it is a set AccountID message
	setIdData := &SetAccountIDMessage{}
	err := json.Unmarshal([]byte(message.Body), &setIdData)
	var isSetId bool = true
	if err != nil {
		pool.logger.Error(err.Error())
		isSetId = false
	}

	pool.logger.Info(setIdData)
	if isSetId && setIdData.SetID != 0 {
		//The message is a setID message so set the id of the connection to the id said in the message
		//Loop through all the connections and send the message
		pool.logger.Info("Message is SetAccountID Message, AccountID = ", setIdData.SetID)
		for client, _ := range pool.Clients {
			if client == message.C {
				//Set the client Id
				client.Id = setIdData.SetID
				//Send a message back which will confirm the id was set
				err = client.Conn.WriteJSON(ResponseMessage{Type: 1, Body: "Account ID has been set"})
				if err != nil {
					//Log the error message
					pool.logger.Error(err.Error())
				}
			}
		}

		//Exit the function as the message has been handled
		return
	}
}

/*
 * This function will start the pool which will handle client connections, client disconnections and broadcast messages
 */
func (pool *Pool) Start() {
	//Loop infinetly
	for {
		//Check what kind of event occured (connect, disconnect, broadcast message)
		select {
		case client := <-pool.Register:
			//A new client connected on the chat service websocket uri
			//Add the client connection to the pool of current connections
			pool.Clients[client] = true
			fmt.Println("Size of Connection Pool: ", len(pool.Clients))
			//Announce the user that it connected to the chat service
			for client, _ := range pool.Clients {
				fmt.Println(client)
				client.Conn.WriteJSON(Message{Type: 1, Body: "New User Joined..."})
			}
			pool.ClientRegistered(client)
		case client := <-pool.Unregister:
			//A client disconnected from the chat service
			//Delete the client connection from the current connections
			//Announce the client that is has disconnected from the chat service
			for client, _ := range pool.Clients {
				client.Conn.WriteJSON(Message{Type: 1, Body: "User Disconnected..."})
			}
			pool.ClientUnregistered(client)
			delete(pool.Clients, client)
			fmt.Println("Size of Connection Pool: ", len(pool.Clients))
		case message := <-pool.Broadcast:
			//Broadcast a message to all the current websocket connections
			pool.MessageReceived(message)
		}
	}
}
