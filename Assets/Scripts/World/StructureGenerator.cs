using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureGenerator
{
	public static StructureLibrary.StructureTest structures = TerrainGenerator.worldBiomes.structures;


	public bool GetStructureMap(Column column)
	{
		column.structureMap = new StructureLibrary.Tiles[World.chunkSize,World.chunkSize];
		bool structuresWereSpawned = false;

		for(int x = 0; x < World.chunkSize; x++)
			for(int z = 0; z < World.chunkSize; z++)
			{
				FastNoise.EdgeData edgeData = column.edgeMap[x,z];
				if( edgeData.currentCellValue >= TerrainGenerator.worldBiomes.spawnStructuresAtNoise && edgeData.distance2Edge > 1)
				{
					Vector3 voxel = column.position + new Vector3(x, 0, z);

					column.structureMap[x,z] = structures.Tile(structures.GetNoise(voxel.x, voxel.z));

					if(column.structureMap[x,z] > 0)
					{
						column.CheckHighest(column.heightMap[x,z] + structures.wallHeight);
						if(!structuresWereSpawned) structuresWereSpawned = true;
					}
				}
			}
		return structuresWereSpawned;
	}

	public void GenerateStructures(Column column)
	{
		if(!column.hasStructures) return;
		
		for(int x = 0; x < World.chunkSize; x++)
			for(int z = 0; z < World.chunkSize; z++)
			{
				switch(column.structureMap[x,z])
				{
					case StructureLibrary.Tiles.WALL:
						GenerateWalls(x, z, column);
						break;

					case StructureLibrary.Tiles.PATH:
						GenerateDirt(x, z, column);
						break;
				}
			}
	}

	void GenerateWalls(int x, int z, Column column)
	{
		Chunk currentOwner = null;

		for(int y = 0; y < structures.wallHeight; y++)
		{
			int gy = y + column.heightMap[x,z] + 1;

			if(column.structureMap[x,z] != 0)
			{
				Vector3 voxel = new Vector3(x, gy, z) + column.position;

				if(currentOwner == null || !Util.InChunk(gy - currentOwner.position.y, 0))
				{
					currentOwner = World.VoxelOwnerChunk(voxel);
				}

				currentOwner.blockTypes[x, (int)(gy-currentOwner.position.y), z] = Blocks.Types.STONE;

				if(currentOwner.composition != Chunk.Composition.MIX) currentOwner.composition = Chunk.Composition.MIX;
			}
		}
	}

	void GenerateDirt(int x, int z, Column column)
	{
		int y = column.heightMap[x,z];
		Vector3 voxel = new Vector3(x,y,z) + column.position;
		Chunk owner = World.VoxelOwnerChunk(voxel);
		owner.blockTypes[x,y - (int)owner.position.y,z] = Blocks.Types.DIRT;
	}

	public void ProcessStructures(List<Column> columns)
	{
		
	}

}
