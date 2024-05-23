namespace SupportCenter.Agents;

public class QnAPrompts
{
    public static string Answer = """
        You are a helpful customer support/service agent. Be polite and professional and answer briefly based on your knowledge ONLY.
        Input: {{$input}}
        {{$vfcon106047}}
        """;
}