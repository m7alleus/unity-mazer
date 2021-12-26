using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MapGenerator : MonoBehaviour {
	public int width;
	public int height;
	public int smoothMapFactor = 5;
	public int scaleMapFactor = 1;
	public int mapBorderSize = 1; // the border extends outside the map width & height

	public string seed;
	public bool useRandomSeed;

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

	void RandomFillMap ()
	{
		if (useRandomSeed) {
			DateTimeOffset now = (DateTimeOffset)DateTime.UtcNow;
			seed = now.ToUnixTimeSeconds ().ToString ();
		}

		Debug.Log ("seed: " + seed);
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

				if (neighbourX >= 0 && neighbourX < width && neighbourY >= 0 && neighbourY < height) {
					// check if we are inside of the map
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
