using OpenAI.Audio;
using static SupportCenter.ApiService.Consts;

namespace SupportCenter.ApiService.RealTimeAudio
{
    public interface IRealTimeAudioService
    {
        Task<string> TranscribeAudioAsync(byte[] audioData);
        Task<byte[]> SynthesizeSpeechAsync(string text);
        Task StartStreamingSessionAsync(string connectionId);
        Task EndStreamingSessionAsync(string connectionId);
    }

    public class RealTimeAudioService(
        ILogger<RealTimeAudioService> logger,
        [FromKeyedServices(Whisper)] AudioClient audioClient) : IRealTimeAudioService
    {
        private readonly GeneratedSpeechVoice _speechVoice = GeneratedSpeechVoice.Alloy;

        public async Task<string> TranscribeAudioAsync(byte[] audioData)
        {
            try
            {
                using var audioStream = new MemoryStream(audioData);

                var response = await audioClient.TranscribeAudioAsync(audioStream, "audio");
                return response.Value.Text;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error transcribing audio");
                throw;
            }
        }

        public async Task<byte[]> SynthesizeSpeechAsync(string text)
        {
            try
            {
                var options = new SpeechGenerationOptions
                {
                    ResponseFormat = GeneratedSpeechFormat.Wav,
                    SpeedRatio = 1.0f,
                };

                using var speechStream = new MemoryStream();
                 await audioClient.GenerateSpeechAsync(text, _speechVoice, options);
                return speechStream.ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error synthesizing speech");
                throw;
            }
        }

        public async Task StartStreamingSessionAsync(string connectionId)
        {
            
        }

        public async Task EndStreamingSessionAsync(string connectionId)
        {
            
        }
    }
}
