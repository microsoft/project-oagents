namespace SupportCenter.Agents;

public class CustomerInfoState
{
    [Id(0)]
    public CustomerInfoInternal? Info { get; set; }
}

public class CustomerInfoInternal
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}