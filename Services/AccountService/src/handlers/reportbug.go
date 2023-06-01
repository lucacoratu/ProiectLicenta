package handlers

import (
	"net/http"
	"willow/accountservice/data"
	"willow/accountservice/database"
	"willow/accountservice/logging"
)

type ReportBug struct {
	logger logging.ILogger
	dbConn *database.Connection
}

/*
 * The NewRegister function will create a new object of the Register struct
 * Factory method for creating an object of Register easily
 */
func NewReportBug(l logging.ILogger, dbConn *database.Connection) *ReportBug {
	return &ReportBug{logger: l, dbConn: dbConn}
}

/*
 * This function will handle the POST request for inserting a new bug report in the database
 */
func (report *ReportBug) AddBugReport(rw http.ResponseWriter, r *http.Request) {
	//Parse the request body which will contain the category of the bug and a description inserted by the user
	report.logger.Info("/reportbug")
	addBug := &data.AddBugReport{}
	err := addBug.FromJSON(r.Body)
	if err != nil {
		report.logger.Error(err.Error())
		return
	}
	//Insert the bug report into the database
	err = report.dbConn.InsertBugReport(*addBug)
	if err != nil {
		report.logger.Error(err.Error())
		return
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write([]byte("Insert of report succeded"))
}

/*
 * This function will return all the available categories for the bug report
 */
func (report *ReportBug) GetBugReportCategories(rw http.ResponseWriter, r *http.Request) {
	//Log that the endpoint /reportcategories has been hit (GET method)
	report.logger.Info("Endpoint /reportcategories hit (GET method)")

	//Get all the categories from the database
	categories, err := report.dbConn.GetAllCategories()
	//Check if an error occured
	if err != nil {
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte(err.Error()))
		return
	}

	//Return the categories
	rw.WriteHeader(http.StatusOK)
	categories.ToJSON(rw)
}

/*
 * This function will handle the GET request which will send all the bugs to the client
 */
func (report *ReportBug) GetAllBugReports(rw http.ResponseWriter, r *http.Request) {
	report.logger.Info("Endpoint /bugreports hit (GET method)")

	//Get all the bug reports from the database
	bugReports, err := report.dbConn.GetAllBugReports()
	if err != nil {
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte(err.Error()))
		return
	}

	rw.WriteHeader(http.StatusOK)
	bugReports.ToJSON(rw)
}
