using JetBrains.Annotations;

namespace Schrodinger.GraphQL.Dto;

public class GetSchrodingerSoldRecordInput
{
    [CanBeNull] public List<int> Types { get; set; }
    public long? TimestampMin { get; set; }
    public string SortType { get; set; }
    
    public string Address { get; set; }
    public string FilterSymbol { get; set; }
    
    public string ChainId { get; set; }
    
    public int SkipCount { get; set; }
    
    public int MaxResultCount { get; set; } = 10;
}

public class GetSchrodingerSoldListInput 
{
    public long? TimestampMin { get; set; }
    public long? TimestampMax { get; set; }
    
    public string ChainId { get; set; }
}