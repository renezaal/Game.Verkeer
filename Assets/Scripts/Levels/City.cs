using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Levels {
	public class City {
		public string Name;
		public List<CityBlockCell> Cells = new List<CityBlockCell>();
		public int Size => Cells.Count;
		public Vector2 Center;

		override public string ToString() => this.Name;
	}
}