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

//Initialize turn/stun server here
var pcConfig = turnConfig;

//Set the localStreamConstraints based on the type of call
var localStreamConstraints = {
    audio: false,
    video: { facingMode: "user" }
};

if(audio.innerHTML === "true") {
    localStreamConstraints.audio = true;
} else {
    localStreamConstraints.audio = false;
}

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

//If found local stream
function gotStream(stream) {
    console.log('Adding local stream.');
    localStream = stream;
    localVideo.srcObject = stream;
    //sendMessage('got user media', room);
    //if (isInitiator) {
    //  maybeStart();
    //}
}
