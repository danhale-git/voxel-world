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

	public byte GetBitMask(Vector3 voxel, StructureLibrary.Tiles tile)
	{
		Vector3[] neighbours = Util.HorizontalBlockNeighbours(voxel);
		int value = 1;
		int total = 0;

		Column owner;

		for(int i = 0; i < neighbours.Length; i++)
		{
			Vector3 pos;
			
			if(BlockOwner(neighbours[i], out owner))
			{
				pos = Util.WrapBlockIndex(neighbours[i]);
			}
			else
			{
				if(owner == null) owner = this;
				pos = neighbours[i];
			}		

			int x = (int)pos.x, z = (int)pos.z;
			
			if(owner.structureMap[x,z] != tile)
			{
				total += value;
			}
			value *= 2;
		}
		return (byte)total;
	}

	bool BlockOwner(Vector3 pos, out Column column)
	{
		//	Get block's edge
		int x = 0, z = 0;

		if		(pos.x < 0) 				x = -1;
		else if (pos.x > World.chunkSize-1) 	x = 1;

		if		(pos.z < 0) 				z = -1;
		else if (pos.z > World.chunkSize-1) 	z = 1;

		//	Voxel is in this chunk
		if(x == 0 && z == 0)
		{	
			column = null;
			return false;
		}

		//	The edge 
		Vector3 edge = new Vector3(x, 0, z);		

		column = World.columns[this.position + (edge * World.chunkSize)];
		return true;
	}

}
