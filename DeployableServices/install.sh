#!/bin/bash

#echo "=====INSTALLING DOCKER=====";
#sudo apt install docker.io;

#echo "=====INSTALLING microk8s=====";
#sudo snap install microk8s --classic --channel=1.27

#echo ""

#Build all the necessary docker images
echo "=====BUILDING IMAGES====="
echo "Building AccountService image"
cd ./AccountService/
sudo docker image rm -f accountservice
sudo docker image build -t accountservice .

echo "Building ChatService image"
cd ../ChatService/
sudo docker image rm -f chatservice
sudo docker image build -t chatservice .

echo "Building FriendRequestService image"
cd ../FriendRequestService
sudo docker image rm -f friendrequestservice
sudo docker image build -t friendrequestservice .

echo "Building FriendService image"
cd ../FriendService
sudo docker image rm -f friendservice
sudo docker image build -t friendservice .

echo "Building Gateway image"
cd ../Gateway
sudo docker image rm -f gateway
sudo docker image build -t gateway .

echo "Building SignalingService image"
cd ../SignalingService
sudo docker image rm -f signalingservice
sudo docker image build -t signalingservice .

echo "Building Database image"
cd ../mysql
sudo docker image rm -f willow_db
sudo docker image build -t willow_db .
echo "=====FINISHED BUILDING IMAGES====="

echo "=====UPLOADING IMAGES TO REGISTRY====="
echo "Uploading account service image"
sudo docker image tag accountservice localhost:32000/accountservice
sudo docker image push localhost:32000/accountservice

echo "Uploading chat service image"
sudo docker image tag chatservice localhost:32000/chatservice
sudo docker image push localhost:32000/chatservice

echo "Uploading friend request service image"
sudo docker image tag friendrequestservice localhost:32000/friendrequestservice
sudo docker image push localhost:32000/friendrequestservice

echo "Uploading friend service image"
sudo docker image tag friendservice localhost:32000/friendservice
sudo docker image push localhost:32000/friendservice

echo "Uploading gateway image"
sudo docker image tag gateway localhost:32000/gateway
sudo docker image push localhost:32000/gateway

echo "Uploading signaling service image"
sudo docker image tag signalingservice localhost:32000/signalingservice
sudo docker image push localhost:32000/signalingservice

echo "Uploading database image"
sudo docker image tag willow_db localhost:32000/willow_db
sudo docker image push localhost:32000/willow_db
echo "=====FINISHED UPLOADING IMAGES TO REGISTRY====="


echo "To start the application run start.sh"