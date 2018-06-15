using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

	public Chunk currentChunk;
	public World world;

	//	First person controls
	int speed = 25;
	int sensitivity = 1;
	Rigidbody rigidBody;
	Camera mycam;

	//	Chunk checks
	float updateTimer;
	int chunkLayerMask;
	

	void Start ()
	{
		mycam = GetComponentInChildren<Camera>();
		rigidBody = GetComponent<Rigidbody>();

		updateTimer = Time.fixedTime;
		chunkLayerMask = LayerMask.GetMask("Chunk");
	}
	
	void Update ()
	{
		Movement();
		
		GetCurrentChunk();
	}
	
	//	Raycast down to find current chunk
	//	Generate more chunks if player has moved to a new chunk
	void GetCurrentChunk()
	{
		//	Only do this twice per second
		if(Time.fixedTime - updateTimer < 0.5f)
		{
			return;
		}
		updateTimer = Time.fixedTime;

		//	Raycast to chunk below player
        RaycastHit hit;
        if (Physics.Raycast(transform.position,
							transform.TransformDirection(Vector3.down),
							out hit, 1000f,
							chunkLayerMask))
        {
			Chunk chunk;			
			//	Check if chunk exists
            if(World.chunks.TryGetValue(hit.transform.position, out chunk))
			{
				//	Initialise currentChunk
				if(currentChunk == null){ currentChunk = chunk; }

				//	Check if player has moved to a different chunk
				else if(chunk.position != currentChunk.position)
				{
					currentChunk = chunk;

					//	Generate chunks around player location
					world.DrawSurroundingChunks(currentChunk.gameObject.transform.position);
				}
			}
			else
			{
				//	Something strange happened
				Debug.Log("chunk at "+hit.transform.position+" not found! ("+hit.transform.gameObject.name+")");
			}
        }
	}

	//	Simple flying first person movement
	//	Probably temporary
	void Movement()
	{
		if(Input.GetKey(KeyCode.W))	//	forward
		{
			transform.Translate((Vector3.forward * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.S))	//	back
		{
			transform.Translate((Vector3.back * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.A))	//	left
		{
			transform.Translate((Vector3.left * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.D))	//	right
		{
			transform.Translate((Vector3.right * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.LeftControl))	//	down
		{
			transform.Translate((Vector3.down * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.Space))	//	up
		{
			transform.Translate((Vector3.up * speed) * Time.deltaTime);
		}
		
		float horizontal = sensitivity * Input.GetAxis("Mouse X");
        float vertical = -(sensitivity * Input.GetAxis("Mouse Y"));
        transform.Rotate(0, horizontal, 0);
		mycam.gameObject.transform.Rotate(vertical, 0, 0);

		if(Input.GetButtonDown("Fire1"))
		{
			//	cast ray from cursor
			RaycastHit hit;
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			if (Physics.Raycast(ray, out hit))
			{
				//	get voxel position
				//Vector3 positionInCube = hit.point - (hit.normal * 0.5f);
				//Vector3 voxel = BlockUtils.RoundVector3(positionInCube);
				
				Vector3 hitBlock = hit.point - hit.normal/2.0f; 

   				int xBlock = (int) (Mathf.Round(hitBlock.x) - hit.collider.gameObject.transform.position.x);
   				int yBlock = (int) (Mathf.Round(hitBlock.y) - hit.collider.gameObject.transform.position.y);
   				int zBlock = (int) (Mathf.Round(hitBlock.z) - hit.collider.gameObject.transform.position.z);

				Block block = World.chunks[hit.transform.position].blocks[xBlock, yBlock, zBlock];

				block.type = Block.BlockType.AIR;
				block.seeThrough = true;

				List<Vector3> updates = new List<Vector3>();

				float xChunk = hit.transform.position.x;
				float yChunk = hit.transform.position.y;
				float zChunk = hit.transform.position.z;

				updates.Add(hit.transform.position);

				//update neighbours?
				if(xBlock == 0) 
					updates.Add((new Vector3(xChunk-World.chunkSize,	yChunk,					zChunk)));
				if(xBlock == World.chunkSize - 1) 
					updates.Add((new Vector3(xChunk+World.chunkSize,	yChunk,					zChunk)));
				if(yBlock == 0) 
					updates.Add((new Vector3(xChunk,					yChunk-World.chunkSize,	zChunk)));
				if(yBlock == World.chunkSize - 1) 
					updates.Add((new Vector3(xChunk,					yChunk+World.chunkSize,	zChunk)));
				if(zBlock == 0) 
					updates.Add((new Vector3(xChunk,					yChunk,					zChunk-World.chunkSize)));
				if(zBlock == World.chunkSize - 1) 
					updates.Add((new Vector3(xChunk,					yChunk,					zChunk+World.chunkSize)));

				Chunk chunk;
				if(World.chunks.TryGetValue(hit.transform.position, out chunk))
				{
					chunk.Redraw();
				}

				/*foreach(Vector3 chunkPos in updates)
				{
					Chunk chunk;
					if(World.chunks.TryGetValue(chunkPos, out chunk))
					{
						chunk.Redraw();
					}
				}*/
				
				
				
				
				
				
				/*Chunk chunk = World.chunks[hit.transform.position];
				Vector3 local = voxel - chunk.position;
				Block block = chunk.blocks[(int)local.x, (int)local.y, (int)local.z];*/
				
			}

		}
	}
}
