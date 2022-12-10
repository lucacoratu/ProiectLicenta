'use strict';

//Defining some global utility variables
var isChannelReady = false;
var isInitiator = false;
var isStarted = false;
var localStream;
var pc;
var remoteStream;
var turnReady;

//Get the options of the call (video and audio enabled/disabled)
var audio = document.querySelector("#audioEnabled");
var video = document.querySelector("#videoEnabled");

//Get the roomID
var roomID = document.querySelector("#roomID");
roomID = roomID.innerHTML;

//Join the room
let ws = new WebSocket("ws://localhost:8090/join?roomID=" + roomID);

ws.onopen = function(event) {
    let msg = {content: "create or join"};
    console.log(JSON.stringify(msg));
    ws.send(JSON.stringify(msg));
}

//The message should be defined as following
// { 
//      type
//      content
// }

ws.onmessage = function(event) {
    let message = JSON.parse(event.data)
    console.log('Client received message:', message);
    if(message.content === 'created') {
        isInitiator = true;
    }
    if(message.content === 'join') {
        isChannelReady = true;
    }
    if(message.content === 'joined') {
        isChannelReady = true;
    }
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
    }
    if (message.content === 'got user media') {
        maybeStart();
    } else if (message.content.type === 'offer') {
        if (!isInitiator && !isStarted) {
          maybeStart();
        }
        pc.setRemoteDescription(new RTCSessionDescription(message.content));
        doAnswer();
    } else if (message.content.type === 'answer' && isStarted) {
        pc.setRemoteDescription(new RTCSessionDescription(message.content));
    } else if (message.content.type === 'candidate' && isStarted) {
        var candidate = new RTCIceCandidate({
          sdpMLineIndex: message.content.label,
          candidate: message.content.candidate
        });
        pc.addIceCandidate(candidate);
    } else if (message === 'bye' && isStarted) {
        handleRemoteHangup();
    }
}

//Initialize turn/stun server here
var pcConfig = turnConfig;

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

function sendMessage(message) {
    console.log('Client sending message: ', message);
    let msg = { content: message }
    console.log(JSON.stringify(msg));
    ws.send(JSON.stringify(msg));
}

//Creating peer connection
function createPeerConnection() {
    try {
        pc = new RTCPeerConnection(pcConfig);
        pc.onicecandidate = handleIceCandidate;
        pc.onaddstream = handleRemoteStreamAdded;
        pc.onremovestream = handleRemoteStreamRemoved;
        console.log('Created RTCPeerConnnection');
    } catch (e) {
        console.log('Failed to create PeerConnection, exception: ' + e.message);
        alert('Cannot create RTCPeerConnection object.');
        return;
    }
}

//Function to handle Ice candidates
function handleIceCandidate(event) {
    console.log('icecandidate event: ', event);
    if (event.candidate) {
        sendMessage({
        type: 'candidate',
        label: event.candidate.sdpMLineIndex,
        id: event.candidate.sdpMid,
        candidate: event.candidate.candidate
        });
    } else {
        console.log('End of candidates.');
    }
}

function handleCreateOfferError(event) {
    console.log('createOffer() error: ', event);
}

function doCall() {
console.log('Sending offer to peer');
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
    pc.setLocalDescription(sessionDescription);
    console.log('setLocalAndSendMessage sending message', sessionDescription);
    sendMessage(sessionDescription);
}

function onCreateSessionDescriptionError(error) {
    console.log('Failed to create session description: ' + error.toString());
}


function handleRemoteStreamAdded(event) {
    console.log('Remote stream added.');
    remoteStream = event.stream;
    remoteVideo.srcObject = remoteStream;
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
    if (!isStarted && typeof localStream !== 'undefined' && isChannelReady) {
      console.log('>>>>>> creating peer connection');
      createPeerConnection();
      pc.addStream(localStream);
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
    sendMessage('got user media');
    if (isInitiator) {
      maybeStart();
    }
}

//Sending bye if user closes the window
window.onbeforeunload = function() {
    sendMessage('bye');
};
