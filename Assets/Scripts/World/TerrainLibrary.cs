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

	//	Represents one layer of a biome
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

	#region Layers

	public class MountainPeak : BiomeLayer
	{
		public MountainPeak()
		{
			min = 0.71f;
			maxHeight = 220;
			surfaceBlock = Blocks.Types.STONE;
			//cut = true;
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
			//return noise * (1 + (NoiseUtils.Rough(x, z, 1) / 2));
		}



		/*public override float CutNoise(int x, int y, int z)
		{
			return NoiseUtils.BrownianMotion3D(x, y, z, 0.05f, 1);
		}*/
	}
	public class Mountainous : BiomeLayer
	{
		public Mountainous()
		{
			min = 0.62f;
			maxHeight = 130;
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
	#endregion	
}
