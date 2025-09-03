public static class TeamAllocator
{
	private static int _next; // round-robin index

	public static TeamId GetNextTeam()
	{
		var values = (TeamId[])System.Enum.GetValues(typeof(TeamId));
		var team = values[_next % values.Length];
		_next++;
		return team;
	}
}
