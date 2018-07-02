using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator
{
	public class Biome
	{
		//	Surface have caves cut into it
		public bool cut;

		//	Blocks used for each layer
		public Blocks.Types[] layerTypes;

		//	Select algorithm to generate height noise for layer
		public virtual int LayerHeight(int x, int y, int layerIndex) { return 0; }
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
	}

	public static Biome defaultBiome = new FarmLand();

	//	TODO: proper biome implementation
	//	Generate topology data maps for biome
	public void GetTopologyData(World.Column column, Biome biome = null)
	{
		if(biome == null) biome = defaultBiome;
		
		int chunkSize = World.chunkSize;


		//	Initialise list of height maps
		column.heightMaps = new int[biome.layerTypes.Length][,];

		//	Iterate over layers in biome
		for(int l = 0; l < biome.layerTypes.Length; l++)
		{
			column.heightMaps[l] = new int[chunkSize,chunkSize];

			for(int x = 0; x < chunkSize; x++)
				for(int z = 0; z < chunkSize; z++)
				{
					GenerateLayerHeight(x, z, column, biome, l);
				}				
		}

		//	Biome does not use cut
		if(!biome.cut) return;

		column.cuts = new int[chunkSize,chunkSize][];
		
		for(int x = 0; x < chunkSize; x++)
			for(int z = 0; z < chunkSize; z++)
			{
				GenerateCuts(x, z, column, biome);
			}
	}

	void GenerateLayerHeight(int x, int z, World.Column column, Biome biome, int layer)
	{		
		//	Get noise for layer
		int height = biome.LayerHeight(x + (int)column.position.x,
									   z + (int)column.position.z,
									   layer);	
		//	Record height								
		column.heightMaps[layer][x,z] = height;

		//	Only get highest and lowest when drawing surface
		if(layer == column.heightMaps.Length - 1)
		{							 
			//	Lowest point
			column.CheckLowest(height);
		
			//	Highest point
			column.CheckHighest(height);
		}
	}
	void GenerateCuts(int x, int z, World.Column column, Biome biome, float frequency = 0.025f, int octaves = 1, int maxDepth = 20, float chance = 0.42f)
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
