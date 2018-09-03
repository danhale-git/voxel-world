using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class Util
{
	public static double EpochMilliseconds()
	{
		DateTime dt1970 = new DateTime(1970, 1, 1);
    	DateTime current = DateTime.UtcNow;
    	TimeSpan span = current - dt1970;
    	return span.TotalMilliseconds;
	}

	/*public static Vector3[] CubeFaceDirections()
	{
		return new Vector3[]
		{
			Vector3.up,			//	0
			Vector3.down,		//	1
			Vector3.right,		//	2
			Vector3.left,		//	3
			Vector3.forward,	//	4
			Vector3.back		//	5
		};
	}*/

	public static Vector3[] CubeFaceDirections()
	{
		return new Vector3[]
		{
			Vector3.forward,			//	0
			Vector3.right,		//	1
			Vector3.back,		//	2
			Vector3.left,		//	3
			Vector3.up,	//	4
			Vector3.down		//	5
		};
	}

	public static Vector3[] AdjacentDirections()
	{
		return new Vector3[]
		{
			Vector3.up,
			Vector3.down,

			Vector3.right,
			Vector3.left,
			Vector3.forward,
			Vector3.back,
			new Vector3(0, 1, 1),	//	right forward
			new Vector3(0, -1, 1),	//	left forward
			new Vector3(0, 1, -1),	//	right back
			new Vector3(0, -1, -1),//	left back

			new Vector3(1, 1, 0),	//	up right
			new Vector3(1, -1, 0),	//	up left
			new Vector3(1, 0, 1),	//	up forward
			new Vector3(1, 0, -0),	//	up back
			new Vector3(1, 1, 1),	//	up right forward
			new Vector3(1, -1, 1),	//	up left forward
			new Vector3(1, 1, -1),	//	up right back
			new Vector3(1, -1, -1),	//	up left back

			new Vector3(-1, 1, 0),	//	down right
			new Vector3(-1, -1, 0),	//	down left
			new Vector3(-1, 0, 1),	//	down forward
			new Vector3(-1, 0, -0),	//	down back
			new Vector3(-1, 1, 1),	//	down right forward
			new Vector3(-1, -1, 1),	//	down left forward
			new Vector3(-1, 1, -1),	//	down right back
			new Vector3(-1, -1, -1),//	down left back
		};
	}

	//	Round Vector values to nearest ints
	public static Vector3 RoundVector3(Vector3 toRound)
	{
		Vector3 rounded = new Vector3(	Mathf.Round(toRound.x),
										Mathf.Round(toRound.y),
										Mathf.Round(toRound.z));
		return rounded;
	}

	public static bool InChunk(float value, float offsetIn = 0)
	{
		if(value < 0 + offsetIn || value >= World.chunkSize - offsetIn) return false;
		return true;
	}
	public static bool InChunk(Vector3 value, float offsetIn = 0)
	{
		if(	(value.x < 0 + offsetIn || value.x >= World.chunkSize - offsetIn) ||
			(value.y < 0 + offsetIn || value.y >= World.chunkSize - offsetIn) ||
			(value.z < 0 + offsetIn || value.z >= World.chunkSize - offsetIn) ) return false;
		return true;
	}

	//	Wrap local block positions outside max chunk size
	public static Vector3 WrapBlockIndex(Vector3 index)
	{
		float[] vector = new float[3] {	index.x,
									index.y,
									index.z};

		for(int i = 0; i < 3; i++)
		{
			//	if below min then max
			if(vector[i] == -1) 
				vector[i] = World.chunkSize-1; 
			//	if above max then min
			else if(vector[i] == World.chunkSize) 
				vector[i] = 0;
		}

		return new Vector3(vector[0], vector[1], vector[2]);
	}

	//	Return 8 adjacent positions
	public static Vector3[] HorizontalBlockNeighbours(Vector3 voxel)
	{
		//	The order of the vectors in this list is sacred
		//	changing it will break bitmasking
		return new Vector3[]
		{
			new Vector3(voxel.x+1, voxel.y, voxel.z),
			new Vector3(voxel.x-1, voxel.y, voxel.z),
			new Vector3(voxel.x, voxel.y, voxel.z+1),
			new Vector3(voxel.x, voxel.y, voxel.z-1),
			new Vector3(voxel.x+1, voxel.y, voxel.z+1),
			new Vector3(voxel.x+1, voxel.y, voxel.z-1),
			new Vector3(voxel.x-1, voxel.y, voxel.z+1),
			new Vector3(voxel.x-1, voxel.y, voxel.z-1)
		};
	}

	//	TODO: make this faster
	public static Vector3[] HorizontalChunkNeighbours(Vector3 position)
	{
		int chunkSize = World.chunkSize;
		return new Vector3[] { 	(Vector3.right * chunkSize) + position,
								(Vector3.left * chunkSize) + position,
								(Vector3.forward * chunkSize) + position,
								(Vector3.back * chunkSize) + position,
								((Vector3.right + Vector3.forward) * chunkSize) + position,
								((Vector3.right + Vector3.back) * chunkSize) + position,
								((Vector3.left + Vector3.forward) * chunkSize) + position,
								((Vector3.left + Vector3.back) * chunkSize) + position	};
	}

	public static double RoundToDP(float value, int decimalPlaces)
	{
		return System.Math.Round(value, decimalPlaces);
	}

	public static int MinInt(int[] values)
	{
		int min = values[0];
		for(int i = 1; i < values.Length; i++)
		{
			if(values[i] < min) min = values[i];
		}
		return min;
	}

	public static int MinIntIndex(int[] values)
	{
		int minIndex = 0;
		int minValue = values[0];
		for(int i = 0; i < values.Length; i++)
		{
			if(values[i] <= minValue)
			{
				minValue = values[i];
				minIndex = i;
			}
		}
		return minIndex;
	}

	public static int MaxIntIndex(int[] values)
	{
		int maxIndex = 0;
		int maxValue = values[0];
		for(int i = 0; i < values.Length; i++)
		{
			if(values[i] >= maxValue)
			{
				maxValue = values[i];
				maxIndex = i;
			}
		}
		return maxIndex;
	}

	static Color DebugBlockColor(int x, int z, Column column)
	{
		Color color;
		FastNoise.EdgeData edge = column.edgeMap[x,z];
		if(edge.distance2Edge < 0.002f)
		{
			color = Color.black;
		}
		else
		{
			if(edge.currentCellValue >= 0.5f)
				color = Color.red;
			else
				color = Color.cyan;
		}
		color -= color * (float)(Mathf.InverseLerp(0, 0.1f, edge.distance2Edge) / 1.5);
		if(edge.distance2Edge < TerrainGenerator.worldBiomes.smoothRadius) color -= new Color(0.1f,0.1f,0.1f);
		return color;
	}
}
