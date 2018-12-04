class OutputWriter {
   textArea: HTMLTextAreaElement

   constructor() {
   }

   /**
    * write
    */
   public write(text: string) {
      if (this.textArea == null) {
         this.textArea = document.getElementById("output") as HTMLTextAreaElement;
      }

      this.textArea.value += text + '\n';
   }

   /**
    * clear
    */
   public clear() {
      if (this.textArea == null) {
         this.textArea = document.getElementById("output") as HTMLTextAreaElement;
      }

      this.textArea.value = "";      
   }
}

let outputWriter = new OutputWriter();

window.onload = function(evt) {
   outputWriter.clear();
   outputWriter.write("No output yet ...");
}
