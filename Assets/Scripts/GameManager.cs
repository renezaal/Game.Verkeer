using Assets.Scripts.Extensions;
using Assets.Scripts.Models;
using Assets.Scripts.Models.Levels;

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

public class GameManager : MonoBehaviour {
	public GameObject RoadCross;
	public GameObject RoadStraight;
	public GameObject RoadTJunction;
	public GameObject RoadCorner;
	public GameObject RoadEnd;
	public GameObject Grass;
	public GameObject Water;
	public GameObject[] Houses;
	public GameObject Tree;
	public Car[] Cars;

	public Transform TileContainer;
	public Transform ActorContainer;

	public float CarSpawnIntervalSeconds = 2f;
	public float AlternativeRouteFindingAggressiveness = 1f;
	public float BaseCarSpeed = 50f;
	public Range CarSpeedVariance = new(){ Minimum = .9f, Maximum = 1.2f };

	private List<LevelBase> levels;
	private Level level;
	private float lastCarSpawnTime = 0;
	private Camera mainCamera;
	private List<(float spawnChance, Car carPrefab)> carSpawnChances;

	// Start is called before the first frame update
	public void Start() {
		this.mainCamera = Camera.main;
		this.carSpawnChances = this.Cars.Select(car => (car.SpawnChance, car)).ToList();

		// Get the levels available.
		this.levels = LevelManager.GetLevels();

		// Load the first level.
		this.LoadLevel(levels.First());
		this.RegenerateTiles();
	}

	public Cell GetCell(Vector3 coordinate) {
		int x = (int)coordinate.x;
		int y = (int)coordinate.y;
		if(
			x >= 0
			&& x < level.Cells.GetLength(0)
			&& y >= 0
			&& y < level.Cells.GetLength(1)) {
			return level.Cells[x, y];
		}
		return null;
	}

	private void LoadLevel(LevelBase levelToLoad) {
		var newLevel = Level.FromBase(levelToLoad);
		newLevel.Cells = LevelManager.LoadLevelMap(newLevel);
		newLevel.Cities = LevelManager.ResolveCities(newLevel);
		LevelManager.GeneratePathingGraph(newLevel.Cells.AsEnumerable());

		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		LevelManager.InitializePathingCosts(newLevel);
		stopwatch.Stop();
		UnityEngine.Debug.Log($"Calculating paths took {stopwatch.ElapsedMilliseconds}ms");
		this.level = newLevel;
	}

	private void RegenerateTile(Cell cell) {
		GameObject prefab = null;
		float rotateDegrees = 0;

		switch(cell.CellType) {
			case CellType.City:
				prefab = this.Houses[Random.Range(0, this.Houses.Length)];
				break;
			case CellType.Road:
				bool north = cell.North is PathCell;
				bool east = cell.East  is PathCell;
				bool south = cell.South is PathCell;
				bool west = cell.West  is PathCell;

				if(!north &&  east &&  south &&  west) {
					prefab = this.RoadTJunction;
					rotateDegrees = 90;
				} else if(north && !east &&  south &&  west) {
					prefab = this.RoadTJunction;
					rotateDegrees = 180;
				} else if(!north && !east &&  south &&  west) {
					prefab = this.RoadCorner;
					rotateDegrees = 90;
				} else if(north &&  east && !south &&  west) {
					prefab = this.RoadTJunction;
					rotateDegrees = 270;
				} else if(!north &&  east && !south &&  west) {
					prefab = this.RoadStraight;
					rotateDegrees = 0;
				} else if(north && !east && !south &&  west) {
					prefab = this.RoadCorner;
					rotateDegrees = 180;
				} else if(north &&  east &&  south && !west) {
					prefab = this.RoadTJunction;
					rotateDegrees = 0;
				} else if(!north &&  east &&  south && !west) {
					prefab = this.RoadCorner;
					rotateDegrees = 0;
				} else if(north && !east &&  south && !west) {
					prefab = this.RoadStraight;
					rotateDegrees = 90;
				} else if(north &&  east && !south && !west) {
					prefab = this.RoadCorner;
					rotateDegrees = 270;
				} else if(north &&  east &&  south &&  west) {
					prefab = this.RoadCross;
					rotateDegrees = 0;
				} else if(north && !east && !south && !west) {
					prefab = this.RoadEnd;
					rotateDegrees = 270;
				} else if(!north &&  east && !south && !west) {
					prefab = this.RoadEnd;
					rotateDegrees = 0;
				} else if(!north && !east &&  south && !west) {
					prefab = this.RoadEnd;
					rotateDegrees = 90;
				} else if(!north && !east && !south &&  west) {
					prefab = this.RoadEnd;
					rotateDegrees = 180;
				} else if(!north && !east && !south && !west) {
					prefab = this.RoadCross;
					rotateDegrees = 0;
				}

				break;
			default:
				prefab = this.Grass;
				break;
		}


		if(cell.Tile != null) {
			Destroy(cell.Tile);
		}
		cell.Tile = Instantiate(prefab, cell.Coordinates + new Vector2(.5f, .5f), Quaternion.AngleAxis(rotateDegrees, Vector3.back), TileContainer);
	}

	private void RegenerateTiles() {
		for(int x = 0; x < level.Width; x++) {
			for(int y = 0; y < level.Height; y++) {
				Cell cell = level.Cells[x, y];
				RegenerateTile(cell);
			}
		}
	}

