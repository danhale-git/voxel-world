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
	public enum NoiseMode {Draw, Log1Noise, LogRandomNoise}

	public bool autoRefresh = false;
	public bool showGrid = true;
	public NoiseMode noiseMode;
	public Vector2 offset = new Vector2(0, 0);
	public int scrollSpeed;
	public int gridSize;
	public int size = 1024;

	#region FastNoise


	// Use this to access FastNoise functions
	public FastNoise fastNoise = new FastNoise();

	[Header("FASTNOISE")]
	public string noiseName = "Default Noise";

	public int seed = 1337;
	public float frequency = 0.01f;
	public FastNoise.Interp interp = FastNoise.Interp.Quintic;
	public FastNoise.NoiseType noiseType = FastNoise.NoiseType.Simplex;
	
	public int octaves = 3;
	public float lacunarity = 2.0f;
	public float gain = 0.5f;
	public FastNoise.FractalType fractalType = FastNoise.FractalType.FBM;
	
	public FastNoise.CellularDistanceFunction cellularDistanceFunction = FastNoise.CellularDistanceFunction.Euclidean;
	public FastNoise.CellularReturnType cellularReturnType = FastNoise.CellularReturnType.CellValue;
	public FastNoiseUnity cellularNoiseLookup = null;
	public int cellularDistanceIndex0 = 0;
	public int cellularDistanceIndex1 = 1;
	public float cellularJitter = 0.45f;

	public float gradientPerturbAmp = 1.0f;

#if UNITY_EDITOR
	public bool generalSettingsFold = true;
	public bool fractalSettingsFold = false;
	public bool cellularSettingsFold = false;
	public bool positionWarpSettingsFold = false;
#endif

	void Awake()
	{
		SaveSettings();
	}

	public void SaveSettings()
	{
		fastNoise.SetSeed(seed);
		fastNoise.SetFrequency(frequency);
		fastNoise.SetInterp(interp);
		fastNoise.SetNoiseType(noiseType);

		fastNoise.SetFractalOctaves(octaves);
		fastNoise.SetFractalLacunarity(lacunarity);
		fastNoise.SetFractalGain(gain);
		fastNoise.SetFractalType(fractalType);

		fastNoise.SetCellularDistanceFunction(cellularDistanceFunction);
		fastNoise.SetCellularReturnType(cellularReturnType);
		fastNoise.SetCellularJitter(cellularJitter);
		fastNoise.SetCellularDistance2Indicies(cellularDistanceIndex0, cellularDistanceIndex1);

		if (cellularNoiseLookup)
			fastNoise.SetCellularNoiseLookup(cellularNoiseLookup.fastNoise);

		fastNoise.SetGradientPerturbAmp(gradientPerturbAmp);
	}

	#endregion

	[System.Serializable]
	public struct Highlight
	{
		public float max;
		public Color color;
	}
	public List<Highlight> highlights;

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
		return fastNoise.GetNoise01(x, y);
		//return fastNoise.GetNoise(x, y);
	}

	public void LogNoise()
	{
        Debug.Log( GetPixelNoise((int)offset.x, (int)offset.y) );
	}
    public void LogRandomNoise()
	{
        Debug.Log( GetPixelNoise(Random.Range(0, 20000), Random.Range(0, 20000)) );
	}


	void OnValidate()
	{
		if(!autoRefresh) return;

		SaveSettings();
		Noise();
	}

	Color GetColor(float noise)
	{
		for(int i = 0; i < highlights.Count; i++)
		{
			if(noise < highlights[i].max)
			{
				return highlights[i].color;
			}
		}
		return Color.black;
	}
    
	public void Noise()
	{
		if(gameObject.activeInHierarchy == false) return;
		
		Debug.Log("Generated Noise");
		texture = new Texture2D(size, size);
		for(int x = 0; x < size; x++)
			for(int y = 0; y < size; y++)
			{
				//bool highlighted = true;

				float noise = GetPixelNoise(x+(int)offset.x, y+(int)offset.y) ;

				int _x = x;
				int _y = y;
	
				Color noiseColor = Color.white;// = new Color(noise, noise, noise, 1);
				
				if((x % gridSize == 0 || y % gridSize == 0) && showGrid)
					texture.SetPixel(_x, _y, new Color(0, 1, 0, 1) * noiseColor);
				else
				{
					/*for(int i = 0; i < highlights.Count; i++)
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
						texture.SetPixel(_x, _y, noiseColor);*/

					texture.SetPixel(_x, _y, GetColor(noise));
				} 
			}
		
		texture.Apply();
		Sprite mySprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector3(0.5f, 0.5f));
		mySprite.name = "noiseImage";
		spriteRenderer.sprite = mySprite;
	}
}