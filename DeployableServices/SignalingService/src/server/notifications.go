package server

import (
	"net/http"
	"willow/signalingservice/logging"
	"willow/signalingservice/websocket"
)

type Notifications struct {
	logger logging.ILogger
}

func NewNotifications(l logging.ILogger) *Notifications {
	return &Notifications{logger: l}
}

func (not *Notifications) NotificationsHandler(pool *websocket.Pool, rw http.ResponseWriter, r *http.Request) {
	not.logger.Info("Endpoint /notifications hit")
	ws, err := websocket.Upgrade(rw, r)
	if err != nil {
		not.logger.Error(err.Error())
		return
	}

	client := &websocket.Client{
		Conn: ws,
		Pool: pool,
		Id:   0,
	}

	pool.Register <- client
	client.Read()
}
