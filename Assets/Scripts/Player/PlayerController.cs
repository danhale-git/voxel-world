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

		if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); 
   			if ( Physics.Raycast (ray,out hit,10)) 
   			{
            
   				Vector3 hitBlock = hit.point - hit.normal/2.0f; 

   				int x = (int) (Mathf.Round(hitBlock.x) - hit.collider.gameObject.transform.position.x);
   				int y = (int) (Mathf.Round(hitBlock.y) - hit.collider.gameObject.transform.position.y);
   				int z = (int) (Mathf.Round(hitBlock.z) - hit.collider.gameObject.transform.position.z);

				Chunk hitc;

				if(World.chunks.TryGetValue(hit.collider.gameObject.transform.position, out hitc))
   				{
					Block block = hitc.blocks[x,y,z];
					block.type = Block.BlockType.AIR;
					block.seeThrough = true;

	   				List<Vector3> updates = new List<Vector3>();
	   				float thisChunkx = hitc.position.x;
	   				float thisChunky = hitc.position.y;
	   				float thisChunkz = hitc.position.z;

	   				updates.Add(hit.collider.gameObject.transform.position);

	   				//update neighbours?
	   				if(x == 0) 
	   					updates.Add(new Vector3(thisChunkx-World.chunkSize,thisChunky,thisChunkz));
					if(x == World.chunkSize - 1) 
						updates.Add(new Vector3(thisChunkx+World.chunkSize,thisChunky,thisChunkz));
					if(y == 0) 
						updates.Add(new Vector3(thisChunkx,thisChunky-World.chunkSize,thisChunkz));
					if(y == World.chunkSize - 1) 
						updates.Add(new Vector3(thisChunkx,thisChunky+World.chunkSize,thisChunkz));
					if(z == 0) 
						updates.Add(new Vector3(thisChunkx,thisChunky,thisChunkz-World.chunkSize));
					if(z == World.chunkSize - 1) 
						updates.Add(new Vector3(thisChunkx,thisChunky,thisChunkz+World.chunkSize));

		   			foreach(Vector3 cname in updates)
		   			{
		   				Chunk c;
						if(World.chunks.TryGetValue(cname, out c))
						{
							c.Redraw();
				   		}
				   	}
				}
		   	}
   		}
		
	}
}
