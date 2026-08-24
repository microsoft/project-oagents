using Microsoft.AI.Agents.Abstractions;

namespace Marketing.Agents
{
    public interface INotary : IGrainWithStringKey
    {
        public Task<List<Event>> GetAllEvents();
    }
}
