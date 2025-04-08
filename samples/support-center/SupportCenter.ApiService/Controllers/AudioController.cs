using Microsoft.AspNetCore.Mvc;
using SupportCenter.ApiService.RealTimeAudio;

namespace SupportCenter.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AudioController(
        IRealTimeAudioService audioService, 
        ILogger<AudioController> logger) : ControllerBase
    {

        /// <summary>
        /// Synthesizes speech from provided text
        /// </summary>
        /// <param name="text">The text to convert to speech</param>
        /// <param name="voice">Optional voice name (defaults to service configuration)</param>
        /// <returns>WAV audio file</returns>
        [HttpGet("synthesize")]
        [ProducesResponseType(200, Type = typeof(FileContentResult))]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SynthesizeSpeech([FromQuery] string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    logger.LogWarning("Attempted to synthesize speech with empty text");
                    return BadRequest("Text cannot be empty");
                }

                // Enforce a reasonable limit for TTS to prevent abuse
                if (text.Length > 1000)
                {
                    logger.LogWarning("Text too long for TTS: {Length} characters", text.Length);
                    return BadRequest($"Text is too long. Maximum {text.Length} characters allowed.");
                }

                var audioData = await audioService.SynthesizeSpeechAsync(text);
                logger.LogInformation("Successfully synthesized {Length} bytes of audio from {TextLength} characters", 
                    audioData.Length, text.Length);
                
                // Return WAV audio file with appropriate cache headers for better performance
                return File(
                    fileContents: audioData, 
                    contentType: "audio/wav", 
                    fileDownloadName: "speech.wav");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Health check failed");
                return StatusCode(503, "Audio service unhealthy");
            }
        }

        /// <summary>
        /// Health check for audio services
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(200)]
        [ProducesResponseType(503)]
        public IActionResult HealthCheck()
        {
            try
            {
                // Simple health check to ensure controller is responding
                // In a production app, this would verify connection to Azure services
                return Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Health check failed");
                return StatusCode(503, "Audio service unhealthy");
            }
        }
    }
}