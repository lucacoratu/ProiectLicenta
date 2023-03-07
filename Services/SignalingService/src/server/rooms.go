package server

import (
	"log"
	"sync"

	"github.com/gorilla/websocket"
)

type Participant struct {
	Host bool
	Conn *websocket.Conn
}

type RoomMap struct {
	Mutex sync.RWMutex
	Map   map[int64][]Participant
}

func (r *RoomMap) Init() {
	r.Map = make(map[int64][]Participant, 0)
}

func (r *RoomMap) Get(roomID int64) []Participant {
	r.Mutex.Lock()
	defer r.Mutex.Unlock()

	return r.Map[roomID]
}

// func (r *RoomMap) CreateRoom() string {
// 	r.Mutex.Lock()
// 	defer r.Mutex.Unlock()

// 	rand.Seed(time.Now().UnixNano())
// 	var letters = []rune("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ")

// 	b := make([]rune, 8)
// 	for i := range b {
// 		b[i] = letters[rand.Intn(len(letters))]
// 	}

// 	roomID := string(b)
// 	r.Map[roomID] = []Participant{}
// 	return roomID
// }

func (r *RoomMap) InsertIntoRoom(roomId int64, host bool, conn *websocket.Conn) {
	r.Mutex.Lock()
	defer r.Mutex.Unlock()

	p := Participant{host, conn}
	log.Println("Inserting into Room with roomID: ", roomId)
	r.Map[roomId] = append(r.Map[roomId], p)
	log.Println(r.Map[roomId])

}

func (r *RoomMap) DeleteRoom(roomID int64) {
	//r.Mutex.Lock()
	//defer r.Mutex.Unlock()
	log.Println("Delete room", roomID)
	delete(r.Map, roomID)
}
