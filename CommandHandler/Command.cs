using System;
using System.Collections.Generic;

namespace CommandHandler
{
    public class Command
    {
        public string Cmd;
        public Dictionary<string, string> Parameters;

        public string this[string param]
        {
            get
            {
                if (Parameters.ContainsKey(param))
                {
                    return Parameters[param]; 
                }
                return "undefined";
            }
        }

        public Command()
        {
            Cmd = "Unknown";
            Parameters = new Dictionary<string, string>();
        }
    }
}
