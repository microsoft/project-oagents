namespace SupportCenter.ApiService.Audio
{
    public class RealTimeAudioConfiguration
    {
        public string AzureOpenAIEndpoint { get; set; } = string.Empty;
        public string AzureOpenAIKey { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
        public string VoiceName { get; set; } = "en-US-JennyNeural";
        public string SpeechRegion { get; set; } = string.Empty;
        public string SpeechKey { get; set; } = string.Empty;
    }
}
