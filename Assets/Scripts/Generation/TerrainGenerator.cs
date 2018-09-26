using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator
{
	//	Static reference for current world
	public static TerrainLibrary.WorldBiomes worldBiomes = new TerrainLibrary.ExampleWorld();

	//	Hold values used in topology smoothing
	private struct Topology
	{
		public readonly float noise, height, baseNoise;
		public Topology(float noise, float height, float baseNoise)
		{
			this.noise = noise;			//	Noise for defining layer detail
			this.height = height;		//	Max height of layer
			this.baseNoise = baseNoise;	//	Biome base noise
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

	private Topology SmoothToPOI(Topology current, Topology other, float interpValue)
	{
		return new Topology(	Mathf.Lerp(other.noise, current.noise, interpValue),
								Mathf.Lerp(other.height, current.height, interpValue),
								Mathf.Lerp(other.baseNoise, current.baseNoise, interpValue));
	}

	//	Return 2 if outside margin
	//	else return 0 - 1 value representing closeness to border
	public static float EdgeGradient(float biomeNoise, float border = 0.5f, float margin = 0.05f)
	{
		if(biomeNoise < border-margin || biomeNoise > border+margin) return 2;

		if(biomeNoise < border)
			return Mathf.InverseLerp(border, border-margin, biomeNoise);
		
		return Mathf.InverseLerp(border, border+margin, biomeNoise);			
	}

	//	Get biome layer topology and smooth between layers at edges using Perlin or Simplex noise
	private Topology GetBiomeTopology(int x, int z, Column column, TerrainLibrary.Biome biome)
	{
		//	Global voxel column coordinates
		int gx = (int)(x+column.position.x);
		int gz = (int)(z+column.position.z);

		//	Base noise to map biome layers and base height
		float baseNoise = biome.BaseNoise(gx, gz);
		TerrainLibrary.BiomeLayer layer = biome.GetLayer(baseNoise);

		//	Do not overwrite data for this block when getting adjacent biome topology
		if(column.biomeLayers[x,z] == null) column.biomeLayers[x,z] = layer;

		//	Make sure gradient margins don't overlap
		float margin = (layer.max - layer.min) / 2;
		//	Clamp margin at max
		margin = margin > layer.maxMargin ? layer.maxMargin : margin;

		//	Layer height data for current layer
		Topology currentTopology = new Topology(layer.Noise(gx, gz),
												layer.maxHeight,
												baseNoise);

		//	Closeness to top and bottom of baseNoise range defining this biome layer
		float bottomGradient = EdgeGradient(baseNoise, layer.min, margin);
		float topGradient = EdgeGradient(baseNoise, layer.max, margin);
		
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
			//	No smoothing required
			return new Topology(currentTopology.noise, currentTopology.height, baseNoise);
		}

		//	Layer height data for adjacent layer
		Topology adjacentTopology = new Topology(	adjacentLayer.Noise(gx, gz),
													adjacentLayer.maxHeight,
													baseNoise);

		//	Return smoothed topology
		return SmoothTopologys(currentTopology, adjacentTopology, interpValue);
	}

	//	Get biome topology and smooth between biomes if necessary
	//	using Cellular value and distance-to-edge noise respectively
	public void GetTopologyData(Column column)
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
				FastNoise.EdgeData edgeData = column.edgeMap[x,z];

				//	Get current biome type
				TerrainLibrary.Biome currentBiome = worldBiomes.GetBiome(edgeData.currentCellValue);

				//	Get adjacent biome type
				TerrainLibrary.Biome adjacentBiome = worldBiomes.GetBiome(edgeData.adjacentCellValue);

				//	Get topology for this pixel
				Topology currentTolopogy = GetBiomeTopology(x, z, column, currentBiome);

				Topology finalTopology;

				//	Within smoothing radius and adjacent biome is different
				if(edgeData.distance2Edge < worldBiomes.smoothRadius && currentBiome != adjacentBiome)
				{
					if(!column.biomeBoundary) column.biomeBoundary = true;

					float InterpValue = Mathf.InverseLerp(0, worldBiomes.smoothRadius, edgeData.distance2Edge);

					//	Get topology for this pixel if adjacent biome type
					Topology adjacentTopology = GetBiomeTopology(x, z, column, adjacentBiome);

					//	Smooth between topologys
					finalTopology = SmoothTopologys(currentTolopogy, adjacentTopology, InterpValue);
				}
				else
				{
					finalTopology = currentTolopogy;
				}

				int POIheight = 0;

				//	Where points of interest exist, flatten terrain
				if(column.POIHeightGradient != null && column.POIHeightGradient[x,z] != 0)
				{
					float interpValue = (float)column.POIHeightGradient[x,z] / chunkSize;
					Topology POITopology = new Topology(0.5f, finalTopology.height, 0.5f);
					finalTopology = SmoothToPOI(POITopology, finalTopology, interpValue);

					//	Adjust heighest point
					POIheight = column.POIType.wallHeight;
				}

				//	Generate final height value for chunk data
				column.heightMap[x,z] = (int)Mathf.Lerp(0, finalTopology.height, finalTopology.baseNoise * finalTopology.noise);

				//	Update highest and lowest block in chunk column
				column.CheckHighest(column.heightMap[x,z] + POIheight);
				column.CheckLowest(column.heightMap[x,z]);
			}
	}

	public void GetCellData(Column column)
	{
		int chunkSize = World.chunkSize;

		column.edgeMap = new FastNoise.EdgeData[chunkSize,chunkSize];

		float currentCellValue = 0;

		//	Iterate over height map
		for(int x = 0; x < chunkSize; x++)
			for(int z = 0; z < chunkSize; z++)
			{
				//	Global voxel column coordinates
				int gx = (int)(x+column.position.x);
				int gz = (int)(z+column.position.z);

				//	Get cellular noise data
				FastNoise.EdgeData edgeData = worldBiomes.edgeNoiseGen.GetEdgeData(gx, gz);
				column.edgeMap[x,z] = edgeData;

				//	Store list of all cellValues present in this column
				if(edgeData.currentCellValue != currentCellValue)
				{
					currentCellValue = edgeData.currentCellValue;
					if(!column.cellValues.Contains(currentCellValue)) column.cellValues.Add(currentCellValue);
				}
			}
	}

	
}
