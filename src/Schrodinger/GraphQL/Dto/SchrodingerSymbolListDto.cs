
namespace Schrodinger.GraphQL.Dto;

public class SchrodingerSymbolListDto
{
    public long TotalCount { get; set; }
    
    public List<SchrodingerSymbolDto> Data { get; set; }
}