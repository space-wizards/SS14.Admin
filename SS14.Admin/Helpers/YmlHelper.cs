using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace SS14.Admin.Helpers
{
    public class YamlHelper
    {
        public List<ChangeEntry> ReadYamlFile(string filePath)
        {
            var deserializer = new DeserializerBuilder().Build();
            var entries = new List<ChangeEntry>();

            using (var reader = new StreamReader(filePath))
            {
                var yamlObject = deserializer.Deserialize<RootObject>(reader);

                if (yamlObject.Entries != null)
                {
                    foreach (var entry in yamlObject.Entries)
                    {
                        if (entry.Changes != null && entry.Changes.Count > 0)
                        {
                            var firstChange = entry.Changes[0];

                            var changeEntry = new ChangeEntry
                            {
                                Author = entry.Author,
                                Message = firstChange.Message,
                                Type = firstChange.Type,
                                Id = entry.Id,
                                Time = entry.Time
                            };
                            entries.Add(changeEntry);
                        }
                    }
                }
            }

            return entries;
        }

        // Define the root object structure
        public class RootObject
        {
            [YamlMember(Alias = "Entries")]
            public List<YamlEntry> Entries { get; set; }
        }

        // Define the entry object structure
        public class YamlEntry
        {
            [YamlMember(Alias = "author")]
            public string Author { get; set; }

            [YamlMember(Alias = "id")]
            public string Id { get; set; }

            [YamlMember(Alias = "time")]
            public DateTime Time { get; set; }

            [YamlMember(Alias = "changes")]
            public List<YamlChange> Changes { get; set; }
        }

        // Define the change object structure
        public class YamlChange
        {
            [YamlMember(Alias = "message")]
            public string Message { get; set; }

            [YamlMember(Alias = "type")]
            public string Type { get; set; }
        }
    }

    public class ChangeEntry
    {
        public string Author { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public string Id { get; set; }
        public DateTime Time { get; set; }
    }
}
