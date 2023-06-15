'use strict';

//Defining some global utility variables
var isChannelReady = false;
var isInitiator = false;
var isStarted = false;
var localStream;
var remoteStream = null;
var turnReady;

//Get the options of the call (video and audio enabled/disabled)
var audio = document.querySelector("#audioEnabled");
var video = document.querySelector("#videoEnabled");

//Get the roomID
var roomID = document.querySelector("#roomID");
roomID = roomID.innerHTML;

//Get the platform
var platform = document.querySelector("#platform");
platform = platform.innerHTML;

//Get the accountID
var currentAccountId = document.querySelector("#accountID");
currentAccountId = currentAccountId.innerHTML;

//This is the object that will be used to handle all the connections made in the group call
//It should contain the following elements
// The id of the account 
// The peer connection
// If the client in the current browser is the one who has to make the offers
// If the connection has started
// If the channel is ready for sending the offers
var participants = [];

//Initialize turn/stun server configurations here
var pcConfig = turnConfig;

//Util functions for creating and managing participants and peer connections
/*
 * This function will create a new peer connection to be used when a new user connects to the group room
 */
function CreatePeerConnection() {
    try {
        var pc = null;
        pc = new RTCPeerConnection(pcConfig);
        pc.onicecandidate = handleIceCandidate;
        pc.onaddstream = handleRemoteStreamAdded;
        pc.onremovestream = handleRemoteStreamRemoved;
        console.log('Created RTCPeerConnnection');
        return pc;
    } catch (e) {
        console.log('Failed to create PeerConnection, exception: ' + e.message);
        alert('Cannot create RTCPeerConnection object.');
        return null;
    }
}

/*
 * This function will create a participant and will append it to the list of current participants in the group
 * Parameters needed for this function are:
 * AccountId - integer - the id of the account that wants to join the conversation
 * isInitiator - bool - a boolean value signifying if the current participant should be the one to initialize connection
 * isStarted - bool - a boolean value representing the status of the connection for video and voice transfer
 * isChannelReady - bool - a boolean specifying if the offers can be transmited to the other client
 */
function CreateParticipant(AccountId, isInitiator, isStarted, isChannelReady) {
    //Create a new peer connection with all the necessary handlers for new remote stream, remove remote stream and ice candidate offer
    var pc = CreatePeerConnection();
    //Check if the peer connection object could be made (is not null)
    if(pc != null) {
        pc.onicecandidate = (event) => {
            if (event.candidate) {
                sendMessage({
                    type: 'candidate',
                    label: event.candidate.sdpMLineIndex,
                    id: event.candidate.sdpMid,
                    candidate: event.candidate.candidate
                }, AccountId);
            } else {
                console.log('End of candidates.');
            }
        }
        //Create a participant object and return it
        var participant = {"peerConnection": pc, "isInitiator": isInitiator, "isChannelReady": isChannelReady, "isStarted": isStarted, conversationStarted: false, "accountID": AccountId};
        return participant;
    }
    return null;
}

/*
 * This function will append a participant to the list of current participants of the group
 */
function AddParticipantToGroup(participant) {
    participants.push(participant);
}

/*
 * This function will search for a specific account id in all the participants of the group
 * If the participant can be found in the list of group members then the participant will be returned
 * Else null will be returned
 */
function SearchForParticipant(AccountID) {
    for(var i =0; i < participants.length; i++){
        if(participants[i]["accountID"] === AccountID)
            return participants[i];
    }
    return null;
}


//Create a pointer to remote stream object to determine the framerate
var remoteStreamCopy = null;
var currentFrameRate = 0;
var localStreamCopy = null;
var localCurrentFrameRate = 0;

//Declare an array for all the remote streams
var remoteStreams = [];
var isStartedList = [];
var isInitiatorList = [];
var isChannelReadyList = [];
var peerConnections = [];

//Update the frames per second on each remote stream
window.onload = () => {
    setInterval(() => {
        //For every remote stream in the remote streams array
        //get the fps-remotestreamid and change the inner html to be the number of frames in this second
        remoteStreams.forEach(remoteStream => {
            const fpsLabel = document.querySelector("#fps-" + remoteStream.id);
            currentFrameRate = remoteStream.getVideoTracks()[0].getSettings().frameRate;
            console.log("Remote current frame rate, id = ", remoteStream.id, " is ", currentFrameRate);
            if(currentFrameRate !== undefined)
                fpsLabel.innerHTML = currentFrameRate.toFixed(2);
        }); 
    }, 1000);
};

