// <copyright file="Symbols.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System.Globalization;

    public class Symbols
    {
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> parsed = [];

        public Symbols()
        : this(string.Empty)
        {
        }

        public Symbols(string? path)
        {
            ArgumentNullException.ThrowIfNull(path);
            if (path.Length > 0)
            {
                this.Parse(path);
                this.AssignSymbols();
                this.AssignScopes();
            }
        }

        public Dictionary<ushort, string> Labels { get; } = [];

        public Dictionary<ushort, string> Constants { get; } = [];

        public Dictionary<string, ushort> Scopes { get; } = [];

        public Dictionary<string, ushort> Addresses { get; } = [];

        private void AssignScopes()
        {
            var parsedScopes = this.parsed["scope"];
            foreach (var parsedScopeElement in parsedScopes)
            {
                var parsedScope = parsedScopeElement.Value;
                var name = parsedScope["name"];
                var trimmedName = name[1..^1];
                var size = parsedScope["size"];
                this.Scopes[trimmedName] = ushort.Parse(size, CultureInfo.InvariantCulture);
            }
        }

        private void AssignSymbols()
        {
            var symbols = this.parsed["sym"];
            foreach (var symbolElement in symbols)
            {
                var symbol = symbolElement.Value;
                var name = symbol["name"];
                var trimmedName = name[1..^1];
                var value = symbol["val"][2..];
                var number = Convert.ToUInt16(value, 16);
                var symbolType = symbol["type"];
                if (symbolType == "lab")
                {
                    this.Labels[number] = trimmedName;
                    this.Addresses[trimmedName] = number;
                }
                else if (symbolType == "equ")
                {
                    this.Constants[number] = trimmedName;
                }
            }
        }

        private void Parse(string path)
        {
            using var reader = new StreamReader(path);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null)
                    break;
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
                        {
                            data[definition[0]] = definition[1];
                        }
                    }

                    if (data.ContainsKey("id"))
                    {
                        if (!this.parsed.ContainsKey(type))
                        {
                            this.parsed[type] = [];
                        }

                        var id = data["id"];
                        data.Remove("id");
                        this.parsed[type][id] = data;
                    }
                }
            }
        }
    }
}
