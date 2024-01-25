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
                var yamlObject = deserializer.Deserialize(reader);
                var entriesList = ((Dictionary<object, object>)yamlObject)["Entries"] as List<object>;

                foreach (var outerEntry in entriesList)
                {
                    var entryDict = (Dictionary<object, object>)outerEntry;
                    var changesList = (List<object>)entryDict["changes"];

                    foreach (var change in changesList)
                    {
                        var changeDict = (Dictionary<object, object>)change;
                        var changeEntry = new ChangeEntry
                        {
                            Author = (string)entryDict["author"],
                            Message = (string)changeDict["message"],
                            Type = (string)changeDict["type"]
                        };
                        entries.Add(changeEntry);
                    }
                }
            }

            return entries;
        }
    }

    public class ChangeEntry
    {
        public string Author { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
    }
}
