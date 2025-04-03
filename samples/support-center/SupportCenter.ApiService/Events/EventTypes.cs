namespace SupportCenter.ApiService.Events;

public enum EventType
{
    UserChatInput,
    UserConnected,
    UserNewConversation,
    AgentNotification,
    Unknown,

    // Domain specific events
    CustomerInfoRequested,
    CustomerInfoRetrieved,
    QnARequested,
    QnARetrieved,
    DiscountRequested,
    DiscountRetrieved,
    InvoiceRequested,
    InvoiceRetrieved,
    ConversationRequested,
    ConversationRetrieved,

    // Voice-related events
    VoiceSessionStarted,
    VoiceSessionEnded,
    VoiceTranscription,

    // Notification events
    DispatcherNotification,
    CustomerInfoNotification,
    QnANotification,
    DiscountNotification,
    InvoiceNotification
}