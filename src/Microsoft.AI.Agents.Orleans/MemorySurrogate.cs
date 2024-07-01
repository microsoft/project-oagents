using Microsoft.AI.Agents.Abstractions;

namespace Microsoft.AI.Agents.Orleans;


[GenerateSerializer]
public struct MemoryItemSurrogate
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string Memory { get; set; }
    [Id(2)]
    public List<string> Tags { get; set; }
}

[RegisterConverter]
public sealed class MemoryItemSurrogateConverter :
    IConverter<MemoryItem, MemoryItemSurrogate>
{
    public MemoryItem ConvertFromSurrogate(
        in MemoryItemSurrogate surrogate) => new MemoryItem {
            Id  = surrogate.Id,
            Memory = surrogate.Memory,
            Tags = surrogate.Tags
        };

    public MemoryItemSurrogate ConvertToSurrogate(in MemoryItem value) =>
     new MemoryItemSurrogate {
        Id = value.Id,
        Memory = value.Memory,
        Tags = value.Tags
     };
}
