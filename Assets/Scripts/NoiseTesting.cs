using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[ExecuteInEditMode]
public class NoiseTesting : MonoBehaviour
{
	private Texture2D texture;
    private SpriteRenderer spriteRenderer;
	private RectTransform rectTrans;

	public enum Algorithm {Brownian, Perlin}

	
	public Algorithm algorithm;
	public int gridSize;

	public int size = 1024;
	public float frequency;

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
		//size = (int)rectTrans.rect.height;
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
				float noise = GetPixelNoise(x, y) ;
				
				int _x = x;// - x/2;
				int _y = y;// - y/2;
				
				texture.SetPixel(_x, _y, new Color(noise, noise, noise, 1));
				if(noise < redMax && noise > redMin) texture.SetPixel(_x, _y, Color.red);
				else if(x % gridSize == 0 || y % gridSize == 0) texture.SetPixel(_x, _y, Color.green);
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