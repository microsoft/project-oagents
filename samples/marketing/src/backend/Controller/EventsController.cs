using Marketing.Agents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Marketing.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IClusterClient _client;

        public EventsController(IClusterClient client)
        {
            _client = client;
        }

        [HttpGet("{id}")]
        // GET: EventsController
        public async Task<string> Get(string id)
        {
            var grain = _client.GetGrain<INotary>(id);
            var allEvents = await grain.GetAllEvents();
            return JsonSerializer.Serialize(allEvents);
        }
    }
}
