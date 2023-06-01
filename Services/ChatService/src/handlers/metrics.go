package handlers

import (
	"net/http"
	"willow/chatservice/data"
	"willow/chatservice/database"
	"willow/chatservice/logging"
)

type MetricsHandler struct {
	dbConn database.IConnection
	logger logging.ILogger
}

func NewMetricsHandler(dbConn database.IConnection, l logging.ILogger) *MetricsHandler {
	return &MetricsHandler{dbConn: dbConn, logger: l}
}

func (met *MetricsHandler) CollectInformation(rw http.ResponseWriter, r *http.Request) {
	met.logger.Info("Endpoint /metrics/collect hit (POST method)")

	profilingInformation := data.ProfilingInformation{}
	err := profilingInformation.FromJSON(r.Body)
	if err != nil {
		met.logger.Error("Metrics collect, cannot parse data from client", err.Error())
		http.Error(rw, "cannot parse data", http.StatusInternalServerError)
		return
	}
	met.logger.Debug(profilingInformation)
}
