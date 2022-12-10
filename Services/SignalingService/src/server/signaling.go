package server

import (
	"encoding/json"
	"log"
	"net/http"
	"strconv"
	"text/template"

	"github.com/gorilla/websocket"
)

var AllRooms RoomMap

func CreateRoomRequestHandler(rw http.ResponseWriter, r *http.Request) {
	rw.Header().Set("Access-Control-Allow-Origin", "*")
	roomID := AllRooms.CreateRoom()

	type resp struct {
		RoomId string `json:"roomID"`
	}

	rw.WriteHeader(http.StatusOK)
	json.NewEncoder(rw).Encode(resp{RoomId: roomID})
}

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool {
		return true
	},
}

type broadcastMsg struct {
	Message map[string]interface{}
	RoomID  string
	Client  *websocket.Conn
}

var broadcast = make(chan broadcastMsg)

func broadcaster() {
	for {
		msg := <-broadcast
		for _, client := range AllRooms.Map[msg.RoomID] {
			if client.Conn != msg.Client {
				err := client.Conn.WriteJSON(msg.Message)

				if err != nil {
					log.Fatal(err.Error())
					client.Conn.Close()
				}
			}
		}
	}
}

func JoinRoomRequestHandler(rw http.ResponseWriter, r *http.Request) {
	rw.Header().Set("Access-Control-Allow-Origin", "*")
	roomID, ok := r.URL.Query()["roomID"]
	if !ok {
		log.Println("Room id missing in URL query")
		rw.WriteHeader(http.StatusBadRequest)
		rw.Write([]byte("RoomId needs to be specified in the GET parameters"))
		return
	}

	ws, err := upgrader.Upgrade(rw, r, nil)
	if err != nil {
		log.Println("Web socket upgrade error", err.Error())
		return
	}

	AllRooms.InsertIntoRoom(roomID[0], false, ws)

	go broadcaster()

	for {
		var msg broadcastMsg
		err := ws.ReadJSON(&msg.Message)
		if err != nil {
			log.Fatal("Read error", err.Error())
		}
		msg.Client = ws
		msg.RoomID = roomID[0]

		broadcast <- msg
	}
}

func RoomRequestHandler(rw http.ResponseWriter, r *http.Request) {
	log.Println("/room endpoint hit")
	rw.Header().Set("Access-Control-Allow-Origin", "*")
	roomID, ok := r.URL.Query()["roomID"]
	if !ok {
		log.Println("Room id missing in URL query")
		rw.WriteHeader(http.StatusBadRequest)
		rw.Write([]byte("RoomId needs to be specified in the GET parameters"))
		return
	}
	log.Println("Room id = ", roomID)

	//Get the audio specs
	audio, ok := r.URL.Query()["audio"]
	if !ok {
		log.Println("Audio missing in URL query")
		rw.WriteHeader(http.StatusBadRequest)
		rw.Write([]byte("audio needs to be specified in the GET parameters"))
		return
	}

	//Get the video specs
	video, ok := r.URL.Query()["video"]
	if !ok {
		log.Println("Video missing in URL query")
		rw.WriteHeader(http.StatusBadRequest)
		rw.Write([]byte("video needs to be specified in the GET parameters"))
		return
	}

	audioEnabled, err := strconv.ParseBool(audio[0])
	if err != nil {
		log.Println("Cannot parse audio value")
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Cannot parse audio value"))
		return
	}

	videoEnabled, err := strconv.ParseBool(video[0])
	if err != nil {
		log.Println("Cannot parse video value")
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Cannot parse video value"))
		return
	}

	type room struct {
		RoomID       string
		AudioEnabled bool
		VideoEnabled bool
	}

	t, err := template.ParseFiles("./templates/index.html")
	if err != nil {
		log.Println("Cannot parse template", err.Error())
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte(err.Error()))
		return
	}
	rw.WriteHeader(http.StatusOK)
	t.Execute(rw, room{RoomID: roomID[0], AudioEnabled: audioEnabled, VideoEnabled: videoEnabled})
}
