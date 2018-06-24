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

	public static void SpiralTest()
	{
		Vector3 position = new Vector3(0,50,0);
		int increment = 1;
		for(int i = 0; i < 10; i++)
		{
			for(int r = 0; r < increment; r++)
			{
				position += Vector3.right;
				//CreateCube(position);
			}
			for(int d = 0; d < increment; d++)
			{
				position += Vector3.back;
				//CreateCube(position);
			}

			increment++;

			for(int l = 0; l < increment; l++)
			{
				position += Vector3.left;
				//CreateCube(position);
			}
			for(int u = 0; u < increment; u++)
			{
				position += Vector3.forward;
				//CreateCube(position);
			}

			increment++;
		}
	}
}
