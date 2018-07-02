using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator
{
	public static Biome defaultBiome = new FarmLand();

	public class Biome
	{
		public Blocks.Types[] layerTypes;
		public bool cut;
		public virtual int LayerHeight(int x, int y, int layerIndex) { return 0; }
		public void BuildColumn(World.Column column) { }

		public int LayerCount() { return layerTypes.Length; }
	}
	public class FarmLand : Biome
	{
		public FarmLand()
		{
			layerTypes = new Blocks.Types[] {Blocks.Types.STONE, Blocks.Types.DIRT};
			cut = true;
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

		/*public override void BuildColumn(World.Column column)
		{
			//	Initialise list of height maps
			column.heightMaps = new int[layerTypes.Length][,];

			//	Iterate over layers in biome
			for(int l = 0; l < layerTypes.Length; l++)
			{
				GenerateLayerHeight(column, biome, l);				
			}

			//	Biome does not use cut

			GenerateCuts(column, this);
		}*/
	}

	//	Generate topology data maps for biome
	public void GetTopologyData(World.Column column, Biome biome = null)
	{
		if(biome == null) biome = defaultBiome;


		//	Initialise list of height maps
		column.heightMaps = new int[biome.layerTypes.Length][,];

		//	Iterate over layers in biome
		for(int l = 0; l < biome.layerTypes.Length; l++)
		{
			GenerateLayerHeight(column, biome, l);				
		}

		//	Biome does not use cut
		if(!biome.cut) return;

		GenerateCuts(column, biome);
	}
	void GenerateLayerHeight(World.Column column, Biome biome, int layer)
	{
		int chunkSize = World.chunkSize;
		int[,] map = new int[chunkSize,chunkSize];

		for(int x = 0; x < chunkSize; x++)
			for(int z = 0; z < chunkSize; z++)
			{
				//	Get noise for layer
				int height = biome.LayerHeight(x + (int)column.position.x,
											z + (int)column.position.z,
											layer);	
				//	Record height								
				map[x,z] = height;

				//	Only get highest and lowest when drawing surface
				if(layer == column.heightMaps.Length - 1)
				{							 
					//	Lowest point
					column.CheckLowest(height);
				
					//	Highest point
					column.CheckHighest(height);
				}
			}

		column.heightMaps[layer] = map;
	}
	void GenerateCuts(World.Column column, Biome biome, float frequency = 0.025f, int octaves = 1, int maxDepth = 20, float chance = 0.42f)
	{
		int chunkSize = World.chunkSize;
		column.cuts = new int[chunkSize,chunkSize][];

		for(int x = 0; x < chunkSize; x++)
			for(int z = 0; z < chunkSize; z++)
			{
				//	Start and finish heights
				column.cuts[x,z] = new int[2];

				//	Got start of cut awaiting finish
				bool cutStarted = false;

				//	Current height of surface
				int surfaceHeight = column.heightMaps[biome.layerTypes.Length - 1][x,z] + 1;

				//	Iterate over column of blocks from depth to surface
				for(int y = surfaceHeight - maxDepth; y <= surfaceHeight; y++)
				{
					//	Procedurally generated 3d 'caves'
					float noise = NoiseUtils.BrownianMotion3D(x + column.position.x, y, z + column.position.z, frequency, octaves);
					if(noise < chance)
					{
						if(!cutStarted)
						{
							//	Bottom of cut
							column.cuts[x,z][0] = y;
							column.CheckLowest(y);
							cutStarted = true;
						}
						//		Top of cut
						else column.cuts[x,z][1] = y;
					}
				}
			}
	}

	

}
