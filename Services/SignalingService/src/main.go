package main

import (
	"log"
	"net/http"
	"willow/signalingservice/server"
)

func main() {
	server.AllRooms.Init()

	http.Handle("/", http.FileServer(http.Dir("./public")))
	http.Handle("/create", http.HandlerFunc(server.CreateRoomRequestHandler))
	http.Handle("/join", http.HandlerFunc(server.JoinRoomRequestHandler))
	http.Handle("/room", http.HandlerFunc(server.RoomRequestHandler))

	log.Println("Starting server on port 8090")
	err := http.ListenAndServeTLS(":8090", "server.crt", "server.key", nil)
	if err != nil {
		log.Fatal(err.Error())
	}
}
