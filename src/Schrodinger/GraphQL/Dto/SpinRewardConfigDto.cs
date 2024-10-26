namespace Schrodinger.GraphQL.Dto;

public class SpinRewardConfigDto
{
    public List<RewardDto>? RewardList { get; set; }
}

public class RewardDto
{
    public string? Name { get; set; }
    public long Amount { get; set; }
}