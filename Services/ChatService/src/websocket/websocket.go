package websocket

import (
	"net/http"
	"github.com/gorilla/websocket"
	"fmt"
)

var upgrader = websocket.Upgrader{
	ReadBufferSize: 1024,
	WriteBufferSize: 1024,
	CheckOrigin: func(r *http.Request) bool { return true },
}

func Upgrade(rw http.ResponseWriter, r *http.Request) (*websocket.Conn, error){
	ws, err := upgrader.Upgrade(rw, r, nil)
	if err != nil{
		fmt.Println(err.Error())
		return ws, err
	}
	return ws, nil
}
