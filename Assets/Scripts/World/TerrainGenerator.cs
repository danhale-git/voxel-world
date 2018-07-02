using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator
{
	public static Blocks.Types[] layerTypes = new Blocks.Types[] {Blocks.Types.STONE, Blocks.Types.DIRT};

	int LayerHeight(int x, int y, int layerIndex)
	{
		switch(layerIndex)
		{
			case 0:
				return(NoiseUtils.HillyPlateus(x,y, 70, ridgify: true));
			case 1:
				return(NoiseUtils.HillyPlateus(x,y, 70, ridgify: false) - 10);
			default:
				return 0;
		}
	}

	public void GetHeightmaps(World.Column column)
	{
		int chunkSize = World.chunkSize;
		int[][,] maps = new int[layerTypes.Length][,];
		int[,][] cuts = new int[chunkSize,chunkSize][];

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
					maps[l][x,z] = LayerHeight(x + (int)column.position.x,
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
				bool cutStarted = false;
				cuts[x,z] = new int[2];
				int surfaceHeight = maps[layerTypes.Length - 1][x,z] + 1;
				for(int y = surfaceHeight - 20; y <= surfaceHeight; y++)
				{
					float noise = NoiseUtils.BrownianMotion3D(x + column.position.x, y, z + column.position.z, 0.05f, 1);
					if(noise < 0.42f)
					{
						if(!cutStarted)
						{
							cuts[x,z][0] = y;

							//	Lowest point
							if(y < lowest)
								lowest = y;
							cutStarted = true;
						}
						else cuts[x,z][1] = y;

					}
				}
			}

		column.cuts = cuts;	

		column.lowestPoint = lowest;
		column.highestPoint = highest;
		column.heightMaps = maps;
	}

}
