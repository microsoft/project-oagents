using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.DevTeam;
using Microsoft.AI.DevTeam.Events;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Orleans.Runtime;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BoilerPlate.Controller
{

    [GenerateSerializer]
    public class Asd
    {
        [Id(0)]
        public string Name { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class Articles : ControllerBase
    {
        private readonly IClusterClient _client;

        public Articles(IClusterClient client)
        {
            _client = client;
        }
        // GET api/<Post>/5
        [HttpGet("{id}")]
        public async Task<string> Get(string id)
        {
            var grain = _client.GetGrain<IWriter>(id);
            string article = await grain.GetArticle();
            return article;
        }

        // PUT api/<Post>/5
        [HttpPut("{id}")]
        public async Task<string> Put(string id, [FromBody] string userMessage)
        {
            var streamProvider = _client.GetStreamProvider("StreamProvider");
            var streamId = StreamId.Create(Consts.OrleansNamespace, id);
            var stream = streamProvider.GetStream<Event>(streamId);


            var data = new Dictionary<string, string>
            {
                { nameof(id), id.ToString() },
                { nameof(userMessage), userMessage },
            };

            await stream.OnNextAsync(new Event
            {
                Type = nameof(EventTypes.UserChatInput),
                Message = userMessage,
                Data = data
            });

            return $"Task {id} accepted";
        }

    }
}
