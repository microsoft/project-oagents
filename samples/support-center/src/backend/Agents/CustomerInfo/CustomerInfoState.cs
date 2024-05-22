namespace SupportCenter.Agents;

public class CustomerInfoState
{
    [Id(0)]
    public Customer? Info { get; set; }
}

public class Customer
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

