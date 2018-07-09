using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainLibrary
{
	#region Base

	//	Represents a large area of land
	//	Contains sub-biomes with their own hieghtmaps
	public class Biome
	{
		protected BiomeLayer[] layers;

		public Biome()
		{		
			 //	Default layer set
			layers = Layers();

			for(int i = 0; i < layers.Length; i++)
			{
				layers[i].max = GetMax(i);
				//	make sure margins do not cross in thin layers
				float margin = (layers[i].max - layers[i].min / 2);
			}
		}
		protected virtual BiomeLayer[] Layers() { return null; }

		//	Default noise for biomes etc
		public virtual float BaseNoise(int x, int z)
		{
			return NoiseUtils.BrownianMotion((x*0.002f), (z*0.002f)*2, 3, 0.3f);
		}

		//	Layer has a min parameter, max is above layer's min
		public float GetMax(int index)
		{
			if(index == 0) return 1;
			else return layers[index - 1].min;
		}

		//	Next in list
		public BiomeLayer LayerBelow(BiomeLayer layer)
		{
			for(int i = 0; i < layers.Length; i++)
			{
				if(layer == layers[i]) return layers[i+1];
			}
			return null;
		}
		//	Previous in list
		public BiomeLayer LayerAbove(BiomeLayer layer)
		{
			for(int i = 0; i < layers.Length; i++)
			{
				if(layer == layers[i]) return layers[i-1];
			}
			return null;
		}

		//	Check layer in column of voxels
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

	//	Represents one layer of the biome
	//	Layers defined by surface height
	public class BiomeLayer
	{
		public float maxMargin = 0.05f;
		public bool cut = false;
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
		public virtual float CutNoise(int x, int y, int z) { return 0; } 
	}

	#endregion	

	#region TestBiome

	public class TestBiome : Biome
	{
		protected override BiomeLayer[] Layers()
		{
			return new BiomeLayer[3] {	new TopLands(),
										new MidLands(),
										new LowLands()};
		}

		public class LowLands : BiomeLayer
		{
			public LowLands()
			{
				min = 0.0f;
				maxHeight = 100;
				surfaceBlock = Blocks.Types.LIGHTGRASS;
			}
			public override float Noise(int x, int z)
			{
				float noise = NoiseUtils.BrownianMotion(x * 0.015f, z * 0.015f, 3, 0.2f);
				return NoiseUtils.LevelOutAtMax(50, 0.5f, noise);
			}
		}

		public class MidLands : BiomeLayer
		{
			public MidLands()
			{
				min = 0.3f;
				maxHeight = 150;
				surfaceBlock = Blocks.Types.DIRT;
			}
			public override float Noise(int x, int z)
			{
				float noise = NoiseUtils.BrownianMotion(x * 0.015f, z * 0.015f, 3, 0.2f);
				return noise;
			}
		}

		public class TopLands : BiomeLayer
		{
			public TopLands()
			{
				min = 0.5f;
				maxHeight = 200;
				surfaceBlock = Blocks.Types.STONE;
			}
			public override float Noise(int x, int z)
			{
				float noise = NoiseUtils.BrownianMotion(x * 0.015f, z * 0.015f, 3, 0.2f);
				return noise;
			}
		}
	}

	#endregion

	#region MountainLowLands

	public class MountainLowLandsBiome : Biome
	{
		protected override BiomeLayer[] Layers()
		{
			return new BiomeLayer[4] {	new MountainPeak(),
										new Mountainous(),
										new MountainForest(),
										new LowLands()};
		}
		public override float BaseNoise(int x, int z)
		{
			return NoiseUtils.BrownianMotion((x*0.002f), (z*0.002f)*2, 3, 0.3f);
		}

		//	Layers

		public class MountainPeak : BiomeLayer
		{
			public MountainPeak()
			{
				min = 0.71f;
				maxHeight = 220;
				surfaceBlock = Blocks.Types.STONE;
			}
			public override float Noise(int x, int z)
			{
				float noise = NoiseUtils.BrownianMotion(x * 0.015f, z * 0.015f, 3, 0.2f);
				noise = NoiseUtils.ReverseTroughs(noise);

				float inverseNoise = NoiseUtils.BrownianMotion(x * 0.015f, z * 0.03f, 3, 0.2f);

				if(inverseNoise > noise)
				{
					noise -= (inverseNoise - noise);
				}

				return ((noise*3) + NoiseUtils.Rough(x, z, 1)) / 4;
			}
		}
		public class Mountainous : BiomeLayer
		{
			public Mountainous()
			{
				min = 0.62f;
				maxHeight = 100;
				surfaceBlock = Blocks.Types.DIRT;
			}
			public override float Noise(int x, int z)
			{
				float noise =  NoiseUtils.BrownianMotion(x * 0.005f, z * 0.005f, 1, 0.3f);
				float cragNoise = NoiseUtils.Noise(x, z, 2, 0.8f, 0.035f, 3, 1);

				if(cragNoise > 0.5f)
				{
					noise *= 1 - (cragNoise - 0.5f);
				}

				return noise;
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
	}

	#endregion
	
}
