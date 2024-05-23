namespace SupportCenter.Agents;

public class DispatcherPrompts
{
    public static string GetIntent = """
        You are a dispatcher agent, working with the Support Center.
        You can help customers with their issues, and you can also assign tickets to other AI agents.
        Read the customer's message carefully, and then decide the appropriate intent.
        
        If you don't know the intent, don't guess; instead respond with "Unknown".
        There may be multiple intents, but you should choose the most appropriate one.
        If you think that the message is not clear, you can ask the customer for more information.

        You can choose between: {{$choices}}.

        Here are few examples:
        - User Input: Can you send a very quick approval to the marketing team?
        - Intent: SendMessage

        - User Input: Can you send the full update to the marketing team?
        - Intent: SendEmail

        Here is the user input:
        User Input: {{$input}}
        Intent: 
        """;
}
