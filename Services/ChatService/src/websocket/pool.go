package websocket

import "fmt"

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
}

/*
 * This function will create a new pool that can then be used when starting the chat service
 */
func NewPool() *Pool {
    return &Pool{
        Register:   make(chan *Client),
        Unregister: make(chan *Client),
        Clients:    make(map[*Client]bool),
        Broadcast:  make(chan Message),
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
            break
        case client := <-pool.Unregister:
			//A client disconnected from the chat service
			//Delete the client connection from the current connections
            delete(pool.Clients, client)
            fmt.Println("Size of Connection Pool: ", len(pool.Clients))
			//Announce the client that is has disconnected from the chat service
            for client, _ := range pool.Clients {
                client.Conn.WriteJSON(Message{Type: 1, Body: "User Disconnected..."})
            }
            break
        case message := <-pool.Broadcast:
			//Broadcast a message to all the current websocket connections
            fmt.Println("Sending message to all clients in Pool")
			//Loop through all the connections and send the message
            for client, _ := range pool.Clients {
                if err := client.Conn.WriteJSON(message); err != nil {
                    fmt.Println(err)
                    return
                }
            }
        }
    }
}