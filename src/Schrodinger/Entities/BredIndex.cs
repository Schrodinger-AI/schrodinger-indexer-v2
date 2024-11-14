using AeFinder.Sdk.Entities;
using Nest;

namespace Schrodinger.Entities;

public class BredIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string AdoptId { get; set; }
    [Keyword] public string Tick { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string Adopter { get; set; }
    [Keyword] public string AdoptIdA { get; set; }
    [Keyword] public string AdoptIdB { get; set; }
    [Keyword] public string TransactionId { get; set; }
    public long AmountA { get; set; }
    public long AmountB { get; set; }

    public long ConsumeAmountA { get; set; } = 100000000;
    public long ConsumeAmountB { get; set; } = 100000000;
    
    public long InputAmount { get; set; }
    public long OutputAmount { get; set; }
    public int Gen { get; set; }
    public int Rank { get; set; }
    public long Level { get; set; }
    public DateTime AdoptTime { get; set; }
}