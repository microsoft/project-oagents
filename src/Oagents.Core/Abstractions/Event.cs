using System.Runtime.Serialization;

namespace Microsoft.AI.Agents.Abstractions
{
    [DataContract]
    public class Event
    {
        [DataMember]
        public Dictionary<string, string> Data { get; set; }
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public string Subject { get; set; }
    }
}