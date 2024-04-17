using System.Collections.Concurrent;

namespace Marketing.Hubs
{
    public static class SignalRConnectionsDB
    {
        public static ConcurrentDictionary<string, string> ConnectionIdByUser { get; set; } = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, string> AllConnections { get; set; } = new ConcurrentDictionary<string, string>();

    }
}
