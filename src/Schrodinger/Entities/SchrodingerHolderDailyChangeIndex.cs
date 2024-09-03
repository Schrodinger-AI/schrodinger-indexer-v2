using AeFinder.Sdk.Entities;
using Nest;

namespace Schrodinger.Entities;

public class SchrodingerHolderDailyChangeIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string  Address{ get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string Date { get; set; }
    public long ChangeAmount { get; set; }
    public long Balance { get; set; }
}