//Join the room
var ws = 0;
if(platform === "android")
    ws = new WebSocket("wss://192.168.137.1:8090/joingroup?roomID=" + roomID);
    //ws = new WebSocket("wss://10.0.2.2:8090/join?roomID=" + roomID);
else if(platform === "windows"){
    ws = new WebSocket("wss://localhost:8090/joingroup?roomID=" + roomID);
}

ws.onclose = function(event) {
    if(platform === "android")
        ws = new WebSocket("wss://192.168.137.1:8090/joingroup?roomID=" + roomID);
    else if(platform === "windows")
        ws = new WebSocket("wss://localhost:8090/joingroup?roomID=" + roomID);
}

ws.onopen = function(event) {
    let msg = {accountId: currentAccountId, content: "create or join"};
    console.log(JSON.stringify(msg));
    ws.send(JSON.stringify(msg));
}

function leaveCall() {
    hangup();
    //Clear the local stream
    const stream = localVideo.srcObject;
    const tracks = stream.getTracks();
  
    tracks.forEach((track) => {
      track.stop();
    });

    const remote_stream = localVideo.srcObject;
    const remote_tracks = remote_stream.getTracks();
  
    tracks.forEach((track) => {
      track.stop();
    });

    localVideo.srcObject = null;
    remoteVideo.srcObject = null;
}

//The message should be defined as following
// { 
//      type
//      content
// }

var muted = false;
var cameraEnabled = true;
//Function for handling muting and unmuting the microphone
function toggleMicrophone() {
    console.log("Toggle microphone called");
    var muted_button = document.querySelector("#microphone_button");
    if(muted == false) {
        muted = true;
        //Change the font icon of the microphone button
        muted_button.innerHTML = '<i class="fa fa-microphone-slash" style="font-size: 30px; color: whitesmoke;" ></i>'
        //Mute the microphone
        console.log(localStream.getAudioTracks());
        localStream.getAudioTracks()[0].enabled = false;
    } else { 
        muted = false;
        muted_button.innerHTML = '<i class="fa fa-microphone" style="font-size: 30px; color: whitesmoke;" ></i>'
        //Unmute the microphone
        localStream.getAudioTracks()[0].enabled = true;
        console.log(localStream.getAudioTracks());
    }
}

function toggleCamera() {
    var camera_button = document.querySelector("#camera_button");
    if(cameraEnabled == true) {
        cameraEnabled = false;
        camera_button.innerHTML = '<i class="fa fa-video-slash" style="font-size: 30px; color: whitesmoke;"></i>';
        localStream.getVideoTracks()[0].enabled = false;
        // localStream.getVideoTracks()[0].stop();
        // localStream.removeTrack(localStream.getVideoTracks()[0]);
        // console.log(localStream.getVideoTracks());
    } else {
        cameraEnabled = true;
        camera_button.innerHTML = '<i class="fa fa-video" style="font-size: 30px; color: whitesmoke;"></i>';
        localStream.getVideoTracks()[0].enabled = true;
        //Get the video stream from the camera
        // var streamConstraints = {
        //     audio: false,
        //     video: { facingMode: "user" }
        // };

        // navigator.mediaDevices.getUserMedia(streamConstraints)
        // .then((stream) => {
        //     localStream.addTrack(stream.getVideoTracks()[0]);
        // })
        // .catch((error) => {
        //     console.log("error: ", error);
        // })

    }
}


