﻿using Marketing.Agents;
using Marketing.Events;
using Marketing.Options;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Orleans.Runtime;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Marketing.Controller
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
        [HttpPut("{sessionId}")]
        public async Task<string> Put(string sessionId, [FromBody] string userMessage)
        {
            var streamProvider = _client.GetStreamProvider("StreamProvider");
            var streamId = StreamId.Create(Consts.OrleansNamespace, sessionId);
            var stream = streamProvider.GetStream<Event>(streamId);

            var data = new Dictionary<string, string>
            {
                { nameof(sessionId), sessionId },
                { nameof(userMessage), userMessage },
            };

            await stream.OnNextAsync(new Event
            {
                Type = nameof(EventTypes.UserChatInput),
                Data = data
            });

            return $"Task {sessionId} accepted";
        }
    }
}
