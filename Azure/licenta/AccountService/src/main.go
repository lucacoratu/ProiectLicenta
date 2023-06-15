package main

import (
	"willow/accountservice/server"
)

func main() {
	err := server.InitServer(":8081")
	if err != nil {
		panic(err)
	}

	err = server.RunServer()
	if err != nil {
		panic(err)
	}
}