ws.onmessage = function(event) {
    let message = JSON.parse(event.data)
    //console.log('Client received message:', message);
    //The client is the first one to join the room
    if(message.content === 'created') {
        //He will initialize the next calls
        isInitiator = true;
        return;
    }

    //A new client joined the room
    if(message.content === 'join') {
        //The current client should connect to him
        //isChannelReady = true;
        //isInitiator = true;
        console.log("New user joined!");
        //Create a participant
        console.log(message);
        var participant = CreateParticipant(message.accountId, true, false, true);
        //Add the participant to group
        AddParticipantToGroup(participant)
        console.log(participants);
        return;
    }

    //The current client joined the room so he has to wait for other participants to send him the sdp offers
    if(message.content === 'joined') {
        isChannelReady = true;
        isInitiator = false;
        return;
    }

    //A client left the room
    if(message.content === 'bye') {
        console.log(message.content);
        if(isInitiator === true) {
            isStarted = false;
            isChannelReady = false;
        } else {
            isStarted = false;
            isChannelReady = false;
            isInitiator = true;
        }
        return;
    }

    //The client that connected succesfully got the local stream
    if (message.content === 'got user media' && currentAccountId != message.accountId) {
        //Try to connect to him
        var participant = SearchForParticipant(message.accountId);
        console.log(participant);
        if(participant != null) {
            if (participant["isStarted"] === false && typeof localStream !== 'undefined' && participant["isChannelReady"] === true) {
                var pc = participant['peerConnection'];
                pc.addStream(localStream);
                participant["isStarted"] = true;
                console.log('isInitiator', participant["isInitiator"]);
                if (participant["isInitiator"] === true) {
                    console.log('Sending offer to peer ', participant['accountID']);
                    pc.createOffer()
                    .then((sessionDescription) => {
                        pc.setLocalDescription(sessionDescription);
                        sendMessage(sessionDescription, participant['accountID']);
                    })
                    .catch((error) => {
                        console.log(error);
                    });
                }
            }
        }
        return;
    } else if (message.content.type === 'offer' && message.receiverId === currentAccountId) {
        //Search the list of participants for the one with accountID = the one specified
        var participant = SearchForParticipant(message.accountId);
        //If there is no participant with the searched accountId then create a new peer connection and add it to the participants list
        if(participant == null) {
            //Create the participant object
            var newParticipant = CreateParticipant(message.accountId, false, false, true);
            //If the participant has been created then set the localstream to the peer connection object if the localStream is not undefined
            if(newParticipant != null) {
                //Add the new participant into the group
                AddParticipantToGroup(newParticipant);
                console.log(newParticipant['isStarted']);
                if(newParticipant['isStarted'] == false && typeof localStream !== 'undefined' && newParticipant['isChannelReady'] == true) {
                    var pc = newParticipant['peerConnection'];
                    newParticipant['isStarted'] = true;
                    pc.addStream(localStream);
                    try{
                        //Set the description of the remote connection based on the offer from the other client
                        pc.setRemoteDescription(new RTCSessionDescription(message.content)).catch((exception) => {console.log(exception);});
                    } catch(exception) {
                        console.log(exception);
                        return;
                    }
                    //Create and send the answer to the other client
                    pc.createAnswer()
                    .then((sessionDescription) => {
                        console.log("Sending answer to peer ", newParticipant['accountID'], sessionDescription);
                        pc.setLocalDescription(sessionDescription);
                        sendMessage(sessionDescription, newParticipant['accountID']);
                    }, (error) => {
                        console.log(error);
                    });
                }

            }
        }
        return;
    } else if (message.content.type === 'answer' && message.receiverId === currentAccountId) {
        //Search for the participant that sent the answer
        console.log("Received answer: ", message);
        var participant = SearchForParticipant(message.accountId);
        if(participant != null && participant['conversationStarted'] === false) {
            try{
                var pc = participant['peerConnection'];
                pc.setRemoteDescription(new RTCSessionDescription(message.content)).catch((exception) => { console.log(exception);});
            } catch(exception) {
                console.log(exception);
            }
        }
        return;
    } else if (message.content.type === 'candidate' && message.receiverId === currentAccountId) {
        //Search for the participant that sent the ICE candidate
        var participant = SearchForParticipant(message.accountId);
        //console.log(participant['conversationStarted']);
        if(participant != null && participant['conversationStarted'] === false) {
            var pc = participant['peerConnection'];
            var candidate = new RTCIceCandidate({
                sdpMLineIndex: message.content.label,
                candidate: message.content.candidate
            });
            pc.addIceCandidate(candidate);
        }
        return;
    } else if (message === 'bye') {
        handleRemoteHangup();
        return;
    }
}

//Set the localStreamConstraints based on the type of call
var localStreamConstraints = {
    audio: false,
    video: { facingMode: "user" }
};

//Microphone should be captured
if(audio.innerHTML === "true") {
    localStreamConstraints.audio = true;
} else {
    localStreamConstraints.audio = false;
}

//Camera should be captured
if(video.innerHTML === "true") {
    localStreamConstraints.video = {facingMode: "user"};
} else {
    localStreamConstraints.video = false;
}

