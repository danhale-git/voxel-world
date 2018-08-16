using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator
{
	//	Temporary because there is only one biome
	public static TerrainLibrary.WorldBiomes defaultWorld = new TerrainLibrary.TestWorld();

	private struct SmoothedTopology
	{
		public readonly float noise, height, baseNoise;
		public SmoothedTopology(float noise, float height, float baseNoise)
		{
			this.noise = noise;
			this.height = height;
			this.baseNoise = baseNoise;
		}
	}

	//	Return 2 if outside margin from border
	//	else return 0 - 1 value representing closeness to border
	public static float GetGradient(float biomeNoise, float border = 0.5f, float margin = 0.05f)
	{
		if(biomeNoise < border-margin || biomeNoise > border+margin) return 2;

		if(biomeNoise < border)
			return Mathf.InverseLerp(border, border-margin, biomeNoise);
		
		return Mathf.InverseLerp(border, border+margin, biomeNoise);			
	}

	private SmoothedTopology GetBiomeTopology(int x, int z, World.Column column, TerrainLibrary.Biome biome)
	{
		SmoothedTopology topology;
	
		//	Global voxel column coordinates
		int gx = (int)(x+column.position.x);
		int gz = (int)(z+column.position.z);

		//	Base noise to map biome layers and base height
		float baseNoise = biome.BaseNoise(gx, gz);
		TerrainLibrary.BiomeLayer layer = biome.GetLayer(baseNoise);
		column.biomeLayers[x,z] = layer;
		TerrainLibrary.BiomeLayer adjacentLayer = null;

		//	Layer detail to overlay
		float layerNoise = layer.Noise(gx, gz);

		//	Make sure gradient margins don't overlap
		float margin = (layer.max - layer.min) / 2;
		//	Clamp margin at max
		margin = margin > layer.maxMargin ? layer.maxMargin : margin;

		//	Returns 0 - 1 gradient if in margin else returns 2
		float bottomGradient = GetGradient(baseNoise, layer.min, margin);
		float topGradient = GetGradient(baseNoise, layer.max, margin);

		float smoothedLayerNoise;
		float smoothedLayerHeight;

		//	Smooth to above layer
		if(bottomGradient != 2 && layer.min != 0)
		{
			adjacentLayer = biome.LayerBelow(layer);

			//	Find mid point between two layers
			float otherNoiseMedian = (layer.Noise(gx, gz) + adjacentLayer.Noise(gx, gz)) / 2;
			float otherHeightMedian = (layer.maxHeight + adjacentLayer.maxHeight) / 2;

			//	Lerp from mid point to layer using gradient
			smoothedLayerNoise = Mathf.Lerp(otherNoiseMedian, layerNoise, bottomGradient);
			smoothedLayerHeight = Mathf.Lerp(otherHeightMedian, layer.maxHeight, bottomGradient);
		}
		//	Smooth to below layer
		else if(topGradient != 2 && layer.max != 1)
		{
			adjacentLayer = biome.LayerAbove(layer);

			float otherNoiseMedian = (layer.Noise(gx, gz) + adjacentLayer.Noise(gx, gz)) / 2;
			float otherHeightMedian = (layer.maxHeight + adjacentLayer.maxHeight) / 2;

			smoothedLayerNoise = Mathf.Lerp(otherNoiseMedian, layerNoise, topGradient);
			smoothedLayerHeight = Mathf.Lerp(otherHeightMedian, layer.maxHeight, topGradient);
		}
		//	Not within margin distance of another layer
		else
		{
			//	Default to layer height
			smoothedLayerNoise = layerNoise;
			smoothedLayerHeight = layer.maxHeight;
		}

		topology = new SmoothedTopology(smoothedLayerNoise, smoothedLayerHeight, baseNoise);

		return topology;
	}

	//	Generate height maps
	// 	Smooth between biome layers
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

				float noise;
				float height;
				float baseNoise;

				//	Get topology for this block column
				SmoothedTopology currentBiome = GetBiomeTopology(x, z, column, defaultWorld.GetBiome(gx, gz));

				//	Get biome edge gradient and adjacent biome type
				float edgeNoise = defaultWorld.edgeNoiseGen.GetNoise(gx, gz);

				if(edgeNoise < 0.2f)
				{
					edgeNoise = Mathf.InverseLerp(0, 0.2f, edgeNoise);

					TerrainLibrary.Biome adjacentBiomeType = defaultWorld.GetBiome(defaultWorld.edgeNoiseGen.AdjacentCellValue(gx, gz));

					//	Get topology for this block column if adjacent biome type
					SmoothedTopology adjacentBiome = GetBiomeTopology(x, z, column, adjacentBiomeType);

					float noiseMedian = (currentBiome.noise + adjacentBiome.noise) / 2;
					float heightMedian = (currentBiome.height + adjacentBiome.height) / 2;
					float baseNoiseMedian = (currentBiome.baseNoise + adjacentBiome.baseNoise) / 2;

					noise = Mathf.Lerp(noiseMedian, currentBiome.noise, edgeNoise);
					height = Mathf.Lerp(heightMedian, currentBiome.height, edgeNoise);
					baseNoise = Mathf.Lerp(baseNoiseMedian, currentBiome.baseNoise, edgeNoise);
				}
				else
				{
					noise = currentBiome.noise;
					height = currentBiome.height;
					baseNoise = currentBiome.baseNoise;
				}

				//	Final height value
				column.heightMap[x,z] = (int)Mathf.Lerp(0, height, baseNoise * noise);

				//	Update highest and lowest in column
				column.CheckHighest(column.heightMap[x,z]);
				column.CheckLowest(column.heightMap[x,z]);

			}
							
	}

}
