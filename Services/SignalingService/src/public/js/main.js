'use strict';

//Defining some global utility variables
var isChannelReady = false;
var isInitiator = false;
var isStarted = false;
var localStream;
var pc;
var remoteStream = null;
var turnReady;

//Worker which will encrypt the data
const worker = new Worker('js/worker.js');

if ('MediaStreamTrackProcessor' in window && 'MediaStreamTrackGenerator' in window) {
    // Insertable streams for `MediaStreamTrack` is supported.
    console.log("Support insertable streams", true);
}

//Create the transformer for the video stream
const transformerSender = new TransformStream({
    async transform(encodedFrame, controller) {
        // Reconstruct the original frame.
        //console.log("Frame received for encryption");
        //console.log(encodedFrame);
        const newData = new ArrayBuffer(encodedFrame.allocationSize());
        encodedFrame.copyTo(newData);
        //console.log(newData);

        //const view = new DataView(encodedFrame.data);

        // const newData = new ArrayBuffer(encodedFrame.data.byteLength);
        const newView = new DataView(newData);

        //Xor all the bytes with 0x4b
        const xor_value = 0x4b;
        for (let i = 0; i < encodedFrame.allocationSize(); ++i)
             newView.setInt8(i, newView.getInt8(i) ^ xor_value);

        encodedFrame.data = newData;
        controller.enqueue(encodedFrame);
    }
});

const transformerReceiver = new TransformStream({
    async transform(encodedFrame, controller) {
        // Reconstruct the original frame.
        //console.log("Frame received for decryption");
        //console.log(encodedFrame);
        const newData = new ArrayBuffer(encodedFrame.allocationSize());
        encodedFrame.copyTo(newData);
        //console.log(newData);

        //const view = new DataView(encodedFrame.data);

        // const newData = new ArrayBuffer(encodedFrame.data.byteLength);
        const newView = new DataView(newData);

        //Xor all the bytes with 0x4b
        const xor_value = 0x4b;
        for (let i = 0; i < encodedFrame.allocationSize(); ++i)
            newView.setInt8(i, newView.getInt8(i) ^ xor_value);
        
        encodedFrame.data = newData;
        controller.enqueue(encodedFrame);
    }
})

//Get the options of the call (video and audio enabled/disabled)
var audio = document.querySelector("#audioEnabled");
var video = document.querySelector("#videoEnabled");

//Get the device info
var deviceInfo = document.querySelector("#deviceInfo");
deviceInfo = deviceInfo.innerHTML;

//Get the roomID
var roomID = document.querySelector("#roomID");
roomID = roomID.innerHTML;

var platform = document.querySelector("#platform");
platform = platform.innerHTML;

var metricsUrl = 0;
if(platform === "android") {
    metricsUrl = "http://192.168.137.1:8087/metrics/collect";
} else if(platform === "windows"){
    metricsUrl = "http://localhost:8087/metrics/collect";
}


//Create a pointer to remote stream object to determine the framerate
var remoteStreamCopy = null;
var currentFrameRate = 0;
var localStreamCopy = null;
var localCurrentFrameRate = 0;
var localLatency = 0;
var remoteLatency = 0;

