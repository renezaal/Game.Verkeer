namespace Assets.Scripts.PathFinding {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public static class CostCalculator {
		/// <summary>
		/// Gets the neighbours for a vertex <typeparamref name="T"/>.
		/// A vertex is considered a neighbour when there is a direct edge connecting the two.
		/// </summary>
		/// <typeparam name="T">The type of the vertices.</typeparam>
		/// <param name="forVertex">The vertex to get the neighbours for.</param>
		/// <returns>All neighbours of the vertex.</returns>
		public delegate IEnumerable<T> GetNeighbours<T>(T forVertex);

		/// <summary>
		/// Gets the cost for pathing from <paramref name="fromVertex"/> to <paramref name="toVertex"/>.
		/// Should only be used for neighbours.
		/// </summary>
		/// <typeparam name="T">The type of the vertices.</typeparam>
		/// <param name="fromVertex">The vertex to path from.</param>
		/// <param name="toVertex">The vertex to path to.</param>
		/// <returns>The cost for the path.</returns>
		public delegate float GetCost<T>(T fromVertex, T toVertex);

		/// <summary>
		/// Calculates the path cost from the <paramref name="fromVertex"/> to each "reachable" vertex.
		/// A vertex 
		/// </summary>
		/// <typeparam name="T">The type of the vertices.</typeparam>
		/// <param name="fromVertex">The vertex to start from.</param>
		/// <param name="getNeighbours">Get the neighbours of the given vertex.</param>
		/// <param name="getCost">Gets the cost of one vertex to another. <see cref="float.PositiveInfinity"/> indicates there is no direct connection.</param>
		/// <returns>A list of all reachable vertices with their associated path costs.</returns>
		public static IEnumerable<(T vertex, float pathCost)> CalculatePathCosts<T>(T fromVertex, GetNeighbours<T> getNeighbours, GetCost<T> getCost) {
			Dictionary<T, float> pathCosts = new Dictionary<T, float>();

			Queue<T> queue = new Queue<T>();
			queue.Enqueue(fromVertex);
			pathCosts[fromVertex] = 0;

			while(queue.Count > 0) {
				T vertex = queue.Dequeue();
				float pathCost = pathCosts[vertex];

				foreach(T neighbour in getNeighbours(vertex)) {
					float neighbourPathCost = pathCost + getCost(vertex, neighbour);
					if(neighbourPathCost < pathCosts.GetValueOrDefault(neighbour, float.PositiveInfinity)) {
						queue.Enqueue(neighbour);
						pathCosts[neighbour] = neighbourPathCost;
					}
				}
			}

			return pathCosts.Select(keyValuePair => (keyValuePair.Key, keyValuePair.Value));
		}

		/// <summary>
		/// Gets all edges connected to <paramref name="forVertex" />.
		/// </summary>
		/// <typeparam name="TVertex">The type of vertex.</typeparam>
		/// <typeparam name="TEdge">The type of edge.</typeparam>
		/// <param name="forVertex">The vertex to get the edges for.</param>
		/// <returns>All edges connected to <paramref name="forVertex" />.</returns>
		public delegate IEnumerable<TEdge> GetEdges<TVertex, TEdge>(TVertex forVertex);

		/// <summary>
		/// Gets the vertex from which the edge originates.
		/// </summary>
		/// <typeparam name="TVertex">The type of vertex.</typeparam>
		/// <typeparam name="TEdge">The type of edge.</typeparam>
		/// <param name="edge">The edge to get the value for.</param>
		/// <returns>The vertex where the edge originates.</returns>
		public delegate TVertex EdgeFromGetter<TVertex, TEdge>(TEdge edge);

		/// <summary>
		/// Gets the vertex at which the edge terminates.
		/// </summary>
		/// <typeparam name="TVertex">The type of vertex.</typeparam>
		/// <typeparam name="TEdge">The type of edge.</typeparam>
		/// <param name="edge">The edge to get the value for.</param>
		/// <returns>The vertex where the edge terminates.</returns>
		public delegate TVertex EdgeToGetter<TVertex, TEdge>(TEdge edge);

		/// <summary>
		/// Gets te cost of traversing the edge.
		/// </summary>
		/// <typeparam name="TEdge">The type of edge.</typeparam>
		/// <param name="edge">The edge to get the value for.</param>
		/// <returns>The cost of traversing the edge.</returns>
		public delegate float EdgeCostGetter<TEdge>(TEdge edge);

		/// <summary>
		/// Gets whether or not the edge is one-directional (only allows traversal from vertex A to B and not the other way around).
		/// </summary>
		/// <typeparam name="TEdge">The type of edge.</typeparam>
		/// <param name="edge">The edge to check for directionality.</param>
		/// <returns>True if the edge is one-directional, false otherwise.</returns>
		public delegate bool EdgeIsDirectionalGetter<TEdge>(TEdge edge);

		/// <summary>
		/// Calculates the path cost from the <paramref name="fromVertex"/> to each "reachable" vertex.
		/// A vertex is considered reachable when the path cost is less than <see cref="float.PositiveInfinity"/>.
		/// </summary>
		/// <typeparam name="TVertex">The type of the vertices.</typeparam>
		/// <typeparam name="TEdge">The type of the edges.</typeparam>
		/// <param name="fromVertex">The vertex to start pathing from.</param>
		/// <param name="getEdges">Getter for all edges connected to a vertex.</param>
		/// <param name="edgeFrom">Getter for the vertex at which the edge originates.</param>
		/// <param name="edgeTo">Getter for the vertex at which the edge terminates.</param>
		/// <param name="edgeIsDirectional">Getter for checking if the edge is directionally constrained.</param>
		/// <param name="getCost">Getter for the cost of traversing the edge.</param>
		/// <returns>A list of all reachable vertices with their associated path costs.</returns>
		public static IEnumerable<(TVertex vertex, float pathCost)> CalculatePathCosts<TVertex, TEdge>(
			TVertex fromVertex,
			GetEdges<TVertex, TEdge> getEdges,
			EdgeFromGetter<TVertex, TEdge> edgeFrom,
			EdgeToGetter<TVertex, TEdge> edgeTo,
			EdgeIsDirectionalGetter<TEdge> edgeIsDirectional,
			EdgeCostGetter<TEdge> getCost) {

			Dictionary<TVertex, float> pathCosts = new Dictionary<TVertex, float>();

			Queue<TVertex> queue = new Queue<TVertex>();
			queue.Enqueue(fromVertex);
			pathCosts[fromVertex] = 0;

			return calculateCostsCore(null, getEdges, edgeFrom, edgeTo, edgeIsDirectional, getCost, pathCosts, queue);
		}

		/// <summary>
		/// Calculates the path cost to the <paramref name="toVertex"/> from each "reachable" vertex.
		/// A vertex is considered reachable when the path cost is less than <see cref="float.PositiveInfinity"/>.
		/// </summary>
		/// <typeparam name="TVertex">The type of the vertices.</typeparam>
		/// <typeparam name="TEdge">The type of the edges.</typeparam>
		/// <param name="toVertex">The vertex to start pathing to.</param>
		/// <param name="getEdges">Getter for all edges connected to a vertex.</param>
		/// <param name="edgeFrom">Getter for the vertex at which the edge originates.</param>
		/// <param name="edgeTo">Getter for the vertex at which the edge terminates.</param>
		/// <param name="edgeIsDirectional">Getter for checking if the edge is directionally constrained.</param>
		/// <param name="getCost">Getter for the cost of traversing the edge.</param>
		/// <returns>A list of all reachable vertices with their associated path costs.</returns>
		public static IEnumerable<(TVertex vertex, float pathCost)> CalculateReversePathCosts<TVertex, TEdge>(
			TVertex toVertex,
			GetEdges<TVertex, TEdge> getEdges,
			EdgeFromGetter<TVertex, TEdge> edgeFrom,
			EdgeToGetter<TVertex, TEdge> edgeTo,
			EdgeIsDirectionalGetter<TEdge> edgeIsDirectional,
			EdgeCostGetter<TEdge> getCost) {

			return CalculatePathCosts(toVertex, getEdges, (e) => edgeTo(e), (e) => edgeFrom(e), edgeIsDirectional, getCost);
		}

		public delegate float CurrentPathCostGetter<TVertex>(TVertex vertex);

		/// <summary>
		/// Calculates the path cost from the <paramref name="fromVertex"/> to each "reachable" vertex.
		/// A vertex is considered reachable when the path cost is less than <see cref="float.PositiveInfinity"/>.
		/// </summary>
		/// <typeparam name="TVertex">The type of the vertices.</typeparam>
		/// <typeparam name="TEdge">The type of the edges.</typeparam>
		/// <param name="fromVertex">The vertex to start pathing from.</param>
		/// <param name="edges">Getter for all edges connected to a vertex.</param>
		/// <param name="fromVertex">Getter for the vertex at which the edge originates.</param>
		/// <param name="toVertex">Getter for the vertex at which the edge terminates.</param>
		/// <param name="isDirectional">Getter for checking if the edge is directionally constrained.</param>
		/// <param name="cost">Getter for the cost of traversing the edge.</param>
		/// <returns>A list of all reachable vertices with their associated path costs.</returns>
		public static IEnumerable<(TVertex vertex, float pathCost)> CalculateChangedPathCosts<TVertex, TEdge>(
			IEnumerable<TVertex> changedVertices,
			CurrentPathCostGetter<TVertex> currentPathCost,
			GetEdges<TVertex, TEdge> edges,
			EdgeFromGetter<TVertex, TEdge> fromVertex,
			EdgeToGetter<TVertex, TEdge> toVertex,
			EdgeIsDirectionalGetter<TEdge> isDirectional,
			EdgeCostGetter<TEdge> cost) {

			Dictionary<TVertex, float> pathCosts = new Dictionary<TVertex, float>();
			var vertices = changedVertices.ToList();
			List<TVertex> sourceVertices = new List<TVertex>();

			IEnumerable<TEdge> incomingEdges(TVertex v) => edges(v).Where(e => !isDirectional(e) || toVertex(e).Equals(v));
			IEnumerable<TEdge> outgoingEdges(TVertex v) => edges(v).Where(e => !isDirectional(e) || fromVertex(e).Equals(v));


			// Invalidate all outgoing connections recursively until the outgoing connection reaches a validating pathcost.
			Queue<TVertex> queueForCostValidation = new Queue<TVertex>();
			HashSet<TVertex> validatingVertices = new HashSet<TVertex>();
			foreach(var vertex in vertices) {
				queueForCostValidation.Enqueue(vertex);
			}

			while(queueForCostValidation.Count > 0) {
				var vertex = queueForCostValidation.Dequeue();
				// Check if this vertex has already been invalidated.
				if(!(pathCosts.ContainsKey(vertex) && pathCosts[vertex] == float.PositiveInfinity)) {
					// Get all incoming connections that validate the pathing cost of this vertex.
					var validatingConnections = incomingEdges(vertex)
						.Select(e => (e, v: fromVertex(e)))
						.Where(ev => pathCosts.GetValueOrDefault(ev.v, currentPathCost(ev.v)) + cost(ev.e) == currentPathCost(vertex))
						.ToArray();

					// Does any incoming connection validate the pathing cost of this vertex?
					if(validatingConnections.Length > 0) {
						// Then start recalculation from here.
						if(validatingVertices.Add(vertex)) {
							pathCosts[vertex] = currentPathCost(vertex);
						}
					} else {
						// If not, we still need to find a vertex to restart our path from.
						// So invalidate any current path cost this vertex might have, we have seen that it is not corrext anyway.
						pathCosts[vertex] = float.PositiveInfinity;
						validatingVertices.Remove(vertex);
						foreach(var edge in incomingEdges(vertex)) {
							queueForCostValidation.Enqueue(fromVertex(edge));
						}

						foreach(var edge in outgoingEdges(vertex)) {
							queueForCostValidation.Enqueue(toVertex(edge));
						}
					}
				}
			}

			Queue<TVertex> queue = new Queue<TVertex>(validatingVertices);
			return calculateCostsCore(currentPathCost, edges, fromVertex, toVertex, isDirectional, cost, pathCosts, queue);
		}

		private static IEnumerable<(TVertex vertex, float pathCost)> calculateCostsCore<TVertex, TEdge>(
			CurrentPathCostGetter<TVertex> currentPathCost,
			GetEdges<TVertex, TEdge> edges,
			EdgeFromGetter<TVertex, TEdge> fromVertex,
			EdgeToGetter<TVertex, TEdge> toVertex,
			EdgeIsDirectionalGetter<TEdge> isDirectional,
			EdgeCostGetter<TEdge> cost,
			Dictionary<TVertex, float> pathCosts,
			Queue<TVertex> queue) {

			Func<TVertex, float, bool> isPathShorter = null;

			if(currentPathCost == null) {
				// Normal cost calculation. No existing costs will be assumed.
				isPathShorter = (vertex, cost) =>
					cost < pathCosts.GetValueOrDefault(vertex, float.PositiveInfinity);
			} else {
				// There are existing costs to keep in mind.
				isPathShorter = (vertex, cost) =>
					cost < pathCosts.GetValueOrDefault(vertex, currentPathCost(vertex));
			}


			while(queue.Count > 0) {
				TVertex vertex = queue.Dequeue();
				float pathCost = pathCosts[vertex];

				foreach(TEdge edge in edges(vertex)) {
					if(!isDirectional(edge) || fromVertex(edge).Equals(vertex)) {
						TVertex neighbour = toVertex(edge);
						float neighbourPathCost = pathCost + cost(edge);
						if(isPathShorter(neighbour, neighbourPathCost)) {
							queue.Enqueue(neighbour);
							pathCosts[neighbour] = neighbourPathCost;
						}
					}
				}
			}

			return pathCosts.Select(keyValuePair => (keyValuePair.Key, keyValuePair.Value));
		}
	}
}
