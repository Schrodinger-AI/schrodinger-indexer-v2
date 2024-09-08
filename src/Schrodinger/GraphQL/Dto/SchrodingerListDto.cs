using System.Collections.Generic;

namespace Schrodinger.GraphQL.Dto;

public class SchrodingerListDto
{
    public long TotalCount { get; set; }
    public List<SchrodingerDto> Data { get; set; } = new();
}