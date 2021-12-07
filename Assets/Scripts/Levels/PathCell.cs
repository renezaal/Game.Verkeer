namespace Assets.Scripts.Levels {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using UnityEngine;

	public class PathCell : Cell {
		public const float SidePadding = .3f;
		private readonly static Vector2 middleOffset = new(.5f, .5f);
		private readonly static Vector2 NeOffset = new(1-SidePadding, 1-SidePadding);
		private readonly static Vector2 SeOffset = new(1-SidePadding, SidePadding);
		private readonly static Vector2 SwOffset = new(SidePadding, SidePadding);
		private readonly static Vector2 NwOffset = new(SidePadding, 1-SidePadding);

		public PathCell() {
			// 4 vertices per cell.
			Vertices = new Vertex[] {
				new(this, () => this.VertexNEPosition),
				new(this, () => this.VertexSEPosition),
				new(this, () => this.VertexSWPosition),
				new(this, () => this.VertexNWPosition),
			};
		}

		public Vertex[] Vertices;

		public float MaxSpeed;

		public Vertex VertexNE => Vertices[0];
		public Vertex VertexSE => Vertices[1];
		public Vertex VertexSW => Vertices[2];
		public Vertex VertexNW => Vertices[3];

		public Vector2 MiddlePosition => this.Coordinates + middleOffset;
		public Vector2 VertexNEPosition => this.Coordinates + NeOffset;
		public Vector2 VertexSEPosition => this.Coordinates + SeOffset;
		public Vector2 VertexSWPosition => this.Coordinates + SwOffset;
		public Vector2 VertexNWPosition => this.Coordinates + NwOffset;

		public override string ToString() => $"{this.CellType}@{this.Coordinates}";
	}
}
