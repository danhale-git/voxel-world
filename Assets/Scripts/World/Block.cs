using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class Block
{
	public enum BlockType {GROUND, AIR};
	
	public BlockType type;
	public Vector3 position;
	public Chunk owner;
	
	//	seeThrough used for quad culling
	public bool seeThrough;

	public Block(BlockType _type, Vector3 _position, Chunk _owner)
	{
		type = _type;
		position = _position;
		owner = _owner;

		//	Set seeThrough
		switch(type)
		{
			case BlockType.AIR:
			seeThrough = true;
			break;
			case BlockType.GROUND:
			seeThrough = false;
			break;
		}
	}

	//	Generate meshes for exposed block faces
	public List<MeshFilter> GetFaces()
	{
		List<MeshFilter> meshFilters = new List<MeshFilter>();
		
		if(type == BlockType.AIR) { return meshFilters; }
		
		//	Iterate over all six faces
		for(int i = 0; i < 6; i++)
		{
			//	Create mesh if exposed
			if(FaceExposed( (BlockUtils.CubeFace)i ))
			{
				meshFilters.Add(DrawQuad( (BlockUtils.CubeFace)i ));
			}
		}

		return meshFilters;
	}

	//	TODO: 	There must be a better way to do this than
	//			creating a bunch of quad gameobjects only
	//			to destroy them in Chunk.MergeQuads()

	//	Create quad representing one side of a cube
	MeshFilter DrawQuad(BlockUtils.CubeFace face)
	{
		Mesh mesh = new Mesh();

		//	Get fixed cube attributes from static utility class
		mesh.vertices = BlockUtils.GetVertices(face);
		mesh.normals = BlockUtils.GetNormals(face);
		mesh.triangles = BlockUtils.GetTriangles(face);
		
		//	Good practice to recalculate bounds
		mesh.RecalculateBounds();

		// Create gameobject for adding mesh
		GameObject quad = new GameObject("Quad");
		quad.transform.position = position;
	    quad.transform.parent = owner.gameObject.transform;
		
		//	Add and return mes
     	MeshFilter meshFilter = (MeshFilter) quad.AddComponent(typeof(MeshFilter));
		meshFilter.mesh = mesh;
		
		return meshFilter;
	}

	//	Block face is on map edge or player can see through adjacent block
	bool FaceExposed(BlockUtils.CubeFace face)
	{	
		//	Direction of neighbour
		Vector3 faceDirection = BlockUtils.GetDirection(face);	
		//	Neighbour position
		Vector3 neighbour = faceDirection + position;
		
		Chunk neighbourOwner;

		//	Neighbour is outside this chunk
		if(neighbour.x < 0 || neighbour.x >= World.chunkSize || 
		   neighbour.y < 0 || neighbour.y >= World.chunkSize ||
		   neighbour.z < 0 || neighbour.z >= World.chunkSize)
		{
			//	Next chunk in direction of neighbour
			Vector3 nbrChunkPos = owner.position + (faceDirection * World.chunkSize);

			//	Neighbouring chunk does not exist (map edge)
			if(!World.chunks.TryGetValue(nbrChunkPos, out neighbourOwner))
			{
				return false;
			}			
			//	Convert local index to neighbouring chunk
			neighbour = BlockUtils.WrapBlockIndex(neighbour);
		}
		//	Neighbour is in this chunk		
		else
		{
			neighbourOwner = owner;
		}
		
		//	Check seeThrough in neighbour
		return neighbourOwner.blocks[(int)neighbour.x, (int)neighbour.y, (int)neighbour.z].seeThrough;
	}
}
