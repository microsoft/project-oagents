namespace SupportCenter.ApiService.SignalRHub;
public class SignalRConnectionGrain([PersistentState("state", "messages")] IPersistentState<Connection> state) : Grain, IStoreConnections
{
    private IPersistentState<Connection> _state = state;

    public Task<Connection?> GetConnection()
    {
        return Task.FromResult(_state.State);
    }

    public async Task AddConnection(Connection connection)
    {
        _state.State = connection;
        await _state.WriteStateAsync();
    }

    public async Task RemoveConnection()
    {
        _state.State = null;
        await _state.WriteStateAsync();
    }
}

public interface IStoreConnections : IGrainWithStringKey
{
    Task AddConnection(Connection connection);
    Task<Connection?> GetConnection();
    Task RemoveConnection();
}

[GenerateSerializer]
public class Connection
{
    [Id(0)]
    public string? Id { get; set; }
    [Id(1)]
    public string? ConversationId { get; set; }
}