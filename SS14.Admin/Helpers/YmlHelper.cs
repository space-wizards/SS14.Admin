using YamlDotNet.Serialization;
using System.IO;
using System.Collections.Generic;

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
                var yamlObject = deserializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(reader);

                foreach (var entry in yamlObject["Entries"])
                {
                    var entryDict = (Dictionary<string, object>)entry;

                    var changeEntry = new ChangeEntry
                    {
                        Author = (string)entryDict["author"],
                        Title = (string)entryDict["title"],
                        Message = (string)entryDict["message"],
                        Type = (string)entryDict["type"],
                        Id = (string)entryDict["id"]
                    };
                    entries.Add(changeEntry);
                }
            }

            return entries;
        }
    }

    public class ChangeEntry
    {
        public string Author { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public string Id { get; set; }
    }
}
