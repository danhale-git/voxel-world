using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator
{
	public static Biome defaultBiome = new Biome();

	public static float GetGradient(float biomeNoise, float border = 0.5f, float margin = 0.05f, bool debug = false)
	{
		if(debug)
		{
			Debug.Log("margins: "+(border-margin)+" - "+(border+margin)+" : "+biomeNoise);
		}

		if(biomeNoise < border-margin || biomeNoise > border+margin) return 2;
		if(biomeNoise < border)
		{
			return Mathf.InverseLerp(border, border-margin, biomeNoise);
		}
			return Mathf.InverseLerp(border, border+margin, biomeNoise);			
	}

	public class BiomeLayer
	{
		public float maxMargin = 0.05f;
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
				//	make sure margins do not cross in thin layers
				float margin = (layers[i].max - layers[i].min / 2);
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

				//	Default to layer values
				float otherNoiseMedian = 0;
				float otherHeightMedian = 0;


				//	Make sure gradients don't overlap
				float margin = (layer.max - layer.min) / 2;
				margin = margin > layer.maxMargin ? layer.maxMargin : margin;

				float bottomGradient = GetGradient(baseNoise, layer.min, margin);
				float topGradient = GetGradient(baseNoise, layer.max, margin);

				float finalNoise;
				float finalHeight;

				if(bottomGradient != 2 && layer.min != 0)
				{
					BiomeLayer adjacentLayer = defaultBiome.LayerBelow(layer);

					otherNoiseMedian = (layer.Noise(gx, gz) + adjacentLayer.Noise(gx, gz)) / 2;
					otherHeightMedian = (layer.maxHeight + adjacentLayer.maxHeight) / 2;

					finalNoise = Mathf.Lerp(otherNoiseMedian, layerNoise, bottomGradient);
					finalHeight = Mathf.Lerp(otherHeightMedian, layer.maxHeight, bottomGradient);
				}
				else if(topGradient != 2 && layer.max != 1)
				{
					BiomeLayer adjacentLayer = defaultBiome.LayerAbove(layer);

					otherNoiseMedian = (layer.Noise(gx, gz) + adjacentLayer.Noise(gx, gz)) / 2;
					otherHeightMedian = (layer.maxHeight + adjacentLayer.maxHeight) / 2;

					finalNoise = Mathf.Lerp(otherNoiseMedian, layerNoise, topGradient);
					finalHeight = Mathf.Lerp(otherHeightMedian, layer.maxHeight, topGradient);
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
