var websocket = null;
var commandHandler = null;
function onConnectClick() {
    var input = document.getElementById("serverAddr");
    var websocketAddress = input.value;
    outputWriter.write("Connecting to WebSocket server at " + websocketAddress);
    websocket = new WebSocket(websocketAddress);
    commandHandler = new CommandHandler(websocket);
    websocket.onopen = function () {
        outputWriter.write("websocket connected");
        setUIConnectionState(true);
    };
    websocket.onclose = function () {
        outputWriter.write("websocket disconnected");
        setUIConnectionState(false);
    };
    websocket.onmessage = function (evt) {
        var fileReader = new FileReader();
        fileReader.onload = function (event) {
            var fr = event.target;
            commandHandler.handleResponse(fr.result);
        };
        fileReader.readAsBinaryString(evt.data);
    };
    websocket.onerror = function (evt) {
        outputWriter.write('error: ' + evt.data);
        websocket.close();
        setUIConnectionState(false);
    };
}
function onDisconnectClick() {
    if (websocket == null || websocket.readyState != WebSocket.OPEN) {
        return;
    }
    websocket.close(1000, "User disconnect");
}
function setElementEnabled(enabled, element) {
    if (enabled) {
        element.classList.remove("disabled");
        element.classList.add("enabled");
    }
    else {
        element.classList.remove("enabled");
        element.classList.add("disabled");
    }
}
function setUIConnectionState(connected) {
    var div = document.getElementById("controlsDiv");
    var conBtn = document.getElementById("conBtn");
    var disconBtn = document.getElementById("disconBtn");
    setElementEnabled(connected, div);
    setElementEnabled(connected, disconBtn);
    setElementEnabled(!connected, conBtn);
}
//# sourceMappingURL=entrypoint.js.map