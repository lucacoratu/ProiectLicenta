package handlers

import (
	"net/http"
	"os"
	"strconv"
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

/*
 * This function will collect metrics sent by the devices that have the application installed
 * The metrics of interest are number of miliseconds to encrypt the message, the message size (in bytes) and the device name
 */
func (met *MetricsHandler) CollectEncryptionMetrics(rw http.ResponseWriter, r *http.Request) {
	met.logger.Info("Endpoint /metrics/encryption hit (POST method)")
	encryptionProfiling := data.EncryptionProfiling{}
	err := encryptionProfiling.FromJSON(r.Body)
	if err != nil {
		met.logger.Error("Encryption metrics collect, cannot parse data from client", err.Error())
		http.Error(rw, "cannot parse data", http.StatusInternalServerError)
		return
	}
	met.logger.Debug(encryptionProfiling)

	encryptionFilename := "encryption_metrics.txt"
	//Save data into the encryption metrics file
	f, err := os.OpenFile(encryptionFilename, os.O_WRONLY|os.O_CREATE|os.O_APPEND, 0644)
	if err != nil {
		met.logger.Error("Error: ", err.Error())
		return
	}
	_, err = f.WriteString(encryptionProfiling.DeviceInfo + "\t" + strconv.FormatInt(encryptionProfiling.MessageSize, 10) + "\t" + strconv.FormatInt(encryptionProfiling.ElapsedMiliseconds, 10) + "\n")
	if err != nil {
		met.logger.Error("Could not save encryption profiling into file", err.Error())
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write([]byte("Metrics received"))
}

/*
 * This function will collect metrics for decryption of messages
 */
func (met *MetricsHandler) CollectDecryptionMetrics(rw http.ResponseWriter, r *http.Request) {
	met.logger.Info("Endpoint /metrics/decryption hit (POST method)")
	decryptionProfiling := data.EncryptionProfiling{}
	err := decryptionProfiling.FromJSON(r.Body)
	if err != nil {
		met.logger.Error("Decryption metrics collect, cannot parse data from client", err.Error())
		http.Error(rw, "cannot parse data", http.StatusInternalServerError)
		return
	}
	met.logger.Debug(decryptionProfiling)

	decryptionFilename := "./decryption_metrics.txt"
	//Save data into the decryption metrics file
	f, err := os.OpenFile(decryptionFilename, os.O_WRONLY|os.O_CREATE|os.O_APPEND, 0644)
	if err != nil {
		met.logger.Error("Error: ", err.Error())
		return
	}
	_, err = f.WriteString(decryptionProfiling.DeviceInfo + "\t" + strconv.FormatInt(decryptionProfiling.MessageSize, 10) + "\t" + strconv.FormatInt(decryptionProfiling.ElapsedMiliseconds, 10) + "\n")
	if err != nil {
		met.logger.Error("Could not save encryption profiling into file", err.Error())
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write([]byte("Metrics received"))
}
