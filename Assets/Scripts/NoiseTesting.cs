using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

[ExecuteInEditMode]
public class NoiseTesting : MonoBehaviour
{
	//	Attach to sprite under canvas to test 2D perlin noise

	private Texture2D texture;
    private SpriteRenderer spriteRenderer;
	private RectTransform rectTrans;

	public enum Algorithm {Brownian, Perlin, Moutainous}

	public Vector2 offset = new Vector2(0, 0);
	
	public Algorithm algorithm;
	public int scrollSpeed;
	public int gridSize;

	public int size = 1024;
	public float frequency;

	[System.Serializable]
	public struct Highlight
	{
		public float min;
		public Color color;
	}

	public List<Highlight> highlights;

	[Header("Brownian only")]
	public int octaves;
	public float persistence;

	public float multiplyX;
	public float multiplyY;

	

	public float redMin;
	public float redMax;

    void Start()
	{
        spriteRenderer = GetComponent<SpriteRenderer>();
		rectTrans = GetComponent<RectTransform>();
		Noise();
    }

	public void Up()
	{
		offset = new Vector2(offset.x, offset.y + scrollSpeed);
		Noise();
	}
	public void Down()
	{
		offset = new Vector2(offset.x, offset.y - scrollSpeed);
		Noise();
	}
	public void Right()
	{
		offset = new Vector2(offset.x + scrollSpeed, offset.y);
		Noise();
	}
	public void Left()
	{
		offset = new Vector2(offset.x - scrollSpeed, offset.y);
		Noise();
	}


	public float GetPixelNoise(int x, int y)
	{
		switch(algorithm)
		{
			case Algorithm.Brownian:
				return NoiseUtils.BrownianMotion((x*frequency)*multiplyX, (y*frequency)*multiplyY, octaves, persistence);
			case Algorithm.Perlin:
				return Mathf.PerlinNoise((x*frequency)*multiplyX, (y*frequency)*multiplyY);
			case Algorithm.Moutainous:
				return NoiseUtils.BrownianMotion((x*0.002f), (y*0.002f)*2, 3, 0.3f);
			default:
				return 0;
		}
	}
    
	public void Noise()
	{
		texture = new Texture2D(size, size);
		Debug.Log("Generating noise");
		for(int x = 0; x < size; x++)
			for(int y = 0; y < size; y++)
			{
				bool highlighted = false;

				float noise = GetPixelNoise(x+(int)offset.x, y+(int)offset.y) ;

				int _x = x;
				int _y = y;
	
				Color noiseColor = new Color(noise, noise, noise, 1);
				
				if(x % gridSize == 0 || y % gridSize == 0)
					texture.SetPixel(_x, _y, new Color(0, 1, 0, 1) * noiseColor);
				else
				{
					for(int i = 0; i < highlights.Count; i++)
					{
						Highlight hl = highlights[i];
						float max;
						if(i == 0) max = 1;
						else max = highlights[i-1].min;

						if(noise > hl.min && noise < max)
						{							
							texture.SetPixel(_x, _y, hl.color * noiseColor);
							highlighted = true;
							break;
						}
					}
					if(!highlighted)
						texture.SetPixel(_x, _y, noiseColor);
				} 
				
			}
		
		texture.Apply();
		Sprite mySprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector3(0.5f, 0.5f));
		mySprite.name = "noiseImage";
		spriteRenderer.sprite = mySprite;
	}
  
	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Space)) Noise();
	}

	class ObjectBuilderScript : MonoBehaviour 
	{
		public GameObject obj;
		public Vector3 spawnPoint;

		
		public void BuildObject()
		{
			Instantiate(obj, spawnPoint, Quaternion.identity);
		}
	}
}