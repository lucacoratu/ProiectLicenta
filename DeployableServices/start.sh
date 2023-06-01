#!/bin/bash

#Start the database first
echo "Starting database service"
sudo microk8s kubectl apply -f ./mysql/mysql.yaml

echo "Starting the account service"
sudo microk8s kubectl apply -f ./AccountService/accountservice.yaml

echo "Starting chat service"
sudo microk8s kubectl apply -f ./ChatService/chatservice.yaml

echo "Starting FriendRequestService"
sudo microk8s kubectl apply -f ./FriendRequestService/friendrequestservice.yaml

echo "Starting Friend service"
sudo microk8s kubectl apply -f ./FriendService/friendservice.yaml

echo "Starting gateway"
sudo microk8s kubectl apply -f ./Gateway/gateway.yaml

echo "Starting Signaling service"
sudo microk8s kubectl apply -f ./SignalingService/signalingservice.yaml
