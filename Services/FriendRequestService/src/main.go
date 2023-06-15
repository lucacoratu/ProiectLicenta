package main

import "willow/friendrequestservice/server"

func main() {
	err := server.InitServer(":8083")
	if err != nil {
		panic(err.Error())
	}

	err = server.RunServer()
	if err != nil {
		panic(err.Error())
	}
}
