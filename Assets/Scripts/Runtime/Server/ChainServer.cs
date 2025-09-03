using System.Collections.Generic;
using Unity.Netcode;

public static class ChainServer
{
	/// Server-only: steal from startFollower (inclusive) to tail, append to captor.
	public static void StealSegment(PlayerMovement victim, DucklingFollower startFollower, PlayerMovement captor)
	{
		var nm = NetworkManager.Singleton;
		if (nm == null || !nm.IsServer) return;
		if (victim == null || startFollower == null || captor == null) return;

		if (!victim.TryIndexOf(startFollower, out int startIndex)) return;

		var slice = victim.SliceFrom(startIndex);
		var segment = new List<DucklingFollower>(slice.Count);
		segment.AddRange(slice);

		victim.RemoveFrom(startIndex);
		captor.AddSegment(segment);
	}
}