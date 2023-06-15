`use strict`;

// code in worker.js file
// Sender transform
function createSenderTransform() {
return new TransformStream({
    start() {
    // Called on startup.
    },

    async transform(encodedFrame, controller) {
        console.log("Frame sent");
    let view = new DataView(encodedFrame.data);
    // Create a new buffer with 4 additional bytes.
    let newData = new ArrayBuffer(encodedFrame.data.byteLength + 4);
    let newView = new DataView(newData);

    // Fill the new buffer with a negated version of all
    // the bits in the original frame.
    for (let i = 0; i < encodedFrame.data.byteLength; ++i)
        newView.setInt8(i, ~view.getInt8(i));
    // Set the padding bytes to zero.
    for (let i = 0; i < 4; ++i)
        newView.setInt8(encodedFrame.data.byteLength + i, 0);

    // Replace the frame's data with the new buffer.
    encodedFrame.data = newData;

    // Send it to the output stream.
    controller.enqueue(encodedFrame);
    },

    flush() {
    // Called when the stream is about to be closed.
    }
});
}

// Receiver transform
function createReceiverTransform() {
return new TransformStream({
    start() {},
    flush() {},
    async transform(encodedFrame, controller) {
        console.log("Frame received");
    // Reconstruct the original frame.
    const view = new DataView(encodedFrame.data);

    // Ignore the last 4 bytes
    const newData = new ArrayBuffer(encodedFrame.data.byteLength - 4);
    const newView = new DataView(newData);

    // Negate all bits in the incoming frame, ignoring the
    // last 4 bytes
    for (let i = 0; i < encodedFrame.data.byteLength - 4; ++i)
        newView.setInt8(i, ~view.getInt8(i));

    encodedFrame.data = newData;
    controller.enqueue(encodedFrame);
    }
});
}

//Generic transform
function createGenericTransform() {
    return new TransformStream({
        start() {},
        flush() {},
        async transform(encodedFrame, controller) {
            // Reconstruct the original frame.
            console.log("Frame received");
            const view = new DataView(encodedFrame.data);

            const newData = new ArrayBuffer(encodedFrame.data.byteLength);
            const newView = new DataView(newData);

            // Negate all bits in the incoming frame, ignoring the
            // last 4 bytes
            for (let i = 0; i < encodedFrame.data.byteLength; ++i)
                newView.setInt8(i, ~view.getInt8(i));

            encodedFrame.data = newData;
            controller.enqueue(encodedFrame);
        }
    })
}

// Code to instantiate transform and attach them to sender/receiver pipelines.
onrtctransform = (event) => {
    let transform;
    if (event.transformer.options.name == "senderTransform")
        transform = createSenderTransform();
    else if (event.transformer.options.name == "receiverTransform")
        transform = createReceiverTransform();
    else
        transform = createGenericTransform();
    event.transformer.readable
        .pipeThrough(transform)
        .pipeTo(event.transformer.writable);
    };