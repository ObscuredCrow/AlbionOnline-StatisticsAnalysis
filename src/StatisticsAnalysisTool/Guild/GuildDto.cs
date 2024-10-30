using System.Collections.Generic;

namespace StatisticsAnalysisTool.Guild;

public class GuildDto
{
    public List<SiphonedEnergyItemDto> SiphonedEnergies { get; set; } = new ();
    public int FameRequirement { get; set; } = new();
    public string RecruitmentMessage { get; set; }
    public List<string> PlayersAlreadyInvited { get; set; } = new();
}