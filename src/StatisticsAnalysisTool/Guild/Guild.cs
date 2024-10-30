using System.Collections.Generic;

namespace StatisticsAnalysisTool.Guild;

public class Guild
{
    public string GuildId { get; set; }
    public List<SiphonedEnergyItem> SiphonedEnergies { get; set; } = new ();
    public int FameRequirement { get; set; } = new();
    public string RecruitmentMessage { get; set; }
    public List<string> PlayersAlreadyInvited { get; set; } = new();
}