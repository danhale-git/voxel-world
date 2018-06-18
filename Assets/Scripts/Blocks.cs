using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Blocks
{
	//	Enum indices correspond to fixed block attribute arrays below
	public enum Types {		AIR = 0,
							DIRT = 1,
							STONE = 2
							};

	//	Block type is see-through
	public static bool[] seeThrough = new bool[] {	true,		//	0	//	AIR
														false,		//	1	//	DIRT
														false		//	2	//	STONE
														};

	//	Block type color
	public static Color32[] colors = new Color32[]{	Color.white,					//	0	//	AIR
													new Color32(11, 110, 35, 255),	//	1	//	DIRT
													new Color32(200, 200, 200, 255)	//	2	//	STONE
													};	
								
}
