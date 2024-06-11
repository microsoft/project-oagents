namespace SupportCenter.Agents;

public class ConversationPrompts
{
    public static string Answer = """
        You are a helpful customer support/service agent at Contoso Electronics. Be polite, friendly and professional and answer briefly.
        Input: {{$input}}
        """;
}