
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
		//	Distance (FastNoise.NoiseType.Distance2EdgeSub) at which biomes begin smoothing
		public float smoothRadius = 0.2f;
		public bool handleSmoothOverlap = false;

		public StructureLibrary.StructureTest structures;
		public float spawnStructuresAtNoise;

		public WorldBiomes()
		{
			//	Default cellular noise settings
			biomeNoiseGen.SetNoiseType(FastNoise.NoiseType.Cellular);
			biomeNoiseGen.SetCellularReturnType(FastNoise.CellularReturnType.CellValue);
			biomeNoiseGen.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
			biomeNoiseGen.SetFrequency(0.01f);
			biomeNoiseGen.SetInterp(FastNoise.Interp.Quintic);	//	Faster noise generation
			biomeNoiseGen.SetFractalOctaves(1);	//	Faster noise generation

			edgeNoiseGen.SetNoiseType(FastNoise.NoiseType.Cellular);
			edgeNoiseGen.SetCellularReturnType(FastNoise.CellularReturnType.Distance2Sub);
			edgeNoiseGen.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
			edgeNoiseGen.SetFrequency(0.01f);
			edgeNoiseGen.SetInterp(FastNoise.Interp.Quintic);	//	Faster noise generation
			edgeNoiseGen.SetFractalOctaves(1);	//	Faster noise generation
		}

		//	Assign next biome's min as max
		protected void InitialiseBiomes()
		{
			for(int i = 0; i < biomes.Length; i++)
			{
				biomes[i].max = i == biomes.Length - 1 ? 1 : biomes[i + 1].min;
			}
		}

		//	Smaller value larger cells
		protected void SetBiomeFrequency(float frequency)
		{
			biomeNoiseGen.SetFrequency(frequency);
			edgeNoiseGen.SetFrequency(frequency);
		}

		//	CellValue for biome type
		public FastNoise biomeNoiseGen = new FastNoise();
		//	Distance2EdgeSub for edge smoothing
		public FastNoise edgeNoiseGen = new FastNoise();

		protected Biome[] biomes;

		//	Get biome using position or noise
		public Biome GetBiome(int x, int z) { return GetBiome( biomeNoiseGen.GetNoise01(x, z) ); }
		public virtual Biome GetBiome(float noise)
		{
			for(int i = 0; i < biomes.Length; i++)
			{
				if(noise >= biomes[i].min && noise < biomes[i].max)
					return biomes[i];
			}
			return biomes[biomes.Length - 1];
		}

		//	Get index of biome in list - used for comparing biomes in FastNoise.GetEdgeData()
		public int GetBiomeIndex(int x, int z) { return GetBiomeIndex( biomeNoiseGen.GetNoise01(x, z) ); }
		public virtual int GetBiomeIndex(float noise)
		{
			for(int i = 0; i < biomes.Length; i++)
			{
				if(noise >= biomes[i].min && noise < biomes[i].max)
					return i;
			}
			return biomes.Length - 1;
		}
	}

	//	Represents a large area of land
	//	Contains sub-biomes with their own heightmaps
	public class Biome
	{
		//	Define biome layers and their minimum base noise values in derived class
		public BiomeLayer[] layers;

		protected FastNoise baseNoiseGen = new FastNoise();

		//	Min and max cellular values
		public float min;
		public float max;

		public Biome(float minNoise)
		{
			min = minNoise;
		}

		protected void InitialiseLayers()
		{
			for(int i = 0; i < layers.Length; i++)
			{	
				//	Assign above layer's min as this layer's max
				layers[i].max = i == layers.Length - 1 ? 1 : layers[i + 1].min;
				layers[i].index = i;
			}
		}

		//	Default noise for biomes etc
		public virtual float BaseNoise(int x, int z)
		{
			return baseNoiseGen.GetNoise01(x, z);
		}

		//	Find layer using pixelNoise
		public BiomeLayer GetLayer(float pixelNoise)
		{
			for(int i = 0; i < layers.Length; i++)
			{
				if(pixelNoise >= layers[i].min && pixelNoise < layers[i].max)
					return layers[i];				
			}
			return layers[layers.Length - 1];
		}	
	}

	//	Represents one layer of the biome
	//	Layers defined by surface height
	public class BiomeLayer
	{
		public BiomeLayer(float minNoise = 0.5f)
		{
			min = minNoise;
		}

		//	Index of layer in this biome
		public int index;

		//	Maximum smoothing margin
		public float maxMargin = 0.05f;

		//	Maximum ground height in voxels
		public int maxHeight;

		//	Min and max base noise values
		public float min;
		public float max;

		//	Block type
		public Blocks.Types surfaceBlock;

		protected FastNoise noiseGen = new FastNoise();

		public virtual float Noise(int x, int z) { return 0; }
	}

	#endregion

	#region Derived

	public class ExampleWorld : WorldBiomes
	{
		public ExampleWorld() : base()
		{
			smoothRadius = 0.6f;
			SetBiomeFrequency(0.01f);
			//handleSmoothOverlap = true;

			biomes = new Biome[2]
			{
				new HillyFlats(	0),
				new Mountainous(0.5f)
			};

			InitialiseBiomes();
		}
	}

	//	Simple hills with flat valleys
	public class HillyFlats : Biome
	{
		public HillyFlats(float minNoise) : base(minNoise)
		{
			baseNoiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			baseNoiseGen.SetFrequency(0.02f);

			layers = new BiomeLayer[2] { 	new Flats(	0	),
											new Hills(	0.5f)};

			InitialiseLayers();
		}
	}
	public class Hills : BiomeLayer
	{
		public Hills(float minNoise) : base(minNoise)
		{
			maxHeight = 25;
			surfaceBlock = Blocks.Types.LIGHTGRASS;

			noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			noiseGen.SetFrequency(0.02f);
		}

		public override float Noise(int x, int z)
		{
			return noiseGen.GetNoise01(x, z);
		}
	}
	public class Flats : BiomeLayer
	{
		public Flats(float minNoise) : base(minNoise)
		{
			maxHeight = 20;
			surfaceBlock = Blocks.Types.LIGHTGRASS;

			noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			noiseGen.SetFrequency(0.02f);
		}

		public override float Noise(int x, int z)
		{
			return noiseGen.GetNoise01(x, z);
		}
	}

	//	Simple mountains
	public class Mountainous : Biome
	{
		public Mountainous(float minNoise) : base(minNoise)
		{
			baseNoiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			baseNoiseGen.SetFrequency(0.015f);
			baseNoiseGen.SetFractalOctaves(2);

			layers = new BiomeLayer[1] { 	new Mountains(	0	)};
		}
	}
	public class Mountains : BiomeLayer
	{
		public Mountains(float minNoise) : base(minNoise)
		{
			maxHeight = 90;
			surfaceBlock = Blocks.Types.STONE;

			noiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			noiseGen.SetFrequency(0.015f);
			noiseGen.SetFractalOctaves(2);
		}

		public override float Noise(int x, int z)
		{
			return noiseGen.GetNoise01(x, z);
		}
	}

	#endregion

	#region Testing

	public class TestBiomes : WorldBiomes
	{
		public TestBiomes() : base()
		{
			structures = new StructureLibrary.StructureTest();

			smoothRadius = 0.6f;
			SetBiomeFrequency(0.01f);
			
			spawnStructuresAtNoise = 0.8f;

			biomes = new Biome[2]
			{
				new TestBiome1(0),
				new TestBiome2(0.25f)
			};

			InitialiseBiomes();
		}
	}

	public class TestBiome1 : Biome
	{
		public TestBiome1(float minNoise) : base(minNoise)
		{
			layers = new BiomeLayer[1] { 	new TestLayer1(0)};
			InitialiseLayers();
		}
		public override float BaseNoise(int x, int z)
		{
			return 1;
		}
		
	}
	public class TestLayer1 : BiomeLayer
	{
		public TestLayer1(float minNoise) : base(minNoise)
		{
			maxHeight = 10;
			surfaceBlock = Blocks.Types.DIRT;
		}

		public override float Noise(int x, int z)
		{
			return 1;
		}
	}

	public class TestBiome2 : Biome
	{
		public TestBiome2(float minNoise) : base(minNoise)
		{
			layers = new BiomeLayer[1] { 	new TestLayer2(0)};
			InitialiseLayers();
		}
		public override float BaseNoise(int x, int z)
		{
			return 1;
		}
		
	}
	public class TestLayer2 : BiomeLayer
	{
		public TestLayer2(float minNoise) : base(minNoise)
		{
			maxHeight = 30;
			surfaceBlock = Blocks.Types.LIGHTGRASS;
		}

		public override float Noise(int x, int z)
		{
			return 1;
		}
	}

	#endregion
}
