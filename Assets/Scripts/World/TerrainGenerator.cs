using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator
{
	public static Biome defaultBiome = new FarmLand();

	int[] Cut(int x, int z, World.Column column, Biome biome, float frequency = 0.025f, int octaves = 1, int maxDepth = 20, float chance = 0.42f)
	{
		//	Start and finish heights
		int[] cut = new int[2];

		//	Got start of cut awaiting finish
		bool cutStarted = false;

		//	Current height of surface
		int surfaceHeight = column.heightMaps[biome.layerTypes.Length - 1][x,z] + 1;

		for(int y = surfaceHeight - maxDepth; y <= surfaceHeight; y++)
		{
			float noise = NoiseUtils.BrownianMotion3D(x + column.position.x, y, z + column.position.z, frequency, octaves);
			if(noise < chance)
			{
				if(!cutStarted)
				{
					cut[0] = y;

					//	Lowest point
					if(y < column.lowestPoint)
						column.lowestPoint = y;

					cutStarted = true;
				}
				else cut[1] = y;

			}
		}
		return cut;
	}

	public void GetHeightmaps(World.Column column, Biome biome = null)
	{
		if(biome == null) biome = defaultBiome;

		int chunkSize = World.chunkSize;
		int[][,] maps = new int[biome.layerTypes.Length][,];
		int[,][] cuts;

		//	initalise lowest as high
		int lowest = 10000;
		//	initialise heighest low
		int highest  = 0;

		for(int l = 0; l < maps.Length; l++)
		{
			maps[l] = new int[chunkSize, chunkSize];
			for(int x = 0; x < chunkSize; x++)
				for(int z = 0; z < chunkSize; z++)
				{				
					maps[l][x,z] = biome.LayerHeight(x + (int)column.position.x,
												 z + (int)column.position.z,
												 l);

					//	Only get highest and lowest when drawing surface
					if(l == maps.Length - 1)
					{							 
						//	Lowest point
						if(maps[l][x,z] < lowest)
							lowest = maps[l][x,z];
					
						//	Highest point
						if(maps[l][x,z] > highest)
							highest = maps[l][x,z];
					}
					
				}
		}
		for(int x = 0; x < chunkSize; x++)
			for(int z = 0; z < chunkSize; z++)
			{
				
			}

		column.lowestPoint = lowest;
		column.highestPoint = highest;
		column.heightMaps = maps;

		if(!biome.cut) return;
		column.cuts = cuts = new int[chunkSize,chunkSize][];

		for(int x = 0; x < chunkSize; x++)
			for(int z = 0; z < chunkSize; z++)
			{
				column.cuts[x,z] = Cut(x, z, column, biome);
			}

	}

	public class Biome
	{
		public Blocks.Types[] layerTypes;
		public bool cut;
		public virtual int LayerHeight(int x, int y, int layerIndex) { return 0; }
	}

	public class FarmLand : Biome
	{
		public FarmLand()
		{
			layerTypes = new Blocks.Types[] {Blocks.Types.STONE, Blocks.Types.DIRT};
			cut = false;
		}

		public override int LayerHeight(int x, int y, int layerIndex)
		{
			switch(layerIndex)
			{
				case 0:
					return(NoiseUtils.TestGround(x,y, 70));
				case 1:
					return(NoiseUtils.TestGround(x,y, 70) + 10);
				default:
					return 0;
			}
		}
	}

}
