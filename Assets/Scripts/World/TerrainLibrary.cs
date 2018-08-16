using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainLibrary
{
	#region Base

	//	Represents the whole game world
	//	Generates biomes areas using cellular noise
	public class WorldBiomes
	{
		public FastNoise biomeNoiseGen = new FastNoise();
		public FastNoise edgeNoiseGen = new FastNoise();

		protected List<Biome> biomes;

		public virtual Biome GetBiome(int x, int z) { return null; }
		public virtual Biome GetBiome(float noise) { return null; }
	}

	//	Represents a large area of land
	//	Contains sub-biomes with their own heightmaps
	public class Biome
	{
		protected BiomeLayer[] layers;
		protected FastNoise noiseGen = new FastNoise();

		public Biome()
		{		
			 //	Default layer set
			layers = Layers();

			for(int i = 0; i < layers.Length; i++)
			{
				layers[i].max = GetMax(i);
			}
		}
		protected virtual BiomeLayer[] Layers() { return null; }

		//	Default noise for biomes etc
		public virtual float BaseNoise(int x, int z)
		{
			return noiseGen.GetNoise01(x, z);
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
		protected FastNoise noiseGen = new FastNoise();


		public bool IsHere(float pixelNoise)
		{
			if( pixelNoise >= min && pixelNoise < max) return true;
			return false;
		}
		public virtual float Noise(int x, int z) { return 0; }
		//public virtual float CutNoise(int x, int y, int z) { return 0; } 
	}

	#endregion

	#region TestWorld

	public class TestWorld : WorldBiomes
	{
		public TestWorld()
		{
			float frequency = 0.01f;
			biomeNoiseGen.SetNoiseType(FastNoise.NoiseType.Cellular);
			biomeNoiseGen.SetCellularReturnType(FastNoise.CellularReturnType.CellValue);
			biomeNoiseGen.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
			biomeNoiseGen.SetFrequency(frequency);

			edgeNoiseGen.SetNoiseType(FastNoise.NoiseType.Cellular);
			edgeNoiseGen.SetCellularReturnType(FastNoise.CellularReturnType.Distance2Sub);
			edgeNoiseGen.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
			edgeNoiseGen.SetFrequency(frequency);

			biomes = new List<Biome>
			{
				new TestBiome(),
				new TestBiome2(),
				new TestBiome3(),
				new TestBiome4(),
				new TestBiome5(),
				new TestBiome6(),
				new TestBiome7(),
				new TestBiome8(),
				new TestBiome9(),
				new TestBiome10()

			};
		}

		public override Biome GetBiome(float noise)
		{
			if(noise < 0.1f)
			{
				return biomes[0];
			}
			else if(noise < 0.2f)
			{
				return biomes[1];
			}
			else if(noise < 0.3f)
			{
				return biomes[2];
			}
			else if(noise < 0.4f)
			{
				return biomes[3];
			}
			else if(noise < 0.5f)
			{
				return biomes[4];
			}
			else if(noise < 0.6f)
			{
				return biomes[5];
			}
			else if(noise < 0.7f)
			{
				return biomes[6];
			}
			else if(noise < 0.8f)
			{
				return biomes[7];
			}
			else if(noise < 0.9f)
			{
				return biomes[8];
			}
			else
			{
				return biomes[9];
			}
		}

		public override Biome GetBiome(int x, int z)
		{
			return GetBiome(biomeNoiseGen.GetNoise01(x, z));		
		}
	}

	public class TestWorld2 : WorldBiomes
	{
		public TestWorld2()
		{
			biomeNoiseGen.SetNoiseType(FastNoise.NoiseType.Cellular);
			biomeNoiseGen.SetCellularReturnType(FastNoise.CellularReturnType.CellValue);
			biomeNoiseGen.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
			biomeNoiseGen.SetFrequency(0.01f);

			edgeNoiseGen.SetNoiseType(FastNoise.NoiseType.Cellular);
			edgeNoiseGen.SetCellularReturnType(FastNoise.CellularReturnType.Distance2Sub);
			edgeNoiseGen.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
			edgeNoiseGen.SetFrequency(0.01f);

			biomes = new List<Biome>
			{
				new MountainLowLandsBiome()
			};
		}

		public override Biome GetBiome(int x, int z)
		{
			float noise = biomeNoiseGen.GetNoise01(x, z);
			return biomes[0];		
		}

		public override Biome GetBiome(float noise)
		{
			return biomes[0];		
		}
	}

	#endregion

	#region TestBiome

	public class TestBiome : Biome
	{
		public TestBiome() : base()
		{
			noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			noiseGen.SetFrequency(0.005f);
			noiseGen.SetSeed(26235);
		}
		protected override BiomeLayer[] Layers()
		{
			return new BiomeLayer[1] {new LowLands()};
		}

		public class LowLands : BiomeLayer
		{
			public LowLands()
			{
				min = 0.0f;
				maxHeight = 100;
				surfaceBlock = Blocks.Types.STONE;

				noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			}
			public override float Noise(int x, int z)
			{
				return 1;
			}
		}
	}

	public class TestBiome2 : Biome
	{
		public TestBiome2() : base()
		{
			noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			noiseGen.SetFrequency(0.005f);
			noiseGen.SetSeed(26235);
		}
		protected override BiomeLayer[] Layers()
		{
			return new BiomeLayer[1] {new LowLands()};
		}

		public class LowLands : BiomeLayer
		{
			public LowLands()
			{
				min = 0.0f;
				maxHeight = 80;
				surfaceBlock = Blocks.Types.STONE;

				noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			}
			public override float Noise(int x, int z)
			{
				return 1;
			}
		}
	}

	public class TestBiome3 : Biome
	{
		public TestBiome3() : base()
		{
			noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			noiseGen.SetFrequency(0.005f);
			noiseGen.SetSeed(26235);
		}
		protected override BiomeLayer[] Layers()
		{
			return new BiomeLayer[1] {new LowLands()};
		}

		public class LowLands : BiomeLayer
		{
			public LowLands()
			{
				min = 0.0f;
				maxHeight = 70;
				surfaceBlock = Blocks.Types.STONE;

				noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			}
			public override float Noise(int x, int z)
			{
				return 1;
			}
		}
	}

	public class TestBiome4 : Biome
	{
		public TestBiome4() : base()
		{
			noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			noiseGen.SetFrequency(0.005f);
			noiseGen.SetSeed(26235);
		}
		protected override BiomeLayer[] Layers()
		{
			return new BiomeLayer[1] {new LowLands()};
		}

		public class LowLands : BiomeLayer
		{
			public LowLands()
			{
				min = 0.0f;
				maxHeight = 60;
				surfaceBlock = Blocks.Types.STONE;

				noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			}
			public override float Noise(int x, int z)
			{
				return 1;
			}
		}
	}

	public class TestBiome5 : Biome
	{
		public TestBiome5() : base()
		{
			noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			noiseGen.SetFrequency(0.005f);
			noiseGen.SetSeed(26235);
		}
		protected override BiomeLayer[] Layers()
		{
			return new BiomeLayer[1] {new LowLands()};
		}

		public class LowLands : BiomeLayer
		{
			public LowLands()
			{
				min = 0.0f;
				maxHeight = 50;
				surfaceBlock = Blocks.Types.STONE;

				noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			}
			public override float Noise(int x, int z)
			{
				return 1;
			}
		}
	}

	public class TestBiome6 : Biome
	{
		public TestBiome6() : base()
		{
			noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			noiseGen.SetFrequency(0.005f);
			noiseGen.SetSeed(26235);
		}
		protected override BiomeLayer[] Layers()
		{
			return new BiomeLayer[1] {new LowLands()};
		}

		public class LowLands : BiomeLayer
		{
			public LowLands()
			{
				min = 0.0f;
				maxHeight = 40;
				surfaceBlock = Blocks.Types.STONE;

				noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			}
			public override float Noise(int x, int z)
			{
				return 1;
			}
		}
	}

	public class TestBiome7 : Biome
	{
		public TestBiome7() : base()
		{
			noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			noiseGen.SetFrequency(0.005f);
			noiseGen.SetSeed(26235);
		}
		protected override BiomeLayer[] Layers()
		{
			return new BiomeLayer[1] {new LowLands()};
		}

		public class LowLands : BiomeLayer
		{
			public LowLands()
			{
				min = 0.0f;
				maxHeight = 30;
				surfaceBlock = Blocks.Types.STONE;

				noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			}
			public override float Noise(int x, int z)
			{
				return 1;
			}
		}
	}

	public class TestBiome8 : Biome
	{
		public TestBiome8() : base()
		{
			noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			noiseGen.SetFrequency(0.005f);
			noiseGen.SetSeed(26235);
		}
		protected override BiomeLayer[] Layers()
		{
			return new BiomeLayer[1] {new LowLands()};
		}

		public class LowLands : BiomeLayer
		{
			public LowLands()
			{
				min = 0.0f;
				maxHeight = 20;
				surfaceBlock = Blocks.Types.STONE;

				noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			}
			public override float Noise(int x, int z)
			{
				return 1;
			}
		}
	}

	public class TestBiome9 : Biome
	{
		public TestBiome9() : base()
		{
			noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			noiseGen.SetFrequency(0.005f);
			noiseGen.SetSeed(26235);
		}
		protected override BiomeLayer[] Layers()
		{
			return new BiomeLayer[1] {new LowLands()};
		}

		public class LowLands : BiomeLayer
		{
			public LowLands()
			{
				min = 0.0f;
				maxHeight = 10;
				surfaceBlock = Blocks.Types.STONE;

				noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			}
			public override float Noise(int x, int z)
			{
				return 1;
			}
		}
	}

	public class TestBiome10 : Biome
	{
		public TestBiome10() : base()
		{
			noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			noiseGen.SetFrequency(0.005f);
			noiseGen.SetSeed(26235);
		}
		protected override BiomeLayer[] Layers()
		{
			return new BiomeLayer[1] {new LowLands()};
		}

		public class LowLands : BiomeLayer
		{
			public LowLands()
			{
				min = 0.0f;
				maxHeight = 5;
				surfaceBlock = Blocks.Types.STONE;

				noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			}
			public override float Noise(int x, int z)
			{
				return 1;
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
