using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator
{
	public static Biome defaultBiome = new Biome();


	/*public static float GetBiomeGradient(int x, int z)
	{
		float biomeRoll = (float)Util.RoundToDP(NoiseUtils.BrownianMotion(x * 0.005f, z * 0.005f, 10), 3);
		if(biomeRoll < 0.45f || biomeRoll > 0.55f) return 2;
		if(biomeRoll < 0.5f)
		{
			return Mathf.InverseLerp(0.5f, 0.45f, biomeRoll);
		}
			return Mathf.InverseLerp(0.5f, 0.55f, biomeRoll);			
	} */

	public static float GetGradient(float biomeNoise, float border = 0.5f, float margin = 0.05f)
	{
		if(biomeNoise < border-margin || biomeNoise > border+margin) return 2;
		if(biomeNoise < border)
		{
			return Mathf.InverseLerp(border, border-margin, biomeNoise);
		}
			return Mathf.InverseLerp(border, border+margin, biomeNoise);			
	}

	public class BiomeLayer
	{
		protected float gradientMargin = 0.5f;
		public float min;
		public float max;
		public int maxHeight;
		public Blocks.Types surfaceBlock;

		public bool IsHere(float pixelNoise)
		{
			if( pixelNoise >= min && pixelNoise < max) return true;
			return false;
		}
		public virtual float Noise(int x, int z) { return 0; } 
	}

	public class MountainPeak : BiomeLayer
	{
		public MountainPeak()
		{
			min = 0.71f;
			maxHeight = 200;
			surfaceBlock = Blocks.Types.STONE;
		}
		public override float Noise(int x, int z)
		{
			return NoiseUtils.BrownianMotion(x * 0.015f, z * 0.015f, 3, 0.2f);
		} 


	}
	public class Mountainous : BiomeLayer
	{
		public Mountainous()
		{
			min = 0.62f;
			maxHeight = 150;
			surfaceBlock = Blocks.Types.DIRT;
		}
		public override float Noise(int x, int z)
		{
			return NoiseUtils.BrownianMotion(x * 0.005f, z * 0.005f, 1, 0.3f);
		} 

	}
	public class MountainForest : BiomeLayer
	{
		public MountainForest()
		{
			min = 0.5f;
			maxHeight = 80;
			surfaceBlock = Blocks.Types.GRASS;
		}
		public override float Noise(int x, int z)
		{
			return NoiseUtils.HillyPlateusTest(x, z);
		} 

	}
	public class LowLands : BiomeLayer
	{
		public LowLands()
		{
			min = 0.0f;
			maxHeight = 50;
			surfaceBlock = Blocks.Types.LIGHTGRASS;
		}
		public override float Noise(int x, int z)
		{
			return NoiseUtils.LowLandsTest(x, z);
		} 

	}

	public class Biome
	{
		public BiomeLayer[] layers = new BiomeLayer[4] {new MountainPeak(), new Mountainous(), new MountainForest(), new LowLands()};

		public Biome()
		{
			for(int i = 0; i < layers.Length; i++)
			{
				layers[i].max = GetMax(i);
			}
		}
		public float GetMax(int index)
		{
			if(index == 0) return 1;
			else return layers[index - 1].min;
		}
		public BiomeLayer LayerBelow(BiomeLayer layer)
		{
			for(int i = 0; i < layers.Length; i++)
			{
				if(layer == layers[i]) return layers[i+1];
			}
			return null;
		}
		public BiomeLayer LayerAbove(BiomeLayer layer)
		{
			for(int i = 0; i < layers.Length; i++)
			{
				if(layer == layers[i]) return layers[i-1];
			}
			return null;
		}

		public float BaseNoise(int x, int z)
		{
			return NoiseUtils.BrownianMotion((x*0.002f), (z*0.002f)*2, 3, 0.3f);
		}

		public BiomeLayer GetLayer(float pixelNoise)
		{
			for(int i = 0; i < layers.Length; i++)
			{
				if(layers[i].IsHere(pixelNoise))
				{
					return layers[i];
				}
			}
			return layers[0];
		}	
	}

	//	TODO: proper biome implementation
	//	Generate topology data maps for biome
	public void GetTopologyData(World.Column column)
	{		
		int chunkSize = World.chunkSize;

		column.heightMap = new int[chunkSize,chunkSize];

		//	Iterate over layers in biome
		for(int x = 0; x < chunkSize; x++)
			for(int z = 0; z < chunkSize; z++)
			{
				int gx = (int)(x+column.position.x);
				int gz = (int)(z+column.position.z);

				float baseNoise = defaultBiome.BaseNoise(gx, gz);
				BiomeLayer layer = defaultBiome.GetLayer(baseNoise);

				float layerNoise = layer.Noise(gx, gz);

				//column.heightMap[x,z] = (int)Mathf.Lerp(0, layer.maxHeight, baseNoise * layer.Noise(gx, gz));	//	//

				
				//	Default to layer values
				float bottomNoiseMedian = 0;
				float bottomHeightMedian = 0;
				bool smoothBottom = false;

				float topNoiseMedian = 0;
				float topHeightMedian = 0;
				bool smoothTop = false;

				float bottomGradient = GetGradient(baseNoise, layer.min);
				float topGradient = GetGradient(baseNoise, layer.max);

				if(bottomGradient != 2 && layer.min != 0)
				{
					smoothBottom = true;
					BiomeLayer adjacentLayer = defaultBiome.LayerBelow(layer);
					float adjacentNoise = adjacentLayer.Noise(gx, gz);
					int adjacentHeight = adjacentLayer.maxHeight;

					bottomNoiseMedian = (layer.Noise(gx, gz) + adjacentNoise) / 2;
					bottomHeightMedian = (layer.maxHeight + adjacentHeight) / 2;

					//bottomNoiseMedian = Mathf.Lerp(bottomNoiseMedian, layerNoise, bottomGradient);
					//bottomHeightMedian = Mathf.Lerp(bottomHeightMedian, layer.maxHeight, bottomGradient);
				}
				
				if(topGradient != 2 && layer.max != 1)
				{
					smoothTop = true;
					BiomeLayer adjacentLayer = defaultBiome.LayerAbove(layer);
					float adjacentNoise = adjacentLayer.Noise(gx, gz);
					int adjacentHeight = adjacentLayer.maxHeight;

					topNoiseMedian = (layer.Noise(gx, gz) + adjacentNoise) / 2;
					topHeightMedian = (layer.maxHeight + adjacentHeight) / 2;

					//topNoiseMedian = Mathf.Lerp(topNoiseMedian, layerNoise, topGradient);
					//topHeightMedian = Mathf.Lerp(topHeightMedian, layer.maxHeight, topGradient);
				}

				float finalNoise;
				float finalHeight;

				if(smoothBottom && smoothTop)
				{
					float gradient = (topGradient + bottomGradient) / 2;
					float noiseMedian = (topNoiseMedian + bottomNoiseMedian) / 2;
					float heightMedian = (topHeightMedian + bottomHeightMedian) / 2;

					finalNoise = Mathf.Lerp(noiseMedian, layerNoise, gradient);
					finalHeight = Mathf.Lerp(heightMedian, layer.maxHeight, gradient);
				}
				else if(smoothBottom)
				{
					finalNoise = Mathf.Lerp(bottomNoiseMedian, layerNoise, bottomGradient);
					finalHeight = Mathf.Lerp(bottomHeightMedian, layer.maxHeight, bottomGradient);
				}
				else if(smoothTop)
				{
					finalNoise = Mathf.Lerp(topNoiseMedian, layerNoise, topGradient);
					finalHeight = Mathf.Lerp(topHeightMedian, layer.maxHeight, topGradient);
				}
				else
				{
					finalNoise = layerNoise;
					finalHeight = layer.maxHeight;
				}

				column.heightMap[x,z] = (int)Mathf.Lerp(0, finalHeight, baseNoise * finalNoise);

				column.CheckHighest(column.heightMap[x,z]);
				column.CheckLowest(column.heightMap[x,z]);				
			}				
		

		column.cuts = new int[chunkSize,chunkSize][];
		
		/*for(int x = 0; x < chunkSize; x++)
			for(int z = 0; z < chunkSize; z++)
			{
				GenerateCuts(x, z,
							 column,
							 GetBiome((int)(x+column.position.x), (int)(z+column.position.z)));
			}*/
	}

	/*void GenerateLayerHeight(int x, int z, World.Column column, int layer)
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
	}*/

	/*void GenerateCuts(int x, int z, World.Column column, Biome biome, float frequency = 0.025f, int octaves = 1, int maxDepth = 20, float chance = 0.42f)
	{
		if(!biome.cut) return;
		//	Start and finish heights
		column.cuts[x,z] = new int[2];

		//	Got start of cut awaiting finish
		bool cutStarted = false;

		//	Current height of surface
		int surfaceHeight = column.heightMap[x,z] + 1;

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
	}*/
}
