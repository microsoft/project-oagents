using Microsoft.Extensions.VectorData;

namespace SupportCenter.Shared;

public class Document
{
    [VectorStoreRecordKey]
    public string Key { get; set; }

    [VectorStoreRecordData]
    public string Description { get; set; }

    [VectorStoreRecordData]
    public string Text { get; set; }

    [VectorStoreRecordVector(3072)]
    public ReadOnlyMemory<float> Vector { get; set; }
}
