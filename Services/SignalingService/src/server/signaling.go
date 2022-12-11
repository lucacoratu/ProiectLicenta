package server

import (
	"encoding/json"
	"log"
	"net/http"
	"strconv"
	"strings"
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
	ReadBufferSize:  4096,
	WriteBufferSize: 4096,
	CheckOrigin: func(r *http.Request) bool {
		return true
	},
}

type broadcastMsg struct {
	Message string
	RoomID  string
	Client  *websocket.Conn
}

var broadcast = make(chan broadcastMsg)

type response struct {
	Content string `json:"content"`
}

func RemoveIndex(s []Participant, index int) []Participant {
	return append(s[:index], s[index+1:]...)
}

func broadcaster() {
	for {
		msg := <-broadcast
		log.Println(msg.Message)
		if strings.Contains(msg.Message, "create or join") {
			//The user wanted to create the room
			//Check if the room is already created
			if _, ok := AllRooms.Map[msg.RoomID]; ok {
				//The key exists
				for _, client := range AllRooms.Map[msg.RoomID] {
					if client.Conn != msg.Client {
						resp := response{Content: "join"}
						err := client.Conn.WriteJSON(resp)

						if err != nil {
							log.Println(err.Error())
							client.Conn.Close()
						}
					}
				}
				//This is the client that joined the room
				log.Println("Here!")
				resp := response{Content: "joined"}
				AllRooms.InsertIntoRoom(msg.RoomID, false, msg.Client)
				msg.Client.WriteJSON(resp)
			} else {
				//The key doesn't exists
				//Create the key
				resp := response{Content: "created"}
				AllRooms.InsertIntoRoom(msg.RoomID, false, msg.Client)
				msg.Client.WriteJSON(resp)
			}

			continue
		}

		//If the message contains bye remove the connection from the room
		if strings.Contains(msg.Message, "bye") {
			i := -1
			for index, client := range AllRooms.Map[msg.RoomID] {
				if client.Conn == msg.Client {
					i = index
					break
				}
			}
			//Remove i client
			RemoveIndex(AllRooms.Map[msg.RoomID], i)
			log.Println(AllRooms.Map[msg.RoomID])
		}

		for _, client := range AllRooms.Map[msg.RoomID] {
			if client.Conn != msg.Client {
				err := client.Conn.WriteMessage(1, []byte(msg.Message))

				if err != nil {
					log.Println(err.Error())
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

	go broadcaster()

	for {
		var msg broadcastMsg
		msgType, data, err := ws.ReadMessage()
		log.Println(msgType)
		if msgType == -1 {
			//remove the connection from the list of participants
			i := -1
			for id, participants := range AllRooms.Map {
				for index, participant := range participants {
					if participant.Conn == ws {
						i = index
						break
					}
				}
				//Remove the participant at i
				AllRooms.Map[id] = RemoveIndex(participants, i)
				log.Println(AllRooms.Map[id])
				//Check if the room is empty
				if len(participants) == 0 {
					AllRooms.DeleteRoom(id)
				}
				if i != -1 {
					break
				}
			}
			return
		}
		if err != nil {
			log.Println("Read error", err.Error())
			return
		}
		msg.Message = string(data)
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

	//Get the platform
	platform, ok := r.URL.Query()["platform"]
	if !ok {
		log.Println("Platform missing in URL query")
		rw.WriteHeader(http.StatusBadRequest)
		rw.Write([]byte("platform needs to be specified in the GET parameters"))
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
		Platform     string
	}

	t, err := template.ParseFiles("./templates/index.html")
	if err != nil {
		log.Println("Cannot parse template", err.Error())
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte(err.Error()))
		return
	}
	rw.WriteHeader(http.StatusOK)
	t.Execute(rw, room{RoomID: roomID[0], AudioEnabled: audioEnabled, VideoEnabled: videoEnabled, Platform: platform[0]})
}