window.onload = function() {
    console.log("On load called");
    //Define a function which will be called once every second to change the frame rate
    setInterval(function() {
        //Update the frame rate of the remote stream
        //console.log("Set interval");
        //console.log(remoteStreamCopy);
        if(remoteStreamCopy !== null) {
            //Get the remote framerate
            //console.log(remoteStreamCopy.getVideoTracks()[0].getSettings().frameRate);
            //console.log(remoteStreamCopy.getVideoTracks()[0].getSettings());
            currentFrameRate = remoteStreamCopy.getVideoTracks()[0].getSettings().frameRate;
            //remoteLatency = remoteStreamCopy.getAudioTracks()[0].getSettings().latency;
            
            console.log("Remote latency = ", remoteLatency * 1000, "ms");
            //console.log("Remote current frame rate = ", currentFrameRate);

            //Show the remote value of the framerate
            var frameRateLabel = document.querySelector("#labelFrameRate");
            frameRateLabel.innerHTML = currentFrameRate.toFixed(2);
        }

        console.log(remoteStream.getAudioTracks());
        
        if(localStreamCopy !== null) {
            //Get the local framerate
            localCurrentFrameRate = localStreamCopy.getVideoTracks()[0].getSettings().frameRate;
            localLatency = localStreamCopy.getAudioTracks()[0].getSettings().latency;

            //console.log("Local latency = ", localLatency * 1000, "ms");
            //console.log("Local current frame rate = ", localCurrentFrameRate);

            //Show the local value of the framerate
            var localFrameRateLabel = document.querySelector("#labelLocalFrameRate");
            localFrameRateLabel.innerHTML = localCurrentFrameRate.toFixed(2);
        }
    }, 1000);

    //Define a function which will be called every 10 seconds to send metrics to the server
    setInterval(function(){
        var xhttp = new XMLHttpRequest();
        console.log(metricsUrl);
        xhttp.open("POST", "/metrics/collect", true);
        xhttp.setRequestHeader("Content-Type", "application/json")
        var metricsData = {'deviceInfo': deviceInfo, "frameRates": [localCurrentFrameRate, currentFrameRate], 'numberParticipants': 2}; 
        console.log(JSON.stringify(metricsData));
        xhttp.onreadystatechange = function() {
            if (this.readyState == 4 && this.status == 200) {
               
            }
        };
        xhttp.send(JSON.stringify(metricsData));
    },10000);
}

//Join the room
var ws = 0;
if(platform === "android")
    ws = new WebSocket("wss://192.168.137.1:8090/join?roomID=" + roomID);
    //ws = new WebSocket("wss://10.0.2.2:8090/join?roomID=" + roomID);
else if(platform === "windows"){
    ws = new WebSocket("wss://localhost:8090/join?roomID=" + roomID);
}


ws.onopen = function(event) {
    let msg = {content: "create or join"};
    console.log(JSON.stringify(msg));
    ws.send(JSON.stringify(msg));
}

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


function PeerMuted() {
    console.log("The other user is muted!");
    //Show an icon on the remote video signifying the user is muted
    const muted_div = document.querySelector("#remote_muted");
    muted_div.style.display = "block";
}

function PeerUnmuted() {
    console.log("The other user unmuted");
    const muted_div = document.querySelector("#remote_muted");
    muted_div.style.display = "none";
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

ws.onmessage = function(event) {
    let message = JSON.parse(event.data)
    console.log('Client received message:', message);
    if(message.content === 'created') {
        isInitiator = true;
    }
    if(message.content === 'join') {
        isChannelReady = true;
        isInitiator = true;
    }
    if(message.content === 'joined') {
        isChannelReady = true;
        isInitiator = false;
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

    // //Decode the remote stream
    remoteStream = event.stream;
    
    // const videoTrack = remoteStream.getVideoTracks()[0];

    // const trackProcessor = new MediaStreamTrackProcessor({ track: videoTrack });
    // const trackGenerator = new MediaStreamTrackGenerator({ kind: 'video' });

    // trackProcessor.readable.pipeThrough(transformerReceiver).pipeTo(trackGenerator.writable);

    // remoteStream = new MediaStream([trackGenerator]);

    //Copy of the remote stream
    remoteStreamCopy = remoteStream;
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
      //pc.addStream(localStream);
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

    // const videoTrack = stream.getVideoTracks()[0];

    // const trackProcessor = new MediaStreamTrackProcessor({ track: videoTrack });
    // const trackGenerator = new MediaStreamTrackGenerator({ kind: 'video' });

    // trackProcessor.readable.pipeThrough(transformerSender).pipeTo(trackGenerator.writable);

    // localStream = new MediaStream([trackGenerator]);

    sendMessage('got user media');
    if (isInitiator) {
      maybeStart();
    }
}

//Sending bye if user closes the window
window.onbeforeunload = function() {
    sendMessage('bye');
};
