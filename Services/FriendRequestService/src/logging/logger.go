package logging

import "log"

/*
 * Interface that will define the required functions that the service
 * logger implementation should expose in order for the service to use
 * the logger properly
 */
type ILogger interface {
	Info(args ...any)
	Warning(args ...any)
	Error(args ...any)
	Debug(args ...any)
}

/*
 * This is the structure that will be used to create the custom logger
 * It has a log.Logger object and some prefixies for different error levels
 * that it will support
 */
type Logger struct {
	InternalLogger *log.Logger
	InfoPrefix     string
	WarningPrefix  string
	ErrorPrefix    string
	DebugEnabled   bool
	DebugPrefix    string
}

/*
 * This function will return an initialized object of the Logger struct
 */
func NewLogger(l *log.Logger, infoPrefix string, warningPrefix string, errorPrefix string) *Logger {
	return &Logger{InternalLogger: l, InfoPrefix: infoPrefix, WarningPrefix: warningPrefix, ErrorPrefix: errorPrefix, DebugEnabled: false, DebugPrefix: "[DEBUG]"}
}

/*
 * This function will return an initialized object of the Logger struct which will be able to print
 * debug messages, if the debug logging is enabled
 */
func NewDebugLogger(l *log.Logger, infoPrefix string, warningPrefix string, errorPrefix string) *Logger {
	return &Logger{InternalLogger: l, InfoPrefix: infoPrefix, WarningPrefix: warningPrefix, ErrorPrefix: errorPrefix, DebugEnabled: true, DebugPrefix: "[DEBUG]"}
}

/*
 * This function will print an information message
 * The parameters are variadic
 */
func (log *Logger) Info(args ...any) {
	log.InternalLogger.Println(log.InfoPrefix, args)
}

/*
 * This function will print a warning message
 * The parameters are variadic
 */
func (log *Logger) Warning(args ...any) {
	log.InternalLogger.Println(log.WarningPrefix, args)
}

/*
 * This function will print an error message
 * The parameters are variadic
 */
func (log *Logger) Error(args ...any) {
	log.InternalLogger.Println(log.ErrorPrefix, args)
}

/*
 * This function will print a debug message
 * The parameters are variadic
 */
func (log *Logger) Debug(args ...any) {
	if log.DebugEnabled {
		log.InternalLogger.Println(log.DebugPrefix, args)
	}
}
