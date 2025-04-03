using Microsoft.AspNetCore.Mvc;
using SupportCenter.ApiService.RealTimeAudio;
using System;
using System.Threading.Tasks;

namespace SupportCenter.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AudioController : ControllerBase
    {
        private readonly IRealTimeAudioService _audioService;
        private readonly ILogger<AudioController> _logger;

        public AudioController(IRealTimeAudioService audioService, ILogger<AudioController> logger)
        {
            _audioService = audioService;
            _logger = logger;
        }

        [HttpGet("synthesize")]
        public async Task<IActionResult> SynthesizeSpeech([FromQuery] string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return BadRequest("Text cannot be empty");
                }

                var audioData = await _audioService.SynthesizeSpeechAsync(text);
                return File(audioData, "audio/wav");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synthesizing speech");
                return StatusCode(500, "Error synthesizing speech");
            }
        }
    }
}