using System.Runtime.Serialization;
using Orleans;
using Orleans.CodeGeneration;

namespace Microsoft.AI.Agents.Abstractions
{
    [DataContract]
    [GenerateSerializer]
    public class Event
    {
        [Id(0)]
        public Dictionary<string, string> Data { get; set; }
        [Id(1)]
        public string Type { get; set; }
        [Id(2)]
        public string Subject { get; set; }
    }
}