let websocket : WebSocket = null;
let commandHandler : CommandHandler = null;

function onConnectClick() {
   let input = document.getElementById("serverAddr") as HTMLInputElement
   let websocketAddress = input.value;
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
   
   websocket.onmessage = function (evt: MessageEvent) {
      let fileReader = new FileReader();
      fileReader.onload = function(event) {
         let fr = event.target as FileReader
         commandHandler.handleResponse(fr.result as string)
      };
      fileReader.readAsBinaryString(evt.data as Blob);
   };
   
   websocket.onerror = function (evt: any) {
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

function setElementEnabled(enabled: boolean, element: HTMLElement) {
   if (enabled) {
      element.classList.remove("disabled");
      element.classList.add("enabled");
   } else {
      element.classList.remove("enabled");
      element.classList.add("disabled");
   }
}

function setUIConnectionState(connected: boolean) {
   let div = document.getElementById("controlsDiv") as HTMLDivElement;
   let conBtn = document.getElementById("conBtn") as HTMLButtonElement;
   let disconBtn = document.getElementById("disconBtn") as HTMLButtonElement;

   setElementEnabled(connected, div);
   setElementEnabled(connected, disconBtn);
   setElementEnabled(!connected, conBtn);
}
