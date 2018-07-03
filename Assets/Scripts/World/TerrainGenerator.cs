using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator
{
	public static Biome farmLand = new FarmLand();
	public static Biome lowLands = new LowLands();


	public static TerrainGenerator.Biome GetBiome(int x, int z)
	{
		float biomeRoll = (float)Util.RoundToDP(NoiseUtils.BrownianMotion(x * 0.005f, z * 0.005f, 10), 3);
		if(biomeRoll < 0.5f)
		{
			return farmLand;
		}
			return lowLands;
	}
	public static float GetBiomeGradient(int x, int z)
	{
		float biomeRoll = (float)Util.RoundToDP(NoiseUtils.BrownianMotion(x * 0.005f, z * 0.005f, 10), 3);
		if(biomeRoll < 0.45f || biomeRoll > 0.55f) return 2;
		if(biomeRoll < 0.5f)
		{
			return Mathf.InverseLerp(0.5f, 0.45f, biomeRoll);
		}
			return Mathf.InverseLerp(0.5f, 0.55f, biomeRoll);			
	}

	public class Biome
	{
		public static int numberOfLayers = 2;
		//	Surface have caves cut into it
		public bool cut;

		//	Blocks used for each layer
		public Blocks.Types[] layerTypes;

		//	Select algorithm to generate height noise for layer
		public virtual float LayerHeight(int x, int z, int layerIndex) { return 0; }

		//	Height offset for each layer
		public virtual int LayerOffset(int layerIndex) { return layerIndex*10; }

		//	Max height of each layer
		public virtual int LayerMaxHeight(int layerIndex) { return 50; }
	}

	public class FarmLand : Biome
	{
		public FarmLand()
		{
			layerTypes = new Blocks.Types[] {Blocks.Types.STONE, Blocks.Types.DIRT};
			cut = false;
		}

		public override float LayerHeight(int x, int z, int layerIndex)
		{
			switch(layerIndex)
			{
				case 0:
					return NoiseUtils.TestGround(x,z);
				case 1:
					return NoiseUtils.TestGround(x,z);
				default:
					return 0;
			}
		}
		public override int LayerMaxHeight(int layerIndex) { return 100; }
	}
	public class LowLands : Biome
	{
		public LowLands()
		{
			layerTypes = new Blocks.Types[] {Blocks.Types.STONE, Blocks.Types.STONE};
			cut = false;
		}

		public override float LayerHeight(int x, int z, int layerIndex)
		{
			switch(layerIndex)
			{
				case 0:
					return NoiseUtils.LowLandsTest(x,z);
				case 1:
					return NoiseUtils.LowLandsTest(x,z);
				default:
					return 0;
			}
		}
	}

	//	TODO: proper biome implementation
	//	Generate topology data maps for biome
	public void GetTopologyData(World.Column column)
	{		
		int chunkSize = World.chunkSize;


		//	Initialise list of height maps
		column.heightMaps = new int[Biome.numberOfLayers][,];

		//	Iterate over layers in biome
		for(int l = 0; l < Biome.numberOfLayers; l++)
		{
			column.heightMaps[l] = new int[chunkSize,chunkSize];

			for(int x = 0; x < chunkSize; x++)
				for(int z = 0; z < chunkSize; z++)
				{

					GenerateLayerHeight(x, z,
										column,
										l);
				}				
		}

		column.cuts = new int[chunkSize,chunkSize][];
		
		for(int x = 0; x < chunkSize; x++)
			for(int z = 0; z < chunkSize; z++)
			{
				GenerateCuts(x, z,
							 column,
							 GetBiome((int)(x+column.position.x), (int)(z+column.position.z)));
			}
	}

	void GenerateLayerHeight(int x, int z, World.Column column, int layer)
	{
		int gx = (int)(x+column.position.x);
		int gz = (int)(z+column.position.z);
		Biome biome = GetBiome(gx, gz);

		//	Get height noise for layer
		float heightSource = biome.LayerHeight(gx, gz, layer);
		float otherHeightSource = 0;
		int maxHeight = biome.LayerMaxHeight(layer);

		float biomeGradient = GetBiomeGradient(gx, gz);

		if(biomeGradient <= 1)
		{
			Biome otherBiome = biome == farmLand ? lowLands : farmLand;
			otherHeightSource = otherBiome.LayerHeight(gx, gz, layer);
			int otherMaxHeight = otherBiome.LayerMaxHeight(layer);

			float median = (heightSource + otherHeightSource) / 2;
			float maxHeightMedian = (maxHeight + otherMaxHeight) / 2;

			heightSource = Mathf.Lerp(median, heightSource, biomeGradient);
			maxHeight = (int) Mathf.Lerp(maxHeightMedian, maxHeight, biomeGradient);
		}

		int offset = biome.LayerOffset(layer);
		int height = (int) Mathf.Lerp(offset, offset + maxHeight, heightSource);

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
		if(!biome.cut) return;
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
