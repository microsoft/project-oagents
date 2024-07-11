namespace SupportCenter.AgentsConfigurationFactory
{
    public static class AgentConfiguration
    {
        public static IAgentConfiguration GetAgentConfiguration(string agent)
        {
            return agent switch
            {
                "Invoice" => new InvoiceAgentConfiguration(),
                "Conversation" => new ConversationAgentConfiguration(),
                "CustomerInfo" => new CustomerInfoAgentConfiguration(),
                "QnA" => new QnAAgentConfiguration(),
                "Dispatcher" => new DispatcherAgentConfiguration(),
                "Discount" => new DiscountAgentConfiguration(),
                "SignalR" => new SignalRAgentConfiguration(),
                _ => throw new ArgumentException("Unsupported agent type", nameof(agent)),
            };
        }
    }
}
