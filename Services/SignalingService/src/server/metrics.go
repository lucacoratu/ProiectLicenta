package server

import (
	b64 "encoding/base64"
	"encoding/csv"
	"encoding/json"
	"fmt"
	"net/http"
	"os"
	"willow/signalingservice/data"
	"willow/signalingservice/database"
	"willow/signalingservice/logging"
)

type MetricsHandler struct {
	dbConn database.IConnection
	logger logging.ILogger
}

func NewMetricsHandler(dbConn database.IConnection, l logging.ILogger) *MetricsHandler {
	return &MetricsHandler{dbConn: dbConn, logger: l}
}

func (met *MetricsHandler) CollectInformation(rw http.ResponseWriter, r *http.Request) {
	if r.Method != "POST" {
		met.logger.Info("Endpoint /metrics/collect hit, wrong method", r.Method)
		http.Error(rw, "Invalid method", http.StatusBadRequest)
		return
	}

	met.logger.Info("Endpoint /metrics/collect hit (POST method)")

	profilingInformation := data.ProfilingInformation{}
	err := profilingInformation.FromJSON(r.Body)
	if err != nil {
		met.logger.Error("Metrics collect, cannot parse data from client", err.Error())
		http.Error(rw, "cannot parse data", http.StatusInternalServerError)
		return
	}
	//Filter the fps = 0
	for _, value := range profilingInformation.FrameRates {
		if value == 0 {
			met.logger.Info("Received metrics that have a 0 value", err.Error())
			http.Error(rw, "FPS cannot be 0", http.StatusBadRequest)
			return
		}
	}
	met.logger.Debug(profilingInformation)

	//Save the data in the csv
	f, err := os.OpenFile("measurements.csv", os.O_WRONLY|os.O_CREATE|os.O_APPEND, 0644)
	if err != nil {
		fmt.Println("Error: ", err)
		return
	}
	jsonStr, err := json.Marshal(profilingInformation)
	if err != nil {
		met.logger.Error("Cannot convert measurement to json", err.Error())
	}
	base64String := b64.StdEncoding.EncodeToString(jsonStr)
	record := make([]string, 0)
	record = append(record, base64String)
	w := csv.NewWriter(f)
	w.Write(record)
	w.Flush()
}
