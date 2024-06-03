namespace SupportCenter.Events;

public enum EventType
{
    UserChatInput,
    UserConnected,
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
    InvoiceRetrieved
}