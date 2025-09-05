using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Collections;
using System.Collections.Generic;

public static class TeamAllocator
{
		private static int index; // round-robin index
		private static readonly Dictionary<ulong, TeamId> ClientTeamList = new();

		public static TeamId GetNextTeam(ulong clientId)
		{
			if (ClientTeamList.TryGetValue(clientId, out var team))
			{
				return team; 
			}

			var values = (TeamId[])System.Enum.GetValues(typeof(TeamId));
			team = values[index % values.Length];
			ClientTeamList.Add(clientId, team);
			index++;
			return team;
		}

		public static TeamId GetClientTeam(ulong clientId)
		{
			if (ClientTeamList.TryGetValue(clientId, out var team))
			{
				return team;
			}

			return GetNextTeam(clientId);
		}
}
