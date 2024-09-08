using System.Collections.Generic;

namespace Schrodinger.GraphQL.Dto;

public class AllSchrodingerListDto
{
    public long TotalCount { get; set; }
    public List<AllSchrodingerDto> Data { get; set; } = new();
}