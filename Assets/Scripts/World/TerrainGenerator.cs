using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator
{
	//	Temporary because there is only one biome
	public static TerrainLibrary.Biome defaultBiome = new TerrainLibrary.Biome();

	//	Return 2 if outside margin from border
	//	else return 0 - 1 value representing closeness to border
	public static float GetGradient(float biomeNoise, float border = 0.5f, float margin = 0.05f)
	{
		if(biomeNoise < border-margin || biomeNoise > border+margin) return 2;

		if(biomeNoise < border)
			return Mathf.InverseLerp(border, border-margin, biomeNoise);
		
		return Mathf.InverseLerp(border, border+margin, biomeNoise);			
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

				//	Base noise to map biome layers and base height
				float baseNoise = defaultBiome.BaseNoise(gx, gz);
				TerrainLibrary.BiomeLayer layer = defaultBiome.GetLayer(baseNoise);
				column.biomeLayers[x,z] = layer;
				TerrainLibrary.BiomeLayer adjacentLayer = null;

				//	Layer detail to overlay
				float layerNoise = layer.Noise(gx, gz);

				//	Make sure gradient margins don't overlap
				float margin = (layer.max - layer.min) / 2;
				margin = margin > layer.maxMargin ? layer.maxMargin : margin;

				//	Returns 0 - 1 gradient if in margin else returns 2
				float bottomGradient = GetGradient(baseNoise, layer.min, margin);
				float topGradient = GetGradient(baseNoise, layer.max, margin);

				float smoothedNoise;
				float smoothedHeight;

				//	Smooth to above layer
				if(bottomGradient != 2 && layer.min != 0)
				{
					adjacentLayer = defaultBiome.LayerBelow(layer);

					//	Find mid point between two layers
					float otherNoiseMedian = (layer.Noise(gx, gz) + adjacentLayer.Noise(gx, gz)) / 2;
					float otherHeightMedian = (layer.maxHeight + adjacentLayer.maxHeight) / 2;

					//	Lerp from mid point to layer using gradient
					smoothedNoise = Mathf.Lerp(otherNoiseMedian, layerNoise, bottomGradient);
					smoothedHeight = Mathf.Lerp(otherHeightMedian, layer.maxHeight, bottomGradient);
				}
				//	Smooth to below layer
				else if(topGradient != 2 && layer.max != 1)
				{
					adjacentLayer = defaultBiome.LayerAbove(layer);

					float otherNoiseMedian = (layer.Noise(gx, gz) + adjacentLayer.Noise(gx, gz)) / 2;
					float otherHeightMedian = (layer.maxHeight + adjacentLayer.maxHeight) / 2;

					smoothedNoise = Mathf.Lerp(otherNoiseMedian, layerNoise, topGradient);
					smoothedHeight = Mathf.Lerp(otherHeightMedian, layer.maxHeight, topGradient);
				}
				//	Not within margin distance of another layer
				else
				{
					//	Default to layer height
					smoothedNoise = layerNoise;
					smoothedHeight = layer.maxHeight;
				}

				//	Final height value
				column.heightMap[x,z] = (int)Mathf.Lerp(0, smoothedHeight, baseNoise * smoothedNoise);

				//	Update highest and lowest in column
				column.CheckHighest(column.heightMap[x,z]);
				column.CheckLowest(column.heightMap[x,z]);

				/* //	//	Cuts	//	//

				if(!layer.cut) continue;

				//	Current height of surface
				int surfaceHeight = column.heightMap[x,z] + 1;

				float chance = 0.42f;
				int maxDepth = 20;

				//	Iterate over column of blocks from depth to surface
				for(int y = surfaceHeight - maxDepth; y <= surfaceHeight; y++)
				{
					//	Procedurally generated 3d 'caves'
					float cutNoise = layer.CutNoise(gx, y, gz);

					if(cutNoise < chance)
					{
						if(column.cuts[x,z] == null)
						{
							column.cuts[x,z] = new int[2];
							//	Bottom of cut
							column.cuts[x,z][0] = y;
							column.CheckLowest(y);
						}
						//		Top of cut
						else column.cuts[x,z][1] = y;
					}
				}*/
			}				
	}

}
