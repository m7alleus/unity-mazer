using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MapGenerator : MonoBehaviour {
	public int width = 130;
	public int height = 80;
	public int smoothMapFactor = 5;
	public int scaleMapFactor = 2;
	public int mapBorderSize = 2; // the border extends outside the map width & height

	public string seed;
	public bool useRandomSeed = true;

	[Range (0, 100)]
	public int randomFillPercent;

	int [,] map;

	void Start ()
	{
		GenerateMap ();
	}

	void Update ()
	{
		if (Input.GetMouseButton (0)) {
			GenerateMap ();
		}
	}

	void GenerateMap ()
	{
		map = new int [width, height];
		RandomFillMap ();

		for (int i = 0; i < smoothMapFactor; i++) {
			SmoothMap ();
		}

		ProcessMap (); // removes regions that are too small

		// define a map with a wall border
		int [,] borderedMap = new int [width + mapBorderSize * 2, height + mapBorderSize * 2];

		for (int x = 0; x < borderedMap.GetLength (0); x++) {
			for (int y = 0; y < borderedMap.GetLength (1); y++) {
				if (x >= mapBorderSize && x < width + mapBorderSize && y >= mapBorderSize && y < height + mapBorderSize) {
					// if we are within the map and within the border size
					borderedMap [x, y] = map [x - mapBorderSize, y - mapBorderSize];
				} else {
					// it's a border
					borderedMap [x, y] = 1;
				}
			}
		}

		MeshGenerator meshGen = GetComponent<MeshGenerator> ();
		meshGen.GenerateMesh (borderedMap, scaleMapFactor);
	}

	void ProcessMap ()
	{
		List<List<Coord>> wallRegions = GetRegions (1);
		// Debug.Log (wallRegions.Count + " wall regions found");

		// this value determines the min number of tiles a wall region has to possess to be kept
		// otherwise it's replaced by a room region
		int wallThresholdSize = 50; // magic number, move to class level variable?
		foreach(List<Coord> wallRegion in wallRegions) {
			if(wallRegion.Count < wallThresholdSize) {
				foreach(Coord tile in wallRegion) {
					map [tile.tileX, tile.tileY] = 0;
                }
            }
        }

		List<List<Coord>> roomRegions = GetRegions (0);
		// Debug.Log (roomRegions.Count + " room regions found");

		// this value determines the min number of tiles a roomregion has to possess to be kept
		// otherwise it's replaced by a wall region
		int roomThresholdSize = 50; // magic number, move to class level variable?
		List<Room> survivingRooms = new List<Room> ();

		foreach (List<Coord> roomRegion in roomRegions) {
			if (roomRegion.Count < roomThresholdSize) {
				foreach (Coord tile in roomRegion) {
					map [tile.tileX, tile.tileY] = 1;
				}
			} else {
				survivingRooms.Add (new Room (roomRegion, map));
			}
		}

		survivingRooms.Sort ();
		survivingRooms [0].isMainRoom = true;
		survivingRooms [0].isAccessibleFromMainRoom= true;

		ConnectClosestRooms (survivingRooms);
	}

	void ConnectClosestRooms (List<Room> allRooms, bool forceAccessibilityFromMainRoom = false)
	{
		List<Room> roomListA = new List<Room>();
		List<Room> roomListB = new List<Room> ();

        if (forceAccessibilityFromMainRoom) {
			foreach(Room room in allRooms) {
				if (room.isAccessibleFromMainRoom) {
					roomListB.Add (room);
                } else {
					roomListA.Add (room);
				}
            }
        } else {
			roomListA = allRooms;
			roomListB = allRooms;
		}

		int bestDistance = 0;
		Coord bestTileA = new Coord ();
		Coord bestTileB = new Coord ();
		Room bestRoomA = new Room ();
		Room bestRoomB = new Room ();
		bool possibleConnectionFound = false;

		foreach (Room roomA in roomListA) {
            if (!forceAccessibilityFromMainRoom) {
				possibleConnectionFound = false;

				if(roomA.connectedRooms.Count > 0) {
					continue;
                }
            }

			foreach (Room roomB in roomListB) {
				if (roomA == roomB || roomA.IsConnected(roomB))
					continue;

				for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++) {
					for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++) {
						Coord tileA = roomA.edgeTiles [tileIndexA];
						Coord tileB = roomB.edgeTiles [tileIndexB];
						int distanceBetweenRooms = (int)(Mathf.Pow (tileA.tileX - tileB.tileX, 2) + Mathf.Pow (tileA.tileY - tileB.tileY, 2));

						if (distanceBetweenRooms < bestDistance || !possibleConnectionFound) {
							bestDistance = distanceBetweenRooms;
							possibleConnectionFound = true;
							bestTileA = tileA;
							bestTileB = tileB;
							bestRoomA = roomA;
							bestRoomB = roomB;
						}
					}
				}
			}

			if (possibleConnectionFound && !forceAccessibilityFromMainRoom) 
				CreatePassage (bestRoomA, bestRoomB, bestTileA, bestTileB);
		}

		if (possibleConnectionFound && forceAccessibilityFromMainRoom) {
			CreatePassage (bestRoomA, bestRoomB, bestTileA, bestTileB);
			ConnectClosestRooms (allRooms, true);
		}
			

		if (!forceAccessibilityFromMainRoom)
			ConnectClosestRooms (allRooms, true);
        
	}

	void CreatePassage (Room roomA, Room roomB, Coord tileA, Coord tileB)
	{
		Room.ConnectRooms (roomA, roomB);
		//Debug.DrawLine (CoordToWorldPoint (tileA), CoordToWorldPoint (tileB), Color.green, 5);

		List<Coord> line = GetLine (tileA, tileB);
		foreach(Coord c in line) {
			DrawCircle (c, 1);
        } 
	}

    void DrawCircle (Coord c, int radius)
    {
		for (int x = -radius; x <= radius; x++) {
			for (int y = -radius; y <= radius; y++) {
				if (x * x + y * y <= radius * radius) {
					int drawX = c.tileX + x;
					int drawY = c.tileY + y;

					if (IsInMapRange (drawX, drawY)) {
						map [drawX, drawY] = 0;
					}
				}
			}
		}
    }

	List<Coord> GetLine(Coord from, Coord to)
    {
		List<Coord> line = new List<Coord> ();
		int x = from.tileX;
		int y = from.tileY;

		int dx = to.tileX - from.tileX;
		int dy = to.tileY - from.tileY;

		bool inverted = false;
		int step = Math.Sign (dx);
		int gradientStep = Math.Sign (dy);

		int longest = Mathf.Abs (dx);
		int shortest = Mathf.Abs (dy);

		if (longest < shortest) {
			inverted = true;
			longest = Mathf.Abs (dy);
			shortest = Mathf.Abs (dx);

			step = Math.Sign (dy);
			gradientStep = Math.Sign (dx);
		}

		int gradientAccumulation = longest / 2;
		for(int i = 0 ; i < longest ; i++) {
			line.Add (new Coord (x, y));
			if (inverted) {
				y += step;
            } else {
				x += step;
            }

			gradientAccumulation += shortest;
			if(gradientAccumulation >= longest) {
                if (inverted) {
					x += gradientStep;
                } else {
					y += gradientStep;
                }
				gradientAccumulation -= longest;
            }
        }

		return line;
	}

	Vector3 CoordToWorldPoint (Coord tile)
	{
		return new Vector3 ((-width / 2 + .5f + tile.tileX) * scaleMapFactor, 2, (-height / 2 + .5f + tile.tileY) * scaleMapFactor);
	}

	List<List<Coord>> GetRegions (int tileType)
	{
		List<List<Coord>> regions = new List<List<Coord>> ();
		int [,] mapFlags = new int [width, height];

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				if (mapFlags [x, y] == 0 && map [x, y] == tileType) {
					List<Coord> newRegion = GetRegionTiles (x, y);
					regions.Add (newRegion);

					foreach(Coord tile in newRegion) {
						mapFlags [tile.tileX, tile.tileY] = 1;
                    }
				}
			}
		}

		return regions;
	}

	List<Coord> GetRegionTiles (int startX, int startY)
	{
		List<Coord> tiles = new List<Coord> ();
		int [,] mapFlags = new int [width, height];
		int tileType = map[startX, startY];

		Queue<Coord> queue = new Queue<Coord> ();
		queue.Enqueue (new Coord (startX, startY));
		mapFlags [startX, startY] = 1;

		while(queue.Count > 0) {
			Coord tile = queue.Dequeue ();
			tiles.Add (tile);

			for (int x = tile.tileX - 1; x <= tile.tileX + 1 ; x++) {
				for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
					// the second condition checks that we are not looking at a tile in diagonal
					if (IsInMapRange (x, y) && (x == tile.tileX || y == tile.tileY)) {
						if (mapFlags [x, y] == 0 && map [x, y] == tileType) {
							mapFlags [x, y] = 1;
							queue.Enqueue (new Coord (x, y));
                        }
                    }
				}
			}
        }

		return tiles;
	}

	bool IsInMapRange(int x, int y)
	{
		return x >= 0 && x < width && y >= 0 && y < height;
	}

	void RandomFillMap ()
	{
		if (useRandomSeed) {
			DateTimeOffset now = (DateTimeOffset)DateTime.UtcNow;
			seed = now.ToUnixTimeSeconds ().ToString ();
		}

		Debug.Log ("SEED: " + seed);
		System.Random pseudoRandom = new System.Random (seed.GetHashCode ());

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				if (x == 0 || x == (width - 1) || y == 0 || y == height - 1) {
					map [x, y] = 1;
				} else {
					map [x, y] = (pseudoRandom.Next (0, 100) < randomFillPercent) ? 1 : 0;
				}
			}
		}
	}

	void SmoothMap ()
	{
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				int neighbourWallTiles = GetSurroundingWallCount (x, y);

				if (neighbourWallTiles > 4) {
					map [x, y] = 1;
				} else if (neighbourWallTiles < 4) {
					map [x, y] = 0;
				}
			}
		}
	}

	int GetSurroundingWallCount (int gridX, int gridY)
	{
		int wallCount = 0;
		for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++) {
			for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++) {

				if (IsInMapRange(neighbourX, neighbourY)) { 
					if (neighbourX != gridX || neighbourY != gridY) {
						// check if we are not on the current cell
						wallCount += map [neighbourX, neighbourY];
					}
				} else {
					wallCount++;
				}
			}
		}
		return wallCount;
	}

	struct Coord {
		public int tileX;
		public int tileY;

		public Coord(int x, int y)
        {
			tileX = x;
			tileY = y;
		}
	}

	class Room : IComparable<Room> {
		public List<Coord> tiles;
		public List<Coord> edgeTiles;
		public List<Room> connectedRooms;
		public int roomSize;
		public bool isAccessibleFromMainRoom;
		public bool isMainRoom;

		public Room ()
		{
		}

		public Room (List<Coord> roomTiles, int [,] map)
		{
			tiles = roomTiles;
			roomSize = tiles.Count;
			connectedRooms = new List<Room> ();

			edgeTiles = new List<Coord> ();
			foreach (Coord tile in tiles) {
				for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++) {
					for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
						if (x == tile.tileX || y == tile.tileY) {
							if (map [x, y] == 1) {
								edgeTiles.Add (tile);
							}
						}
					}
				}
			}
		}

		public void SetAccessibleFromMainRoom ()
        {
			if(!isAccessibleFromMainRoom) {
				isAccessibleFromMainRoom = true;
				foreach(Room connectedRoom in connectedRooms) {
					connectedRoom.SetAccessibleFromMainRoom ();
                }
            }
        }

		public static void ConnectRooms (Room roomA, Room roomB)
		{
            if (roomA.isAccessibleFromMainRoom) {
				roomB.SetAccessibleFromMainRoom ();
            } else if(roomB.isAccessibleFromMainRoom) {
				roomA.SetAccessibleFromMainRoom ();
			}
			roomA.connectedRooms.Add (roomB);
			roomB.connectedRooms.Add (roomA);
		}

		public bool IsConnected (Room otherRoom)
		{
			return connectedRooms.Contains (otherRoom);
		}

		public int CompareTo(Room otherRoom)
        {
			return otherRoom.roomSize.CompareTo (roomSize);
        }
	}

	// disabled for now
	//void OnDrawGizmos ()
	//{
	//	return;

	//	if (map == null) {
	//		return;
	//	}

	//	for (int x = 0; x < width; x++) {
	//		for (int y = 0; y < height; y++) {
	//			Gizmos.color = (map [x, y] == 1) ? Color.black : Color.white;
	//			//Vector3 pos = new Vector3 (-width/2 + x, 0, -height/2 + y);
	//			Vector3 pos = new Vector3 (-width / 2 + x + .5f, 0, -height / 2 + y + .5f);
	//			Gizmos.DrawCube (pos, Vector3.one);
	//		}
	//	}
	//}
}
