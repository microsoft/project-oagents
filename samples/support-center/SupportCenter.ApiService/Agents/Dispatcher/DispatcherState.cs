namespace SupportCenter.ApiService.Agents.Dispatcher;

public class DispatcherState
{
}

public class Choice(string name, string description)
{
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;
}