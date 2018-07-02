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
				return(NoiseUtils.HillyPlateus(x,y, 70, ridgify: false) + 20);
			default:
				return 0;
		}
	}

	public int[][,] GetHeightmaps(World.Column column)
	{
		int chunkSize = World.chunkSize;
		int[][,] maps = new int[layerTypes.Length][,];

		//	initalise lowest as high
		int lowest = 10000;
		//	initialise heighest low
		int highest  = 0;

		for(int l = 0; l < maps.Length; l++)
		{
			maps[l] = new int[chunkSize, chunkSize];
			for(int _x = 0; _x < chunkSize; _x++)
				for(int _z = 0; _z < chunkSize; _z++)
				{				
					maps[l][_x,_z] = LayerHeight(_x + (int)column.position.x,
												 _z + (int)column.position.z,
												 l);

					//	Only get highest and lowest when drawing surface
					if(l == maps.Length - 1)
					{							 
						//	Lowest point
						if(maps[l][_x,_z] < lowest)
							lowest = maps[l][_x,_z];
					
						//	Highest point
						if(maps[l][_x,_z] > highest)
							highest = maps[l][_x,_z];
					}
					
				}
		}
		column.lowestPoint = lowest;
		column.highestPoint = highest;
		return maps;
	}

}
