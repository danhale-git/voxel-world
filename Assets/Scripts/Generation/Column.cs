using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Column
{
	WorldManager world;

	int chunkSize = WorldManager.chunkSize;
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
	
	public Column(Vector3 position, WorldManager world)
	{
		biomeLayers = new TerrainLibrary.BiomeLayer[chunkSize,chunkSize];

		this.position = position;
		this.world = world;	

		world.GetColumnCellularData(this);

		IsPOI = SetPOIEligibility();
	}

	public static Column Get(Vector3 position)
	{
		Column column = WorldManager.columns[new Vector3(position.x, 0, position.z)];
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

	//	TODO: Iterate over y then x and z for less chunk swapping
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
				if(!walls || POIWalls[x,z] == 0) continue;

				int ly = LocalY(heightMap[x,z]);

				//	Get starting chunk
				int chunkY = Mathf.FloorToInt(heightMap[x,z] / chunkSize) * chunkSize;
				currentChunk = WorldManager.chunks[new Vector3(position.x, chunkY, position.z)];

				//	Handle POI with different heights at different points
				Chunk newChunk = null;
				if(BlockOwnerChunk(new Vector3(x,ly,z), currentChunk, out newChunk))
				{
					currentChunk = newChunk;
					allAlteredChunks.Add(currentChunk);
				}
				
				int iterationReset = 0;

				for(int i = 0; i < POIType.wallHeight; i++)
				{
					int y = (ly + i) - iterationReset;

					//	Iteration has moved out of current chunk
					if(y > 15)
					{
						bool gotChunk = BlockOwnerChunk(new Vector3(x,y,z), currentChunk, out currentChunk);
						allAlteredChunks.Add(currentChunk);

						//	Offset i to zero for new chunk
						iterationReset = i;
						//	Local y to zero for new chunk
						ly = 0;
						//	New y value for new chunk
						y = (ly + i) - iterationReset;
					}
									
					switch(POIWalls[x,z])
					{
						case 1:
							//if(y > 15 || y < 0) continue;	//	DEBUG !!!kS
							currentChunk.blockTypes[x,y,z] = Blocks.Types.STONE;
							if(!hasBlocks) hasBlocks = true;
							break;
						
						case 2:
							if(i<3) continue;
							//if(y > 15 || y < 0) continue;	//	DEBUG !!!kS
							currentChunk.blockTypes[x,y,z] = Blocks.Types.STONE;
							if(!hasBlocks) hasBlocks = true;
							break;

						default:
							break;
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

	/*public byte GetBitMask(Vector3 voxel, POILibrary.Tiles tile)
	{
		Vector3[] neighbours = Util.HorizontalBlockNeighbours(voxel);
		int value = 1;
		int total = 0;

		Column owner;
*
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
	}*/

	bool BlockOwnerColumn(Vector3 pos, out Column column)
	{
		//	Get block's edge
		int x = 0, z = 0;

		if		(pos.x < 0) 				x = -1;
		else if (pos.x > WorldManager.chunkSize-1) 	x = 1;

		if		(pos.z < 0) 				z = -1;
		else if (pos.z > WorldManager.chunkSize-1) 	z = 1;

		//	Voxel is in this column
		if(x == 0 && z == 0)
		{	
			column = null;
			return false;
		}

		//	The edge 
		Vector3 edge = new Vector3(x, 0, z);		

		column = WorldManager.columns[this.position + (edge * WorldManager.chunkSize)];
		return true;
	}

	bool BlockOwnerChunk(Vector3 pos, Chunk currentChunk, out Chunk chunk)
	{
		//	Get block's edge
		int x = 0, z = 0, y = 0;

		if		(pos.x < 0) 				x = -1;
		else if (pos.x > WorldManager.chunkSize-1) 	x = 1;

		if		(pos.z < 0) 				z = -1;
		else if (pos.z > WorldManager.chunkSize-1) 	z = 1;

		if		(pos.y < 0) 				y = -1;
		else if (pos.y > WorldManager.chunkSize-1) 	y = 1;

		//	Voxel is in this chunk
		if(currentChunk != null && x == 0 && z == 0 && y == 0)
		{	
			chunk = null;
			return false;
		}

		//	The edge 
		Vector3 edge = new Vector3(x, y, z);		

		chunk = WorldManager.chunks[currentChunk.position + (edge * WorldManager.chunkSize)];
		return true;
	}
}
