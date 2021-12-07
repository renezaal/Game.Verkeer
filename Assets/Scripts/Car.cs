using Assets.Scripts.Levels;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour {
	public City TargetCity;
	public Vertex CurrentVertex;
	public Edge CurrentEdge;
	public float SpeedModifier = 1f;
	public GameManager GameManager;
	public float WaitCostScale = 1;
	public int Id = idCounter++;

	private float waitCost = 0;
	private static int idCounter;

	// Start is called before the first frame update
	public void Start() {
		var meshRenderer = GetComponentInChildren<MeshRenderer>();
		meshRenderer.materials[1].color = Color.HSVToRGB(Random.value, 1, .65f);
	}

	// Update is called once per frame
	public void Update() {
		// Do something if we're close enough to our partial destination.
		if(Vector3.Distance(transform.position, CurrentVertex.Position) < .01f) {

			// If we're at the city we're heading to, despawn.
			if(CurrentVertex.ContainingCell is CityBlockCell cityBlockCell && cityBlockCell.OfCity == TargetCity) {
				CurrentVertex.Occupant = null;
				Destroy(this.gameObject);
				return;
			}

			waitCost+=WaitCostScale;
			var selectedVertex = CurrentVertex;
			float selectedVertexCost = CurrentVertex.PathCosts[TargetCity] + waitCost;
			foreach(var connection in CurrentVertex.Connections) {
				if(!connection.Directional || connection.from == CurrentVertex) {
					if(connection.to.Occupant == null && connection.to.PathCosts[TargetCity] + connection.cost < selectedVertexCost) {
						// The vertex is open and considered faster than the last selected option.
						// So select this as the next vertex.
						selectedVertex = connection.to;
						selectedVertexCost = connection.to.PathCosts[TargetCity] + connection.cost;
					}
				}
			}

			if(selectedVertex != CurrentVertex) {
				waitCost = 0;
				selectedVertex.Occupant = this;
				CurrentVertex.Occupant = null;
				CurrentVertex = selectedVertex;
			}
		}

		// Lerp based on cell maxSpeed.
		this.transform.LookAt(CurrentVertex.Position,Vector3.back);
		this.transform.position = Vector3.MoveTowards(this.transform.position, CurrentVertex.Position, SpeedModifier * GetCurrentCell().MaxSpeed * Time.deltaTime);
	}

	private PathCell GetCurrentCell() {
		return this.GameManager.GetCell(transform.position) as PathCell;
	}
}
