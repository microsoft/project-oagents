namespace SupportCenter.ApiService.RealTimeAudio
{
    public interface IRealTimeAudioService
    {
        Task<string> TranscribeAudioAsync(byte[] audioData);
        Task<byte[]> SynthesizeSpeechAsync(string text);
        Task StartStreamingSessionAsync(string connectionId);
        Task EndStreamingSessionAsync(string connectionId);
        Task ProcessRealtimeAudioAsync(string connectionId, string userId, string conversationId, byte[] audioData);
    }
}