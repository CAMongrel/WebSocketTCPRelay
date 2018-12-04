interface ICommand {
   Cmd : string;
   Parameters : { [key: string] : string };
}

class Command implements ICommand {
   Cmd : string;
   Parameters : { [key: string] : string };
}

class CommandHandler {
   websocket: WebSocket;

   constructor(setWebsocket: WebSocket) {
      this.websocket = setWebsocket;
   }

   public handleResponse(resp: string) {
      if (resp == null) {
         outputWriter.write("Cannot handle null response");
         return;
      }
   
      let cmd = <ICommand>JSON.parse(resp)
   
      switch (cmd.Cmd) {
         case "error":
         outputWriter.write("Received error: " + cmd.Parameters["error"]);
            break;
   
         case "message":
         outputWriter.write("Received error: " + cmd.Parameters["message"]);
            break;
   
         case "listresult":
            for (let key in cmd.Parameters) {
               let value = cmd.Parameters[key];
               outputWriter.write(value);
            }
            break;
            
         default:
         outputWriter.write("unknown command: " + cmd.Cmd);
            this.sendCommand("error", { "error" : "Unknown command: " + cmd.Cmd });
            break;
      }
   }
   
   public sendCommand(command: string, parameters: { [key: string] : string }) {
      let cmd = new Command()
      cmd.Cmd = command;
      cmd.Parameters = parameters;
   
      let json = JSON.stringify(cmd);
      if (this.websocket.readyState == WebSocket.OPEN) {
         outputWriter.write("Sending command: " + command);
         this.websocket.send(json);
      }
   }
}
