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

	public TerrainLibrary.BiomeLayer[,] biomeLayers;
	public int[,] heightMap;

	public FastNoise.EdgeData[,] edgeMap;
	public List<float> cellValues = new List<float>();
	public bool biomeBoundary = false;

	public POILibrary.POI POIType;
	public int[,] POIMap;
	public int[,] POIDebug;
	public int[,] POIHeightGradient;
	public bool IsPOI = false;

	public int[,] POIWalls;

	public int highestPoint = 0;
	public int topChunkGenerate;
	public int topChunkDraw;	

	public int lowestPoint = 1000;
	public int bottomChunkGenerate;
	public int bottomChunkDraw;
	
	public Column(Vector3 position, World world)
	{
		biomeLayers = new TerrainLibrary.BiomeLayer[chunkSize,chunkSize];

		this.position = position;
		this.world = world;	

		world.GetColumnCellularData(this);

		IsPOI = SetPOIEligibility();
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

	bool SetPOIEligibility()
	{
		//	Column includes the boundary between two biomes
		if(biomeBoundary) return false;
		
		foreach(float cellValue in cellValues)
		{
			//	Column includes ineligible cells
			if(cellValue >= TerrainGenerator.worldBiomes.spawnStructuresAtNoise) return true;
		}
			
		return false;
	}

	public void GeneratePOIBlocks()
	{
		if(!IsPOI) return;

		bool hasBlocks = false;

		Chunk currentChunk = null;
		List<Chunk> allAlteredChunks = new List<Chunk>();

		bool walls = POIWalls != null;

		for(int x = 0; x < chunkSize; x++)
			for(int z = 0; z < chunkSize; z++)
			{

				if(walls)
				{
					int ly = LocalY(heightMap[x,z]);

					if(currentChunk == null)
					{
						int chunkY = Mathf.FloorToInt(heightMap[x,z] / chunkSize) * chunkSize;
						currentChunk = World.chunks[new Vector3(position.x, chunkY, position.z)];
					}

					if(POIWalls[x,z] == 1)
					{
						Chunk newChunk = null;
						if(BlockOwnerChunk(new Vector3(x,ly,z), currentChunk, out newChunk))
						{
							currentChunk = newChunk;
							allAlteredChunks.Add(newChunk);
						}

						for(int i = 0; i < POIType.wallHeight; i++)
						{
							int y = ly + i;
							currentChunk.blockTypes[x,y,z] = Blocks.Types.STONE;
							if(!hasBlocks) hasBlocks = true;
						}
					}
				}
			}

		foreach(Chunk chunk in allAlteredChunks)
		{
			if(chunk.composition == Chunk.Composition.EMPTY) chunk.composition = Chunk.Composition.MIX;
		}
	}

	public int LocalY(int globalY)
	{
		int localY = globalY;
		int iterationLimit = 500;
		while(localY >= 16)
		{
			localY -= chunkSize;
			iterationLimit--;
			if(iterationLimit < 1) break;
		}
		return localY;
	}


	/*if(column.IsPOI)
		{
			bool walls = column.POIWalls != null;

			for(int x = 0; x < World.chunkSize; x++)
				for(int z = 0; z < World.chunkSize; z++)
				{
					if(walls)
					{
						if(column.POIWalls[x,z] == 1)
						{
							int groundHeight = column.heightMap[x,z];
							for(int i = 0; i < column.POIType.wallHeight; i++)
							{
								int y = groundHeight + i;
								blockTypes[x,y,z] = Blocks.Types.STONE;
								if(!hasBlocks) hasBlocks = true;
							}
						}
					}
				}
		}*/

	public byte GetBitMask(Vector3 voxel, POILibrary.Tiles tile)
	{
		Vector3[] neighbours = Util.HorizontalBlockNeighbours(voxel);
		int value = 1;
		int total = 0;

		Column owner;

		for(int i = 0; i < neighbours.Length; i++)
		{
			Vector3 pos;
			
			if(BlockOwnerColumn(neighbours[i], out owner))
			{
				pos = Util.WrapBlockIndex(neighbours[i]);
			}
			else
			{
				if(owner == null) owner = this;
				pos = neighbours[i];
			}		

			int x = (int)pos.x, z = (int)pos.z;
			
			if(owner.POIMap[x,z] != (int)tile)
			{
				total += value;
			}
			value *= 2;
		}
		return (byte)total;
	}

	bool BlockOwnerColumn(Vector3 pos, out Column column)
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

	bool BlockOwnerChunk(Vector3 pos, Chunk currentChunk, out Chunk chunk)
	{
		//	Get block's edge
		int x = 0, z = 0, y = 0;

		if		(pos.x < 0) 				x = -1;
		else if (pos.x > World.chunkSize-1) 	x = 1;

		if		(pos.z < 0) 				z = -1;
		else if (pos.z > World.chunkSize-1) 	z = 1;

		if		(pos.y < 0) 				y = -1;
		else if (pos.y > World.chunkSize-1) 	y = 1;

		//	Voxel is in this chunk
		if(currentChunk != null && x == 0 && z == 0 && y == 0)
		{	
			chunk = null;
			return false;
		}

		//	The edge 
		Vector3 edge = new Vector3(x, y, z);		

		Debug.Log(currentChunk.position + (edge * World.chunkSize));

		chunk = World.chunks[currentChunk.position + (edge * World.chunkSize)];
		return true;
	}
}
