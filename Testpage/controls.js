function onStartCapturePress() {
    if (websocket.readyState == WebSocket.OPEN) {
        commandHandler.sendCommand("startcapture", {});
    }
    else {
    }
}
function onGetInfoPress() {
    if (websocket.readyState == WebSocket.OPEN) {
        commandHandler.sendCommand("getinfo", {});
    }
    else {
    }
}
//# sourceMappingURL=controls.js.map