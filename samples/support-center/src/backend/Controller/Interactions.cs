using SupportCenter.Agents;
using SupportCenter.Events;
using SupportCenter.Options;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Orleans.Runtime;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SupportCenter.Agents
{

    [GenerateSerializer]
    public class Asd
    {
        [Id(0)]
        public string Name { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class Interactions : ControllerBase
    {
        private readonly IClusterClient _client;

        public Interactions(IClusterClient client)
        {
            _client = client;
        }

        // POST api/<Post>/5
        [HttpPost("{UserId}")]public async Task<string> Post(string UserId, [FromBody] string userMessage)
        {
            var streamProvider = _client.GetStreamProvider("StreamProvider");
            var streamId = StreamId.Create(Consts.OrleansNamespace, UserId);
            var stream = streamProvider.GetStream<Event>(streamId);

            var data = new Dictionary<string, string>
            {
                { nameof(UserId), UserId.ToString() },
                { nameof(userMessage), userMessage },
            };

            await stream.OnNextAsync(new Event
            {
                Type = nameof(EventTypes.UserQuestionReceived),
                Data = data
            });

            return $"Task {UserId} accepted";
        }
    }
}
