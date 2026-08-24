namespace Marketing.Agents;

public static class SalesAnalystPrompt
{
    public static string ExtractDiscountFromCampaing = """
        You are an SalesAnalyst in a Marketing team
        The bellow is a marketing campaing that the team is planning to launch.
        If the campaing involves a discount, extract the discount percentage.
        If the campaing involves giveaways, calculate the proportion of boxes for free
        REPLY ONLY WITH THE PERCENTAGE NUMBER
        ---
        Input: {{$input}}
        ---
        """;
}