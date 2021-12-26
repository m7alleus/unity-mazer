using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MapGenerator : MonoBehaviour {
	public int width = 100;
	public int height = 70;
	public int smoothMapFactor = 5;
	public int scaleMapFactor = 10;
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
		Debug.Log (wallRegions.Count + " wall regions found");

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
		Debug.Log (roomRegions.Count + " room regions found");

		// this value determines the min number of tiles a roomregion has to possess to be kept
		// otherwise it's replaced by a wall region
		int roomThresholdSize = 50; // magic number, move to class level variable?
		foreach (List<Coord> roomRegion in roomRegions) {
			if (roomRegion.Count < roomThresholdSize) {
				foreach (Coord tile in roomRegion) {
					map [tile.tileX, tile.tileY] = 1;
				}
			}
		}
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

	void OnDrawGizmos ()
	{
		// disabled for now
		return;

		if (map == null) {
			return;
		}

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				Gizmos.color = (map [x, y] == 1) ? Color.black : Color.white;
				//Vector3 pos = new Vector3 (-width/2 + x, 0, -height/2 + y);
				Vector3 pos = new Vector3 (-width / 2 + x + .5f, 0, -height / 2 + y + .5f);
				Gizmos.DrawCube (pos, Vector3.one);
			}
		}
	}
}
