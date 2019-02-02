namespace EightBit
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class Symbols
    {
        private readonly Dictionary<ushort, string> labels;
        private readonly Dictionary<ushort, string> constants;
        private readonly Dictionary<string, ushort> scopes;
        private readonly Dictionary<string, ushort> addresses;
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> parsed;

        public Symbols() : this("") { }

        public Symbols(string path)
        {
            labels = new Dictionary<ushort, string>();
            constants = new Dictionary<ushort, string>();
            scopes = new Dictionary<string, ushort>();
            addresses = new Dictionary<string, ushort>();
            parsed = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

            if (path.Length > 0)
            {
                Parse(path);
                AssignSymbols();
                AssignScopes();
            }
        }

        public Dictionary<ushort, string> Labels => labels;
        public Dictionary<ushort, string> Constants => constants;
        public Dictionary<string, ushort> Scopes => scopes;
        public Dictionary<string, ushort> Addresses => addresses;

        private void AssignScopes()
        {
            var parsedScopes = parsed["scope"];
            foreach (var parsedScopeElement in parsedScopes)
            {
                var parsedScope = parsedScopeElement.Value;
                var name = parsedScope["name"];
                var trimmedName = name.Substring(1, name.Length - 2);
                var size = parsedScope["size"];
                scopes[trimmedName] = ushort.Parse(size);
            }
        }

        private void AssignSymbols()
        {
            var symbols = parsed["sym"];
            foreach (var symbolElement in symbols)
            {
                var symbol = symbolElement.Value;
                var name = symbol["name"];
                var trimmedName = name.Substring(1, name.Length - 2);
                var value = symbol["val"].Substring(2);
                var number = Convert.ToUInt16(value, 16);
                var symbolType = symbol["type"];
                if (symbolType == "lab")
                {
                    labels[number] = trimmedName;
                    addresses[trimmedName] = number;
                }
                else if (symbolType == "equ")
                {
                    constants[number] = trimmedName;
                }
            }
        }

        private void Parse(string path)
        {
            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var lineElements = line.Split(' ', '\t');
                    if (lineElements.Length == 2)
                    {
                        var type = lineElements[0];
                        var dataElements = lineElements[1].Split(',');
                        var data = new Dictionary<string, string>();
                        foreach (var dataElement in dataElements)
                        {
                            var definition = dataElement.Split('=');
                            if (definition.Length == 2)
                                data[definition[0]] = definition[1];
                        }

                        if (data.ContainsKey("id"))
                        {
                            if (!parsed.ContainsKey(type))
                                parsed[type] = new Dictionary<string, Dictionary<string, string>>();
                            var id = data["id"];
                            data.Remove("id");
                            parsed[type][id] = data;
                        }
                    }
                }
            }
        }
    }
}
