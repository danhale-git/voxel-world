using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POILibrary
{
	public enum Tiles {NONE, WALL, PATH};

	public class POI
	{
		public int wallHeight = 6;

		public void GenerateMatrixes(LSystem lSystem, Zone zone)
		{

			//	Create a big square
			//	Choose two random points on random sides
			//	Generate rooms with corridors at chosen points
			//	Grow two smaller squares off chosen points

			if(lSystem.SquareInBounds(zone.bufferedBounds, zone.back, positionOnSide: 0.5f, minWidth:40, maxWidth:50, minLength:40, maxLength:50))
			{
				lSystem.ConnectedSquare(zone.bufferedBounds, 0, bestSide:true, minWidth:20, maxWidth:30, minLength:20, maxLength:30);
			}

			lSystem.GenerateRooms(lSystem.currentBounds[0]);
			lSystem.DrawRooms(zone.debugMatrix);
			lSystem.DrawRooms(zone.wallMatrix);
			lSystem.GenerateRooms(lSystem.currentBounds[1]);
			lSystem.DrawRooms(zone.debugMatrix);
			lSystem.DrawRooms(zone.wallMatrix);

			foreach(int[] bounds in lSystem.currentBounds)
			{
				//lSystem.DrawPoint(LSystem.BoundsCenter(bounds), zone.debugMatrix, 2);
				lSystem.DrawBoundsBorder(bounds, zone.debugMatrix, 1);
				lSystem.DrawBoundsBorder(bounds, zone.wallMatrix, 1);
			}

			

			//lSystem.DrawCorridors(zone.debugMatrix, 1);
			foreach(Int2 point in lSystem.originPoints)
			{
				//lSystem.DrawPoint(point, zone.debugMatrix, 3);
			}

			lSystem.DefineArea();

			lSystem.ApplyMaps(this);
		}


	}

}
