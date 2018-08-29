using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Column
{
	World world;

	int chunkSize = World.chunkSize;
	public Vector3 position;
	public Chunk.Status spawnStatus;
	public bool sizeCalculated = false;

	public int[,] heightMap;
	public FastNoise.EdgeData[,] edgeMap;
	public TerrainLibrary.BiomeLayer[,] biomeLayers;

	public StructureLibrary.Tiles[,] structureMap;
	public bool hasStructures = false;

	public int highestPoint = 0;
	public int topChunkGenerate;
	public int topChunkDraw;	

	public int lowestPoint = 1000;
	public int bottomChunkGenerate;
	public int bottomChunkDraw;
	
	public Column(Vector3 position, TerrainGenerator terrain, World world)
	{
		biomeLayers = new TerrainLibrary.BiomeLayer[chunkSize,chunkSize];

		this.position = position;
		this.world = world;	

		terrain.GetTopologyData(this);
	}

	public static Column Get(Vector3 position)
	{
		Column column = World.columns[new Vector3(position.x, 0, position.z)];
		return column;
	}

	public void CheckLowest(int value)
	{
		if(value < lowestPoint)
		{
			lowestPoint = value;
		}
	}
	public void CheckHighest(int value)
	{
		if(value > highestPoint)
		{
			highestPoint = value;
		}
	}
}
