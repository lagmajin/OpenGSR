using System.Collections.Generic;

namespace OpenGSCore
{
	public class PlayerIDList : List<PlayerID>
	{
		public bool Contains(string id)
		{
			return FindIndex(pid => pid != null && pid.ToString() == id) >= 0;
		}

		public bool Remove(string id)
		{
			var index = FindIndex(pid => pid != null && pid.ToString() == id);
			if (index < 0)
			{
				return false;
			}

			RemoveAt(index);
			return true;
		}

		public void AddUnique(PlayerID id)
		{
			if (id == null || Contains(id.ToString()))
			{
				return;
			}

			Add(id);
		}
	}
}
