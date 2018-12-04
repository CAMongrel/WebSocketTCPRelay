var Command = (function () {
    function Command() {
    }
    return Command;
}());
var CommandHandler = (function () {
    function CommandHandler(setWebsocket) {
        this.websocket = setWebsocket;
    }
    CommandHandler.prototype.handleResponse = function (resp) {
        if (resp == null) {
            outputWriter.write("Cannot handle null response");
            return;
        }
        var cmd = JSON.parse(resp);
        switch (cmd.Cmd) {
            case "error":
                outputWriter.write("Received error: " + cmd.Parameters["error"]);
                break;
            case "message":
                outputWriter.write("Received error: " + cmd.Parameters["message"]);
                break;
            case "listresult":
                for (var key in cmd.Parameters) {
                    var value = cmd.Parameters[key];
                    outputWriter.write(value);
                }
                break;
            default:
                outputWriter.write("unknown command: " + cmd.Cmd);
                this.sendCommand("error", { "error": "Unknown command: " + cmd.Cmd });
                break;
        }
    };
    CommandHandler.prototype.sendCommand = function (command, parameters) {
        var cmd = new Command();
        cmd.Cmd = command;
        cmd.Parameters = parameters;
        var json = JSON.stringify(cmd);
        if (this.websocket.readyState == WebSocket.OPEN) {
            outputWriter.write("Sending command: " + command);
            this.websocket.send(json);
        }
    };
    return CommandHandler;
}());
//# sourceMappingURL=commands.js.map