
using System.Collections.Generic;

namespace Assets.Scripts.Models.Levels {
	public class LevelBase {

		/// <summary>
		/// Name used as folder name.
		/// </summary>
		public string DirectoryPath;

		/// <summary>
		/// Human-readable name of the level.
		/// </summary>
		public string Name;

		/// <summary>
		/// The current amount of available funds for the player.
		/// </summary>
		public double Funds;

		/// <summary>
		/// City names not in use yet.
		/// </summary>
		public List<string> AvailableCityNames = new List<string>();
	}
}