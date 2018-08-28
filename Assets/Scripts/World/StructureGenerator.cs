using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureGenerator
{
	public static StructureLibrary.StructureTest structures = new StructureLibrary.StructureTest();


	public void GetStructureData(Column column)
	{
		column.structureMap = new StructureLibrary.Tiles[World.chunkSize,World.chunkSize];

		for(int x = 0; x < World.chunkSize; x++)
			for(int z = 0; z < World.chunkSize; z++)
			{
				if(column.biomeLayers[x,z].surfaceBlock == Blocks.Types.LIGHTGRASS && column.edgeMap[x,z].distance2Edge > 1)
				{
					int nx = Mathf.FloorToInt((x+column.position.x)/4);
					int nz = Mathf.FloorToInt((z+column.position.z)/4);
					int gx = (int)(x+column.position.x);
					int gz = (int)(z+column.position.z);
					column.structureMap[x,z] = structures.Tile(structures.GetNoise(nx, nz));
					//column.structureMap[x,z] = structures.Tile(structures.GetNoise(gx, gz));

					if(column.structureMap[x,z] > 0)
					{
						column.CheckHighest(column.heightMap[x,z] + structures.height);
					}
				}
			}
	}

	public void GenerateStructures(Column column)
	{
		Chunk currentOwner = null;

		for(int x = 0; x < World.chunkSize; x++)
			for(int z = 0; z < World.chunkSize; z++)
			{
				for(int y = 0; y < structures.height; y++)
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
	}

}
