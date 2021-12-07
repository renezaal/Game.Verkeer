namespace Assets.Scripts.Models.Levels {
	using Assets.Scripts.Models.Levels;

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using UnityEngine;

	public class Vertex {
		private readonly Func<Vector2> positionGetter;
		public Vertex(PathCell containingCell, Func<Vector2> positionGetter) {
			this.ContainingCell = containingCell;
			this.positionGetter=positionGetter;
		}
		/// <summary>
		/// True if an actor holds claim to this vertex.
		/// </summary>
		public Car Occupant { get; set; } = null;

		/// <summary>
		/// Costs of the path from this vertex to each possible target.
		/// </summary>
		public Dictionary<object, float> PathCosts { get; } = new Dictionary<object, float>();

		/// <summary>
		/// The neighbours of this vertex and the associated cost of going from this vertex to that neighbour.
		/// </summary>
		public List<Edge> Connections { get; } = new();

		public PathCell ContainingCell { get; }
		public Vector2 Position => positionGetter();

		public override string ToString() => $"{this.Position}@{this.ContainingCell}";
	}
}
