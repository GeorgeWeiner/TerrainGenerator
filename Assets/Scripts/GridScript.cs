using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainType
{
    public string name;
    public float height;
    public Color color;
}


[RequireComponent (typeof(MeshFilter), typeof(MeshRenderer))]
public class GridScript : MonoBehaviour
{
    [SerializeField] private TerrainType[] terrainTypes;
    
    [Header("Size, Scale & Curves")]
    [SerializeField] private int xSize;
    [SerializeField] private int ySize;
    [SerializeField] private float maxHeight = 1;
    [SerializeField] private float perlinScale;
    [SerializeField] private AnimationCurve animationCurve;
    
    [Header("Noise Octaves")]
    [Range(1, 10)]
    [SerializeField] private int numOctaves = 7;
    [Range(0, 1)]
    [SerializeField] private float persistence = .5f;
    [Range(0, 4)]
    [SerializeField] private float lacunarity = 2;
 
    
    [ContextMenu("Regenerate Terrain")]
    private void Start()
    {
        Generator();
    }

    private float[,] GenerateNoiseMap(int zGrid, int xGrid, float scale)
    {
        var initialScale = scale;
        var noiseMap = new float[zGrid, xGrid];
        
        //This adds random offsets for the noise, to generate a different terrain every time.
        var offsets = new Vector2[numOctaves]; 
        for (var i = 0; i < numOctaves; i++) {
            offsets[i] = new Vector2 (Random.Range(-1000, 1000), Random.Range(-1000, 1000));
        }
        
        for (var zIndex = 0; zIndex < zGrid; zIndex++)
        {
            for (var xIndex = 0; xIndex < xGrid; xIndex++)
            {
                scale = initialScale;
                var weight = 1f;
                
                //This is what creates different octaves of noise, for detail in the mesh.
                for (var octave = 0; octave < numOctaves; octave++)
                {
                    var position = offsets[octave] + new Vector2(zIndex / (float) ySize, xIndex / (float) xSize) * scale;
                    noiseMap[zIndex, xIndex] += Mathf.PerlinNoise(position.y, position.x) * weight;
                    
                    weight *= persistence;
                    scale *= lacunarity;
                }
            }
        }
        return noiseMap;
    }

    private TerrainType ChooseTerrainType(float height)
    {
        foreach (var terrainType in terrainTypes)
        {
            if (height < terrainType.height)
            {
                return terrainType;
            }
        }

        return terrainTypes[terrainTypes.Length - 1];
    }
    
    private Texture2D BuildTexture(float[,] heightMap)
    {
        var depth = heightMap.GetLength(0);
        var width = heightMap.GetLength(1);

        var colorMap = new Color[depth * width];

        for (var zIndex = 0; zIndex < depth; zIndex++)
        {
            for (var xIndex = 0; xIndex < width; xIndex++)
            {
                var colorIndex = zIndex * width + xIndex;
                var height = heightMap[zIndex, xIndex];
                var terrainType = ChooseTerrainType(height);

                //colorMap[colorIndex] = Color.Lerp(Color.black, Color.white, height);
                colorMap[colorIndex] = terrainType.color;
            }
        }

        var texture = new Texture2D(width, depth);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();

        return texture;
    }

    private void Generator()
    {
        var vertexBuffer = new Vector3[(xSize +1 ) * (ySize + 1) ];
        var indexBuffer = new int[xSize * ySize * 6];
        var uvBuffer = new Vector2[(xSize +1 ) * (ySize + 1) ];

        var heightMap = GenerateNoiseMap(ySize + 1, xSize + 1, perlinScale);
        var heightTexture = BuildTexture(heightMap);
 
        for (int i = 0, y = 0; y <= ySize; y++)
        {
            for (var x = 0; x <= xSize; x++,i++)
            {
                vertexBuffer[i] = new Vector3(x,  animationCurve.Evaluate(heightMap[x,y]) * maxHeight, y);
                uvBuffer[i] = new Vector2(y / (float)ySize, x / (float)xSize);
                Debug.Log(vertexBuffer[i]);
            }      
        }
 
        for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
        {
            for (var x = 0; x < xSize; x++, ti +=6, vi++)
            {
                indexBuffer[ti] = vi;
                indexBuffer[ti + 3] = indexBuffer[ti + 2] = vi + 1;
                indexBuffer[ti + 4] = indexBuffer[ti + 1] = vi + xSize + 1;
                indexBuffer[ti + 5] =  vi + xSize + 2;
            }
        }
 
        var mesh = new Mesh
        {
            vertices = vertexBuffer,
            triangles = indexBuffer,
            uv = uvBuffer
        };
        
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material.mainTexture = heightTexture;
    }
 
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
 
        for (var i = 0; i < GetComponent<MeshFilter>().mesh.vertexCount; i++)
            Gizmos.DrawSphere(GetComponent<MeshFilter>().mesh.vertices[i], 0.1f);
    }
}