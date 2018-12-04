var OutputWriter = (function () {
    function OutputWriter() {
    }
    OutputWriter.prototype.write = function (text) {
        if (this.textArea == null) {
            this.textArea = document.getElementById("output");
        }
        this.textArea.value += text + '\n';
    };
    OutputWriter.prototype.clear = function () {
        if (this.textArea == null) {
            this.textArea = document.getElementById("output");
        }
        this.textArea.value = "";
    };
    return OutputWriter;
}());
var outputWriter = new OutputWriter();
window.onload = function (evt) {
    outputWriter.clear();
    outputWriter.write("No output yet ...");
};
//# sourceMappingURL=outputwriter.js.map