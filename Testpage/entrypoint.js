var Command = /** @class */ (function () {
    function Command() {
    }
    return Command;
}());
function Utf8ArrayToStr(array) {
    var out, i, len, c;
    var char2, char3;
    out = "";
    len = array.length;
    i = 0;
    while (i < len) {
        c = array[i++];
        switch (c >> 4) {
            case 0:
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
                // 0xxxxxxx
                out += String.fromCharCode(c);
                break;
            case 12:
            case 13:
                // 110x xxxx   10xx xxxx
                char2 = array[i++];
                out += String.fromCharCode(((c & 0x1F) << 6) | (char2 & 0x3F));
                break;
            case 14:
                // 1110 xxxx  10xx xxxx  10xx xxxx
                char2 = array[i++];
                char3 = array[i++];
                out += String.fromCharCode(((c & 0x0F) << 12) |
                    ((char2 & 0x3F) << 6) |
                    ((char3 & 0x3F) << 0));
                break;
        }
    }
    return out;
}
var websocketAddress = "wss://localhost:54018";
//--END OF CONFIGURATION--
var c = document.getElementById("canvas");
var ctx = c.getContext("2d");
var websocket = new WebSocket(websocketAddress);
websocket.onopen = function () {
    console.log("mjpeg-relay connected");
};
websocket.onclose = function () {
    console.log("mjpeg-relay disconnected");
};
websocket.onmessage = function (evt) {
    var fileReader = new FileReader();
    fileReader.onload = function (event) {
        var fr = event.target;
        handleResponse(fr.result);
    };
    fileReader.readAsBinaryString(evt.data);
    return;
    var image = new Image();
    image.onload = function () {
        ctx.drawImage(image, 0, 0);
    };
    image.src = evt.data;
};
websocket.onerror = function (evt) {
    console.log('error: ' + evt.data);
    websocket.close();
};
function handleResponse(resp) {
    if (resp == null) {
        console.log("Cannot handle null response");
        return;
    }
    var cmd = JSON.parse(resp);
    switch (cmd.Cmd) {
        case "error":
            console.log("Received error: " + cmd.Parameters["error"]);
            break;
        case "message":
            console.log("Received error: " + cmd.Parameters["message"]);
            break;
        case "listresult":
            for (var key in cmd.Parameters) {
                var value = cmd.Parameters[key];
                console.log(value);
            }
            break;
        default:
            console.log("unknown command: " + cmd.Cmd);
            sendCommand("error", { "error": "Unknown command: " + cmd.Cmd });
            break;
    }
}
function sendCommand(command, parameters) {
    var cmd = new Command();
    cmd.Cmd = command;
    cmd.Parameters = parameters;
    var json = JSON.stringify(cmd);
    if (websocket.readyState == websocket.OPEN) {
        console.log("Sending command: " + command);
        websocket.send(json);
    }
}
function onpress() {
    sendCommand("list", { "folder": "/Users/henning/" });
}
//# sourceMappingURL=entrypoint.js.map