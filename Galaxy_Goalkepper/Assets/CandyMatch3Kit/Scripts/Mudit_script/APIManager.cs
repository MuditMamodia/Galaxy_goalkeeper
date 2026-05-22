using System.Collections.Generic;

internal class APIManager
{

    internal List<LeaderboardData> _leaderboard_data_full = new(), _my_own_data = new();
   
    
}
public class LeaderboardData
{
    public string Title; // "Weekly", "Monthly", "All Time"
    public List<TopScoreCategory> TopScores;
}

public class GamingScore
{
    public string GameId;
    public long Score;
    public string PlayTime;
    public int TotalWins;
    public int TotalGamePoints;
    public string User;
}

public class TopScoreCategory
{
    public string Title; // "Total Score", "Total Wins", "Total Game Points"
    public List<GamingScore> TopGamingScores;
}