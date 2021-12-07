namespace Assets.Scripts.Levels {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public class Level : LevelBase {
		public static Level FromBase(LevelBase levelBase) {
			Level level = new Level();
			level.AvailableCityNames.AddRange(levelBase.AvailableCityNames);
			level.DirectoryPath = levelBase.DirectoryPath;
			level.Funds = levelBase.Funds;
			level.Name = levelBase.Name;
			return level;
		}

		public Cell[,] Cells = new Cell[0,0];

		public int Width => Cells.GetLength(0);
		public int Height => Cells.GetLength(1);

		public List<City> Cities = new List<City>();
	}
}
