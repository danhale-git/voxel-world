using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator
{
	//	Temporary static reference for debugging
	public static TerrainLibrary.WorldBiomes defaultWorld = new TerrainLibrary.ExampleWorld();

	//	Hold value used in topology smoothing
	private struct Topology
	{
		public readonly float noise, height, baseNoise;
		public Topology(float noise, float height, float baseNoise)
		{
			this.noise = noise;
			this.height = height;
			this.baseNoise = baseNoise;
		}
	}

	//	Interpolate between one topology and it's median with another
	private Topology SmoothTopologys(Topology current, Topology other, float interpValue)
	{
		float noiseMedian = (current.noise + other.noise) / 2;
		float heightMedian = (current.height + other.height) / 2;
		float baseNoiseMedian = (current.baseNoise + other.baseNoise) / 2;

		return new Topology(	Mathf.Lerp(noiseMedian, current.noise, interpValue),
								Mathf.Lerp(heightMedian, current.height, interpValue),
								Mathf.Lerp(baseNoiseMedian, current.baseNoise, interpValue));
	}

	//	Return 2 if outside margin
	//	else return 0 - 1 value representing closeness to border
	public static float GetGradient(float biomeNoise, float border = 0.5f, float margin = 0.05f)
	{
		if(biomeNoise < border-margin || biomeNoise > border+margin) return 2;

		if(biomeNoise < border)
			return Mathf.InverseLerp(border, border-margin, biomeNoise);
		
		return Mathf.InverseLerp(border, border+margin, biomeNoise);			
	}

	//	Get biome layer topology and smooth between layers at edges using Perlin or Simplex noise
	private Topology GetBiomeTopology(int x, int z, World.Column column, TerrainLibrary.Biome biome)
	{
		//	Global voxel column coordinates
		int gx = (int)(x+column.position.x);
		int gz = (int)(z+column.position.z);

		//	Base noise to map biome layers and base height
		float baseNoise = biome.BaseNoise(gx, gz);
		TerrainLibrary.BiomeLayer layer = biome.GetLayer(baseNoise);

		//	If statement prevents biome type from bein overwritten when getting adjacent biome topology
		if(column.biomeLayers[x,z] == null) column.biomeLayers[x,z] = layer;

		//	Make sure gradient margins don't overlap
		float margin = (layer.max - layer.min) / 2;
		//	Clamp margin at max
		margin = margin > layer.maxMargin ? layer.maxMargin : margin;

		Topology currentTopology = new Topology(layer.Noise(gx, gz),
												layer.maxHeight,
												baseNoise);

		float bottomGradient = GetGradient(baseNoise, layer.min, margin);
		float topGradient = GetGradient(baseNoise, layer.max, margin);
		
		TerrainLibrary.BiomeLayer adjacentLayer = null;
		float interpValue;

		//	Smooth to above layer
		if(bottomGradient != 2 && layer.min != 0)
		{
			adjacentLayer = biome.layers[layer.index - 1];
			interpValue = bottomGradient;
		}
		//	Smooth to below layer
		else if(topGradient != 2 && layer.max != 1)
		{
			adjacentLayer = biome.layers[layer.index + 1];
			interpValue = topGradient;
		}
		//	Not within margin distance of another layer
		else
		{
			return new Topology(currentTopology.noise, currentTopology.height, baseNoise);
		}

		Topology adjacentTopology = new Topology(	adjacentLayer.Noise(gx, gz),
													adjacentLayer.maxHeight,
													baseNoise);

		return SmoothTopologys(currentTopology, adjacentTopology, interpValue);
	}

	//	Get biome topology and smooth between biomes if necessary
	//	using Cellular value and distance-to-edge noise respectively
	public void GetTopologyData(World.Column column)
	{	
		int chunkSize = World.chunkSize;
		column.heightMap = new int[chunkSize,chunkSize];

		//	Iterate over height map
		for(int x = 0; x < chunkSize; x++)
			for(int z = 0; z < chunkSize; z++)
			{
				//	Global voxel column coordinates
				int gx = (int)(x+column.position.x);
				int gz = (int)(z+column.position.z);

				//	Get cellular noise data
				FastNoise.EdgeData edgeData = defaultWorld.edgeNoiseGen.GetEdgeData(gx, gz, defaultWorld);

				//	Get current biome type
				TerrainLibrary.Biome currentBiome = defaultWorld.GetBiome(edgeData.currentCellValue);

				//	Get biome edge gradient and adjacent biome type
				TerrainLibrary.Biome adjacentBiome = defaultWorld.GetBiome(edgeData.adjacentCellValue);

				//	Get topology for this pixel
				Topology currentTolopogy = GetBiomeTopology(x, z, column, currentBiome);

				Topology finalTopology;

				//	Use dynamic smooth radius if two edges overlap, prevents artefacts where cell width is less than smoothRadius*2
				float smooth = edgeData.overlap ? Mathf.Min(edgeData.maxSmoothRadius, defaultWorld.smoothRadius) : defaultWorld.smoothRadius;

				//	Within smoothing radius and adjacent biome is different
				if(edgeData.distance2Edge < smooth && currentBiome != adjacentBiome)
				{
					float InterpValue = Mathf.InverseLerp(0, smooth, edgeData.distance2Edge);

					//	Get topology for this pixel if adjacent biome type
					Topology adjacentTopology = GetBiomeTopology(x, z, column, adjacentBiome);

					finalTopology = SmoothTopologys(currentTolopogy, adjacentTopology, InterpValue);
				}
				else
				{
					finalTopology = currentTolopogy;
				}

				//	Generate final height value for chunk data
				column.heightMap[x,z] = (int)Mathf.Lerp(0, finalTopology.height, finalTopology.baseNoise * finalTopology.noise);

				//	Update highest and lowest in chunk column
				column.CheckHighest(column.heightMap[x,z]);
				column.CheckLowest(column.heightMap[x,z]);

			}					
	}

	
}
