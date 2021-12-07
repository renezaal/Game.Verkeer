

namespace Assets.Scripts.Models.Levels {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public class Edge {

		public Vertex from;
		public Vertex to;
		public float cost;
		public bool Directional;

		public Edge(Vertex from, Vertex to, float cost, bool directional) {
			this.from=from;
			this.to=to;
			this.cost=cost;
			this.Directional=directional;
			from.Connections.Add(this);
			to.Connections.Add(this);
		}

		public void Disconnect() {
			from.Connections.Remove(this);
			to.Connections.Remove(this);
			from = null;
			to = null;
		}
	}
}