using BoilerPlate.Events;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.DevTeam;
using Microsoft.AI.DevTeam.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Amqp.Framing;
using Orleans.Runtime;
using System.IO;
using System.Runtime.CompilerServices;

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

        // GET: api/<Post>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<Post>/5
        [HttpGet("{id}")]
        public async Task<string> Get(int id, string context)
        {
            var streamProvider = _client.GetStreamProvider("StreamProvider");
            var streamId = StreamId.Create(Consts.OrleansNamespace, id);
            var stream = streamProvider.GetStream<Event>(streamId);


            var data = new Dictionary<string, string>
            {
                { nameof(id), id.ToString() },
                { nameof(context), context },
            };

            await stream.OnNextAsync(new Event
            {
                Type = nameof(EventTypes.NewRequest),
                Message = id.ToString(),
                Data = data
            });

            return $"Task {id} accepted";
        }

        // POST api/<Post>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<Post>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<Post>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
