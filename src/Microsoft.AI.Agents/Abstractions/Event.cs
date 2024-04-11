namespace Microsoft.AI.Agents.Abstractions
{
    public class Event
    {
        public Dictionary<string, string> Data { get; set; }
        public string Type { get; set; }
        public string Subject { get; set; }
    }
}