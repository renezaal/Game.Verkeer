using UnityEngine;

namespace Assets.Scripts.Levels {
	public class Cell {
		public int X => Coordinates.x;
		public int Y => Coordinates.y;

		public Cell North => Neighbours[0];
		public Cell East => Neighbours[1];
		public Cell South => Neighbours[2];
		public Cell West => Neighbours[3];

		public Vector2Int Coordinates;

		public Cell[] Neighbours = new Cell[4];
		public CellType CellType;
		public GameObject Tile = null;
	}
}