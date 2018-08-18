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
		//	Distance (FastNoise.NoiseType.Distance2EdgeSub) from the cell edge at which biomes are smoothed
		public float smoothRadius = 0.2f;

		//	CellValue
		public FastNoise biomeNoiseGen = new FastNoise();
		//	Distance2EdgeSub
		public FastNoise edgeNoiseGen = new FastNoise();

		protected List<Biome> biomes;

		public Biome GetBiome(int x, int z) { return GetBiome( biomeNoiseGen.GetNoise01(x, z) ); }
		public virtual Biome GetBiome(float noise) { return null; }
	}

	//	Represents a large area of land
	//	Contains sub-biomes with their own heightmaps
	public class Biome
	{
		//	Define biome layers and their minimum base noise values in derived class
		public BiomeLayer[] layers;
		protected FastNoise baseNoiseGen = new FastNoise();

		protected void InitialiseLayers()
		{
			for(int i = 0; i < layers.Length; i++)
			{
				layers[i].max = GetMax(i);
				layers[i].index = i;
			}
		}

		protected virtual BiomeLayer[] Layers() { return null; }

		//	Default noise for biomes etc
		public virtual float BaseNoise(int x, int z)
		{
			return baseNoiseGen.GetNoise01(x, z);
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
		public BiomeLayer(float startAtNoise = 0.5f)
		{
			min = startAtNoise;
		}

		//	index of layer in this biome
		public int index;

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

	#region Derived

	public class Temperate : WorldBiomes
	{
		public Temperate()
		{
			biomeNoiseGen.SetNoiseType(FastNoise.NoiseType.Cellular);
			biomeNoiseGen.SetCellularReturnType(FastNoise.CellularReturnType.CellValue);
			biomeNoiseGen.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
			biomeNoiseGen.SetFrequency(0.01f);

			edgeNoiseGen.SetNoiseType(FastNoise.NoiseType.Cellular);
			edgeNoiseGen.SetCellularReturnType(FastNoise.CellularReturnType.Distance2Sub);
			edgeNoiseGen.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);
			edgeNoiseGen.SetFrequency(0.01f);

			smoothRadius = 0.5f;

			biomes = new List<Biome>
			{
				new HillyFlats(),
				new Mountainous()
			};
		}

		public override Biome GetBiome(float noise)
		{
			if(noise > 0.5)
				return biomes[1];
			else
				return biomes[0];		
		}
	}

	//	Simple hills with flat valleys
	public class HillyFlats : Biome
	{
		public HillyFlats() : base()
		{
			baseNoiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			baseNoiseGen.SetFrequency(0.02f);

			layers = new BiomeLayer[2] { 	new Hills(	0.5f),
											new Flats(	0	)};

			InitialiseLayers();
		}
	}
	public class Hills : BiomeLayer
	{
		public Hills(float start) : base(start)
		{
			maxHeight = 40;
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
		public Flats(float start) : base(start)
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
		public Mountainous() : base()
		{
			baseNoiseGen.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
			baseNoiseGen.SetFrequency(0.015f);
			baseNoiseGen.SetFractalOctaves(2);

			layers = new BiomeLayer[1] { 	new Mountains(	0	)};
		}
	}
	public class Mountains : BiomeLayer
	{
		public Mountains(float start) : base(start)
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
}
