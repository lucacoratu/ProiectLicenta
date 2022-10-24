package websocket

import (
	"net/http"
	"github.com/gorilla/websocket"
	"fmt"
)

/*
 * This variable will be an object of strcture Upgrader from the gorilla websocket package
 * In this structure we need to define the maximum size of the ReadBuffer and WriteBuffer
 * We also need to define the CheckOrigin function which will return true (this function is used to filter the origin of the connection)
 * By returning true it means that everybody can connect to the chat service
 */
var upgrader = websocket.Upgrader{
	ReadBufferSize: 4096,
	WriteBufferSize: 4096,
	CheckOrigin: func(r *http.Request) bool { return true },
}

/*
 * This function will upgrade a HTTP connection to a websocket connection that will be used to send and receive messages
 * TO DO ... better logging of the error messages
 */
func Upgrade(rw http.ResponseWriter, r *http.Request) (*websocket.Conn, error){
	ws, err := upgrader.Upgrade(rw, r, nil)
	if err != nil{
		fmt.Println(err.Error())
		return ws, err
	}
	return ws, nil
}
