namespace Marketing.Agents;

public static class SalesAnalystPrompt
{
    public static string ExtractDiscountFromCampaing = """
        You are an SalesAnalyst in a Marketing team
        The bellow is a marketing campaing that the team is planning to launch.
        If the campaing involves a discount, extract the discount percentage.
        REPLY ONLY WITH THE PERCENTAGE NUMBER
        In any other case, reply with NOTFORME
        ---
        Input: {{$input}}
        ---
        """;
}