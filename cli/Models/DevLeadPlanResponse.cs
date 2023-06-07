using System.Text.Json.Serialization;

public class Subtask
{
    public string description { get; set; }
    public string llm_prompt { get; set; }
}

public class Step
{
    public string description { get; set; }
    public List<Subtask> subtasks { get; set; }
}

public class DevLeadPlanResponse
{
    public List<Step> steps { get; set; }
}