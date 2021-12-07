using Assets.Scripts.Models.Levels;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

public static class LevelManager {
	public const string LevelsDirectoryName = "Levels";
	public const string LevelMapFileName = "map.png";
	public const string LevelDataFileName = "data.json";
	public static List<LevelBase> GetLevels() {
		List<LevelBase> levels = new List<LevelBase>();
		var dataFilePaths = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, LevelsDirectoryName), "*data.json", SearchOption.AllDirectories);
		foreach(var dataFilePath in dataFilePaths) {
			string json = File.ReadAllText(dataFilePath);
			LevelBase level = JsonUtility.FromJson<LevelBase>(json);
			level.DirectoryPath = Path.GetDirectoryName(dataFilePath);
			levels.Add(level);
		}

		return levels;
	}

	public static Cell[,] LoadLevelMap(Level level) {
		string levelMapPath = Path.Combine(level.DirectoryPath, LevelMapFileName);

		Texture2D texture = new Texture2D(1, 1);
		texture.LoadImage(File.ReadAllBytes(levelMapPath));
		int width = texture.width;
		int height = texture.height;

		Cell[,] cells = new Cell[width + 2, height + 2];

		// Generate border.
		for(int x = 0; x < width + 2; x++) {
			cells[x, 0] =           new Cell { CellType = CellType.Border, Coordinates = new Vector2Int(x, 0), };
			cells[x, height + 1] =  new Cell { CellType = CellType.Border, Coordinates = new Vector2Int(x, height + 1), };
		}
		for(int y = 1; y < height + 1; y++) {
			cells[0, y] =           new Cell { CellType = CellType.Border, Coordinates = new Vector2Int(0, y), };
			cells[width + 1, y] =   new Cell { CellType = CellType.Border, Coordinates = new Vector2Int(width + 1, y), };
		}

		// Define the chances of a city block being of a certain type.
		var cityBlockTypeChances = new List<(float chance, CityBlockType cityBlockType)> {
				(8f, CityBlockType.Housing),
				(3f, CityBlockType.Commercial),
				(4f, CityBlockType.Industry),
			};

		// Instantiate all cells of the map.
		for(int x = 0; x < width; x += 1) {
			for(int y = 0; y < height; y += 1) {
				Cell cell = null;
				Color pixelColor = texture.GetPixel(x, y);

				if(pixelColor.a == 0) {
					cell = new Cell {
						CellType = CellType.Empty,
					};
				} else if(pixelColor.r == pixelColor.g && pixelColor.r == pixelColor.b) {
					float kmph = 30f + pixelColor.r * 100f;
					float unitDistancePerSecond = kmph / 3600;
					cell = new PathCell {
						CellType = CellType.Road,
						MaxSpeed = unitDistancePerSecond,
					};
				} else if(pixelColor == Color.red) {
					CityBlockType cityBlockType = Randomization.GetRandom(cityBlockTypeChances);
					float kmph = 20f;
					float unitDistancePerSecond = kmph / 3600;

					cell = new CityBlockCell {
						CellType        = CellType.City,
						MaxSpeed        = unitDistancePerSecond,
						CityBlockType   = cityBlockType,
					};
				}

				// Correct for border with +1 in each coordinate.
				cell.Coordinates = new Vector2Int(x + 1, y + 1);
				cells[x + 1, y + 1] = cell;
			}
		}

		// Resolve neighbours.
		int gridWidth = cells.GetLength(0);
		int gridHeight = cells.GetLength(1);
		for(int x = 0; x < gridWidth; x += 1) {
			for(int y = 0; y < gridHeight; y += 1) {
				Cell cell = cells[x, y];

				cell.Neighbours[0] = y != gridHeight - 1 ? cells[x, y + 1] : null;
				cell.Neighbours[1] = x != gridWidth - 1 ? cells[x + 1, y] : null;
				cell.Neighbours[2] = y != 0 ? cells[x, y - 1] : null;
				cell.Neighbours[3] = x != 0 ? cells[x - 1, y] : null;
			}
		}

		return cells;
	}

	public static List<City> ResolveCities(Level level) {
		List<City> cities = new List<City>();

		// Go through all cells except the borders.
		for(int x = 1; x < level.Width -1; x += 1) {
			for(int y = 1; y < level.Height -1; y += 1) {
				Cell cell = level.Cells[x, y];

				// Check if the cell is a city cell and not yet claimed for a city.
				if(cell is CityBlockCell cityBlockCell && cityBlockCell.OfCity == null) {
					// Generate a city with a unique name.
					City city = new City();
					city.Name = level.AvailableCityNames.GetAndRemoveRandom();

					// Cost function for finding connected city cells.
					static float getCost(Cell from, Cell to) {
						return to.CellType switch {
							CellType.City => 0f,
							CellType.Road => 1f,
							_ => float.PositiveInfinity,
						};
					}

					// Calculate the cost to all connected cells.
					var costs = CostCalculator.CalculatePathCosts(cell, c => c.Neighbours, getCost)
							// Only retain city cells that are close enough.
							.Where(x => x.pathCost < 10f && x.vertex.CellType == CellType.City);

					// Add each remaining cell to the generated city.
					foreach(var cellCost in costs) {
						var connectedCityCell = (CityBlockCell)cellCost.vertex;

						// But only if it is not already claimed.
						if(connectedCityCell.OfCity == null) {
							connectedCityCell.OfCity = city;
							city.Cells.Add(connectedCityCell);
						}
					}

					// Add the generated city to the results.
					cities.Add(city);
				}
			}
		}

		return cities;
	}

	public static void GeneratePathingGraph(IEnumerable<Cell> cells) {
		var pathCells = cells.Select(cell => cell as PathCell).Where(cell => cell != null);
		Debug.Log($"Path cells: {pathCells.Count()}");
		Debug.Log($"Expected vertices: {pathCells.Count() * 4}");
		foreach(var cell in pathCells) {
			// Keep in mind that the cost should rise as the maximum speed lowers.
			// This may be counterintuitive, but since the distances stay the same, the costs are only varied through the allowed speeds.
			// The result should be the estimated time of traversing from one vertex to the next in hours / 2.

			// Internal neighbours (edges add themselves to the relevant vertices).
			new Edge(cell.VertexNE, cell.VertexNW, (1 - PathCell.SidePadding * 2) / cell.MaxSpeed, true);
			new Edge(cell.VertexNW, cell.VertexSW, (1 - PathCell.SidePadding * 2) / cell.MaxSpeed, true);
			new Edge(cell.VertexSW, cell.VertexSE, (1 - PathCell.SidePadding * 2) / cell.MaxSpeed, true);
			new Edge(cell.VertexSE, cell.VertexNE, (1 - PathCell.SidePadding * 2) / cell.MaxSpeed, true);

			// External neighbours (edges add themselves to the relevant vertices).
			// Doubles are not present since we only consider the outgoing connections for each vertex.
			if(cell.North is PathCell northPathCell) {
				new Edge(cell.VertexNE, northPathCell.VertexSE, PathCell.SidePadding * 4 / (cell.MaxSpeed + northPathCell.MaxSpeed), true);
			}

			if(cell.East is PathCell eastPathCell) {
				new Edge(cell.VertexSE, eastPathCell.VertexSW, PathCell.SidePadding * 4 / (cell.MaxSpeed + eastPathCell.MaxSpeed), true);
			}

			if(cell.South is PathCell southPathCell) {
				new Edge(cell.VertexSW, southPathCell.VertexNW, PathCell.SidePadding * 4 / (cell.MaxSpeed + southPathCell.MaxSpeed), true);
			}

			if(cell.West is PathCell westPathCell) {
				new Edge(cell.VertexNW, westPathCell.VertexNE, PathCell.SidePadding * 4 / (cell.MaxSpeed + westPathCell.MaxSpeed), true);
			}
		}
	}


	public static void InitializePathingCosts(Level level) {
		System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
		stopwatch.Start();


		foreach(var city in level.Cities) {
			var vertices = city.Cells.SelectMany(cell=>cell.Vertices);

			// We calculate the reverse route, so for each vertex we look at the incoming connections.
			// The "from-to" terminology may get confusing here because of searching in the reverse direction.
			var costs = CostCalculator.CalculateReversePathCosts(
					vertices.Select(v=>(v,0f)),
					v => v.Connections,
					e => e.from,
					e => e.to,
					e => e.Directional,
					e => e.cost);

			// Apply the path cost to this city for each reachable vertex.
			foreach(var vertexPathCost in costs) {
				vertexPathCost.vertex.PathCosts[city] = vertexPathCost.pathCost;
			}
		}

		stopwatch.Stop();
		UnityEngine.Debug.Log($"Calculating paths took {stopwatch.ElapsedMilliseconds}ms");
	}

	public static void RecalculatePathingCosts(Level level, IEnumerable<PathCell> changedCells) {
		System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
		stopwatch.Start();
		IEnumerable<Vertex> changedVertices = changedCells.SelectMany(cell => cell.Vertices);

		foreach(var city in level.Cities) {

			// We calculate the reverse route, so for each vertex we look at the incoming connections.
			// The "from-to" terminology may get confusing here because of searching in the reverse direction.
			var costs = CostCalculator.CalculateChangedPathCosts(
					changedVertices,
					(v) => v.PathCosts[city],
					(v) => v.Connections,
					e => e.to,
					e => e.from,
					e => e.Directional,
					e => (e.to.ContainingCell as CityBlockCell)?.OfCity == city ? 0f : e.cost);

			// Apply the path cost to this city for each reachable vertex.
			foreach(var vertexPathCost in costs) {
				vertexPathCost.vertex.PathCosts[city] = vertexPathCost.pathCost;
			}
		}

		stopwatch.Stop();
		Debug.Log($"Recalculating paths took {stopwatch.ElapsedMilliseconds}ms");
	}
}
