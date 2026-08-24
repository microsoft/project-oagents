using System.Runtime.Serialization;
using CloudNative.CloudEvents;

namespace Microsoft.AI.Agents.Abstractions
{
    [DataContract]
    public class Event
    {
        private CloudEvent _cloudEvent;

        public Event()
        {
            _cloudEvent = new CloudEvent();
            Data = new Dictionary<string, string>();
        }

        public Event(CloudEvent cloudEvent)
        {
            _cloudEvent = cloudEvent ?? throw new ArgumentNullException(nameof(cloudEvent));
            Data = ExtractDataFromCloudEvent(cloudEvent);
        }

        [DataMember]
        public Dictionary<string, string> Data { get; set; }

        [DataMember]
        public string Type 
        { 
            get => _cloudEvent.Type ?? string.Empty;
            set => _cloudEvent.Type = value;
        }

        [DataMember]
        public string Subject 
        { 
            get => _cloudEvent.Subject ?? string.Empty;
            set => _cloudEvent.Subject = value;
        }

        public CloudEvent CloudEvent 
        { 
            get 
            {
                // Sync the Data dictionary back to CloudEvent
                if (Data.Count > 0)
                {
                    _cloudEvent.Data = Data;
                }
                return _cloudEvent;
            }
        }

        public static implicit operator CloudEvent(Event evt)
        {
            return evt.CloudEvent;
        }

        public static implicit operator Event(CloudEvent cloudEvent)
        {
            return new Event(cloudEvent);
        }

        private static Dictionary<string, string> ExtractDataFromCloudEvent(CloudEvent cloudEvent)
        {
            var data = new Dictionary<string, string>();
            
            if (cloudEvent.Data != null)
            {
                if (cloudEvent.Data is Dictionary<string, string> dictData)
                {
                    return dictData;
                }
                else if (cloudEvent.Data is Dictionary<string, object> objDict)
                {
                    foreach (var kvp in objDict)
                    {
                        data[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                    }
                }
                else
                {
                    // For other data types, we'll need to handle them differently
                    // For now, just convert to string representation
                    data["data"] = cloudEvent.Data.ToString() ?? string.Empty;
                }
            }

            return data;
        }
    }
}