//Displaying Local Stream and Remote Stream on webpage
var localVideo = document.querySelector('#localVideo');
var remoteVideo = document.querySelector('#remoteVideo');
//console.log("Going to find Local media");
navigator.mediaDevices.getUserMedia(localStreamConstraints)
.then(gotStream)
.catch(function(e) {
  alert('getUserMedia() error: ' + e.name);
});

function sendMessage(message, receiverId) {
    //console.log('Client sending message: ', message);
    let msg = {accountId: currentAccountId, receiverId: receiverId, content: message }
    console.log(JSON.stringify(msg));
    ws.send(JSON.stringify(msg));
}


//Function to handle Ice candidates
function handleIceCandidate(event) {
    //console.log('icecandidate event: ', event);
    if (event.candidate) {
        sendMessage({
            type: 'candidate',
            label: event.candidate.sdpMLineIndex,
            id: event.candidate.sdpMid,
            candidate: event.candidate.candidate
        }, 0);
    } else {
        console.log('End of candidates.');
        //Search for the PeerConnection that is the target of this event
        participants[participants.length - 1]['conversationStarted'] = true;
    }
}

function handleCreateOfferError(event) {
    console.log('createOffer() error: ', event);
}

function doCall() {
    console.log('Sending offer to peer');
    var pc = participants[participants.length - 1]["peerConnection"];
    pc.createOffer(setLocalAndSendMessage, handleCreateOfferError);
}

function doAnswer() {
    console.log('Sending answer to peer.');
    pc.createAnswer().then(
        setLocalAndSendMessage,
        onCreateSessionDescriptionError
    );
}

function setLocalAndSendMessage(sessionDescription) {
    var pc = participants[participants.length - 1]["peerConnection"];
    pc.setLocalDescription(sessionDescription);
    console.log('setLocalAndSendMessage sending message', sessionDescription);
    sendMessage(sessionDescription);
}

function onCreateSessionDescriptionError(error) {
    console.log('Failed to create session description: ' + error.toString());
}


function handleRemoteStreamAdded(event) {
    console.log('Remote stream added.');
    console.log(event);
    remoteStream = event.stream;
    //Add the remote stream to the list of remote streams
    remoteStreams.push(remoteStream);
    console.log(remoteStreams);
    
    //Create a new video element and add it in the remoteVideosContainer
    const newVideo = document.createElement("video");
    newVideo.srcObject = remoteStream;
    newVideo.autoplay = true;
    newVideo.playsInline = true;
    newVideo.id = remoteStream.id;
    newVideo.className = "video";
    //Create a <b> where the frames of the video will be displayed
    const newFrameRate = document.createElement("b");
    newFrameRate.innerHTML = "FPS";
    newFrameRate.id = "fps-" + remoteStream.id;
    newFrameRate.className = "fps-label";

    const newStreamDiv = document.createElement("div");
    newStreamDiv.className = "video-container";
    newStreamDiv.appendChild(newFrameRate);
    newStreamDiv.appendChild(newVideo);

    const remoteStreamsContainer = document.querySelector("#remoteVideosContainer");
    remoteStreamsContainer.appendChild(newStreamDiv);

    //Copy of the remote stream
    //remoteStreamCopy = remoteStream;
    //remoteVideo.srcObject = remoteStream;
}

function handleRemoteStreamRemoved(event) {
    console.log('Remote stream removed. Event: ', event);
}

function hangup() {
    console.log('Hanging up.');
    stop();
    sendMessage('bye');
}

function handleRemoteHangup() {
    console.log('Session terminated.');
    stop();
    isInitiator = false;
}

function stop() {
    isStarted = false;
    pc.close();
    pc = null;
}

function maybeStart(){
    console.log('>>>>>>> maybeStart() ', isStarted, localStream, isChannelReady);
    if (!isStartedList[isStartedList.length - 1] && typeof localStream !== 'undefined' && isChannelReadyList[isChannelReadyList.length - 1]) {
      console.log('>>>>>> creating peer connection');
      createPeerConnection();
      peerConnections[peerConnections.length - 1].addStream(localStream);
      isStarted = true;
      console.log('isInitiator', isInitiator);
      if (isInitiator) {
        doCall();
      }
    }
}

//If found local stream
function gotStream(stream) {
    console.log('Adding local stream.');
    localStream = stream;
    localVideo.srcObject = stream;
    localStreamCopy = stream;

    sendMessage('got user media');
    //if () {
    //  maybeStart();
    //}
}

//Sending bye if user closes the window
window.onbeforeunload = function() {
    sendMessage('bye');
};