	private Task pathCalculationTask = Task.CompletedTask;
	private ulong ticks = 0;

	public void FixedUpdate() {
		ticks++;
		if((this.lastCarSpawnTime + this.CarSpawnIntervalSeconds) < Time.fixedTime) {
			// Get all cells belonging to cities.
			var cityCells = this.level.Cities
				.SelectMany(city => city.Cells)
				.ToList();
			City targetCity = cityCells[Random.Range(0, cityCells.Count)].OfCity;
			var spawnVertices = cityCells
				.Where(cell => cell.OfCity != targetCity)
				.SelectMany(cell => cell.Vertices)
				.Where(vertex => vertex.Occupant == null)
				.ToList();

			if(spawnVertices.Count > 0) {
				Vertex spawn = spawnVertices[Random.Range(0, spawnVertices.Count)];
				Car prefab = Randomization.GetRandom(this.carSpawnChances);
				var instance = Instantiate(prefab, spawn.Position, Quaternion.identity, this.ActorContainer);
				instance.SpeedModifier *= this.CarSpeedVariance.GetRandomInRange() * this.BaseCarSpeed;

				instance.TargetCity = targetCity;
				instance.CurrentVertex = spawn;
				instance.WaitCostScale *= this.AlternativeRouteFindingAggressiveness;
				instance.GameManager = this;
				spawn.Occupant = instance;

				this.lastCarSpawnTime = Time.fixedTime;
			}
		}

		if(pathCalculationTask.IsCompleted) {
			if(cellsMarkedForPathCostUpdate.Count == 0) {
				List<PathCell> cellsToDelete = new List<PathCell>(cellsMarkedForDelete);
				foreach(var cell in cellsToDelete) {
					// If the cell has no occupants, proceed to delete.
					if(!cell.Vertices.Any(v => v.Occupant != null)) {
						// Clean up vertices.
						if(cell is PathCell pathCell) {
							foreach(var vertex in pathCell.Vertices) {
								while(vertex.Connections.Count > 0) {
									vertex.Connections[0].Disconnect();
								}
							}
						}

						// Clean up city references.
						if(cell is CityBlockCell cityBlock) {
							cityBlock.OfCity.Cells.Remove(cityBlock);
						}

						// Create a replacement.
						Cell replacementCell = new Cell {
							CellType = CellType.Empty,
							Coordinates = cell.Coordinates,
							Neighbours = cell.Neighbours,
							Tile = cell.Tile,
						};

						// Reallocate the neighbour references.
						foreach(var neighbour in cell.Neighbours) {
							for(int i = 0; i < neighbour.Neighbours.Length; i++) {
								if(neighbour.Neighbours[i] == cell) {
									neighbour.Neighbours[i] = replacementCell;
								}
							}
						}

						// Apply the replacement.
						level.Cells[cell.X, cell.Y] = replacementCell;
						RegenerateTile(replacementCell);
						foreach(var neighbour in replacementCell.Neighbours) {
							RegenerateTile(neighbour);
						}
						cellsMarkedForDelete.Remove(cell);
					}
				}
			} else {
				HashSet<PathCell> cells = new HashSet<PathCell>(cellsMarkedForPathCostUpdate);
				cellsMarkedForPathCostUpdate.Clear();
				//pathCalculationTask = Task.Run(() => LevelManager.RecalculatePathingCosts(this.level, cells));
				pathCalculationTask = Task.Run(() => LevelManager.InitializePathingCosts(this.level));
			}
		}

		/*
		 * Steps for removing a PathCell:
		 * 1. Set the cost for incoming edges to infinity. Internal edges and outgoing edges remain unmodified.
		 * 2. Recalculate pathing costs for all connected vertices not in this cell.
		 * 3. Wait for all of the vertices in this cell to be unoccupied.
		 * 4. Once all vertices are free, remove the edges, the vertices and the cell in that order.
		 *		NOTE: Remember to also remove the edge-references from the vertices outside the cell.
		 */
	}

	readonly HashSet<PathCell> cellsMarkedForDelete = new HashSet<PathCell>();
	readonly HashSet<PathCell> cellsMarkedForPathCostUpdate = new HashSet<PathCell>();
	// Update is called once per frame
	public void Update() {
		if(Input.GetMouseButtonDown(0)) {
			Cell cell = null;
			if(this.mainCamera.orthographic) {
				cell = GetCell(this.mainCamera.ScreenToWorldPoint(Input.mousePosition));
			} else {
				var ray = this.mainCamera.ScreenPointToRay(Input.mousePosition);
				if(Physics.Raycast(ray, out RaycastHit hit)) {
					cell = GetCell(hit.point);
				}
			}
			if(cell is PathCell pathCell) {
				if(cellsMarkedForDelete.Add(pathCell)) {
					cell.Tile.GetComponentInChildren<MeshRenderer>().material.color = Color.red;
					var verticeEdges = pathCell.Vertices.SelectMany(v => v.Connections.Select(e => (v, e)));
					foreach(var ve in verticeEdges) {
						if(ve.v == ve.e.to && ve.e.from.ContainingCell != pathCell) {
							ve.e.cost = float.PositiveInfinity;
						}
					}
					cellsMarkedForPathCostUpdate.Add(pathCell);
				}
			}
		}
	}
}
