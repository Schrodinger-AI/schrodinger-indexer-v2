namespace Schrodinger.GraphQL.Dto;

public class AdoptInfoListDto
{
    public List<string> TransactionIds { get; set; }
    public List<string> AdoptionIds { get; set; }
    public List<string> SymbolIds { get; set; }
}