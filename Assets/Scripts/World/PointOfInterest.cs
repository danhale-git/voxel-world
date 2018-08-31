using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointOfInterest
{
	World world;

	List<Column> allColumns = new List<Column>();
	List<Column> exposedEdge = new List<Column>();

	public struct POIData
	{
		public bool isEdge;
		/*public POIData(bool isEdge)
		{
			this.isEdge = isEdge;
		}*/
	}

	public PointOfInterest(Column initialColumn, World world)
	{
		this.world = world;

		DiscoverCells(initialColumn);

		Debug.Log("POI created with "+allColumns.Count+" columns");
	}

	//	TODO: process initial column the same as the rest to keep everything coherent
	List<Column> DiscoverCells(Column initialColumn)
	{
		allColumns.Add(initialColumn);

		//	Columns currently being checked
		List<Column> columnsToCheck = new List<Column>();
		columnsToCheck.Add(initialColumn);

		int iterationCount = 0;
		//	Continue until no more chunks need to be checked (should never need 500)
		while(iterationCount < 500)	//	recursive
		{
			List<Column> newColumns = new List<Column>();

			//	Process current column list
			foreach(Column column in columnsToCheck)	//	check columns
			{
				POIData data = new POIData();

				Vector3[] neighbourPos = Util.HorizontalChunkNeighbours(column.position);

				//	Check adjacent columns
				for(int i = 0; i < neighbourPos.Length; i++)	//	adjacent
				{
					Column newColumn;
					if(world.GenerateColumnData(neighbourPos[i], out newColumn))
					{
						//	If adjacent column needed spawning and is eligible for structures
						if(newColumn.IsPOI) newColumns.Add(newColumn);
					}
					else
					{
						newColumn = World.columns[neighbourPos[i]];
					}

					if(!data.isEdge && !newColumn.IsPOI && !newColumn.biomeBoundary && i < 4)
					{
						data.isEdge = true;

						World.debug.OutlineChunk(new Vector3(column.position.x, 100, column.position.z), Color.green, sizeDivision: 2f);	//	//	//
					}
				}
			}
			if(newColumns.Count == 0) break;

			//	Check newly created columns with structures next
			columnsToCheck = newColumns;

			//	Store new columns
			allColumns.AddRange(newColumns);

			//	Safety, maybe remove this later
			iterationCount++;
			if(iterationCount > 498) Debug.Log("Too many structure processing iterations!\nAbandoned while loop early");
		}
		return allColumns;
	}

}
