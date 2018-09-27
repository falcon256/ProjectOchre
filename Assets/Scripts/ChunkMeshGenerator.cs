using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMeshGenerator {

	public volatile static int Threads = 0;

	private Chunk chunk = null;
	private Mesh mesh = null;
    private Mesh waterMesh = null;
    private byte[,,] data = null;
	private byte[,,] shapeData = null;
    private FlowPoint[,,] fluidData = null;
	private List<Vector3> newVertices = null;
	private List<Vector2> newUV = null;
	private List<int> newTriangles = null;
    private List<Vector3> newWaterVertices = null;
    private List<Vector2> newWaterUV = null;
    private List<int> newWaterTriangles = null;
    private int currentIndex = 0;
	public volatile bool isStarted = false;
	public volatile bool isDone = false;

	private Vector3[] outputVertices = null;
	private Vector2[] outputUVs = null;
	private int[] outputTriangles = null;
    private Vector3[] outputWaterVertices = null;
    private Vector2[] outputWaterUVs = null;
    private int[] outputWaterTriangles = null;

    public Vector3[] getVertices()
	{
		return outputVertices;
	}
	public Vector2[] getUVs()
	{
		return outputUVs;
	}
	public int[] getTriangles()
	{
		return outputTriangles;
	}

    public Vector3[] getWaterVertices()
    {
        return outputWaterVertices;
    }
    public Vector2[] getWaterUVs()
    {
        return outputWaterUVs;
    }
    public int[] getWaterTriangles()
    {
        return outputWaterTriangles;
    }

    public ChunkMeshGenerator(Chunk c)
	{
        try
        {
            if (c == null)
            {
			    Debug.LogError ("Null chunk passed to chunk mesh generator");
			    return;
		    }
		    chunk = c;
		    newVertices = new List<Vector3>();
		    newUV = new List<Vector2>();
		    newTriangles = new List<int>();
            newWaterVertices = new List<Vector3>();
            newWaterUV = new List<Vector2>();
            newWaterTriangles = new List<int>();
        }
        catch (System.Exception e)
        {
            TerrainManager.getSingleton().logError(e.Message + e.StackTrace);
        }
    }

	public void Generate()
	{
        try
        {

            Threads++;
            isStarted = true;
            mesh = chunk.getChunkMesh();
            if (mesh == null)
            {
                Debug.LogError("Terrain mesh generator was given a null reference by a chunk... Odd...");
                mesh = new Mesh();
                chunk.setChunkMesh(mesh);
            }
            data = chunk.getVoxelData();
            shapeData = chunk.getVoxelShapeData();
            iterateOverGrid();
            newVertices.TrimExcess();
            newUV.TrimExcess();
            newTriangles.TrimExcess();
            outputVertices = newVertices.ToArray();
            outputUVs = newUV.ToArray();
            outputTriangles = newTriangles.ToArray();
            generateWater();
            isDone = true;
            Threads--;
            
        }
        catch (System.Exception e)
        {
            TerrainManager.getSingleton().logError(e.Message + e.StackTrace);
        }
	}

    public void fluidGenerate()
    {
        try
        {

            Threads++;
            isStarted = true;
            data = chunk.getVoxelData();
            shapeData = chunk.getVoxelShapeData();
            generateWater();
            isDone = true;
            Threads--;

        }
        catch (System.Exception e)
        {
            TerrainManager.getSingleton().logError(e.Message + e.StackTrace);
        }
    }

    public void generateWater()
    {
        try
        {

            //Threads++;
            //isStarted = true;
            currentIndex = 0;
            waterMesh = chunk.getChunkWaterMesh();
            if (waterMesh == null)
            {
                Debug.LogError("Terrain mesh generator was given a null reference by a chunk... Odd...");
                waterMesh = new Mesh();
                chunk.setChunkWaterMesh(waterMesh);
            }
            fluidData = chunk.getFlowVolumeData();
            iterateOverGridWater();
            newWaterVertices.TrimExcess();
            newWaterUV.TrimExcess();
            newWaterTriangles.TrimExcess();
            outputWaterVertices = newWaterVertices.ToArray();
            outputWaterUVs = newWaterUV.ToArray();
            outputWaterTriangles = newWaterTriangles.ToArray();
            //isDone = true;
            //Threads--;

        }
        catch (System.Exception e)
        {
            TerrainManager.getSingleton().logError(e.Message + e.StackTrace);
        }
    }

	private void iterateOverGrid()
	{
		for (int y = 0; y < GlobalConfig.Config_ChunkVerticalDimensionality; y++) {
			for (int z = 1; z < GlobalConfig.Config_ChunkDimensionality+1; z++) {
				for (int x = 1; x < GlobalConfig.Config_ChunkDimensionality+1; x++) {
					calculateBlockCheckZero (x, y, z, GlobalConfig.Config_ChunkDimensionality+1, GlobalConfig.Config_ChunkVerticalDimensionality);                
				}
			}
		}
        if (currentIndex > 65564)
        {
            TerrainManager.getSingleton().logError("Max vertices reached in mesh generation!");
        }
    }

    private void iterateOverGridWater()
    {
        for (int y = 0; y < GlobalConfig.Config_ChunkVerticalDimensionality; y++)
        {
            for (int z = 1; z < GlobalConfig.Config_ChunkDimensionality + 1; z++)
            {
                for (int x = 1; x < GlobalConfig.Config_ChunkDimensionality + 1; x++)
                {
					mesh_water_genSurface(x, y, z, GlobalConfig.Config_ChunkDimensionality + 1, GlobalConfig.Config_ChunkVerticalDimensionality);
                }
            }
        }
        if (currentIndex > 65564)
        {
            TerrainManager.getSingleton().logError("Max vertices reached in mesh generation!");
        }
    }

    private void indexIntoAtlas(int index)
	{
		//index = index % 16;
		float xoff = (((index - 1) % 4) * 0.25f);
		float yoff = (((index - 1) / 4) * 0.25f);
		newUV.Add (new Vector2 (xoff, yoff));
		newUV.Add (new Vector2 (xoff+0.25f, yoff));
		newUV.Add (new Vector2 (xoff+0.25f, yoff+0.25f));
		newUV.Add (new Vector2 (xoff, yoff+0.25f));
	}

	private void indexIntoAtlasTriangle(int index)
	{
		//index = index % 16;
		float xoff = (((index - 1) % 4) * 0.25f);
		float yoff = (((index - 1) / 4) * 0.25f);
		newUV.Add (new Vector2 (xoff, yoff));
		newUV.Add (new Vector2 (xoff+0.25f, yoff));
		newUV.Add (new Vector2 (xoff+0.25f, yoff+0.25f));
	}

	private void calculateBlockCheckZero(int x, int y, int z, int max,int vmax)
    {
        if (data[x, y, z] != 0)
			calculateBlock(x, y, z, max, vmax);
    }


	private void calculateBlock(int x, int y, int z, int max,int vmax)
	{
        byte xpos = 0;
        byte xposShape = 0;
        byte ypos = 0;
        byte yposShape = 0;
        byte zpos = 0;
        byte zposShape = 0;
        byte xneg = 0;
        byte xnegShape = 0;
        byte yneg = 0;
        byte ynegShape = 0;
        byte zneg = 0;
        byte znegShape = 0;




        xpos = data[x + 1, y, z];
        xneg = data[x - 1, y, z];
        zpos = data[x, y, z + 1];
        zneg = data[x, y, z - 1];


        if (y == vmax - 1)
            ypos = (byte)(0);
        else
            ypos = data[x, y + 1, z];

        if (y == 0)
            yneg = (byte)(1);
        else
            yneg = data[x, y - 1, z];


        xposShape = shapeData[x + 1, y, z];
        xnegShape = shapeData[x - 1, y, z];
        zposShape = shapeData[x, y, z + 1];
        znegShape = shapeData[x, y, z - 1];

        if (y == vmax - 1)
            yposShape = 0;
        else
            yposShape = shapeData[x, y + 1, z];
      

        if (y == 0 )
            ynegShape = 1;
        else
            ynegShape = shapeData[x, y - 1, z];
       

         
        
            
        

        if (shapeData[x, y, z]==1) 
            mesh_genCube(x, y, z, max, xpos, xposShape, ypos, yposShape, zpos, zposShape, xneg, xnegShape, yneg, ynegShape, zneg, znegShape);
        
		if (shapeData[x, y, z]==2)
            mesh_genxposSlope(x, y, z, max, xpos, xposShape, ypos, yposShape, zpos, zposShape, xneg, xnegShape, yneg, ynegShape, zneg, znegShape);
        if (shapeData[x, y, z] == 3)
            mesh_genxnegSlope(x, y, z, max, xpos, xposShape, ypos, yposShape, zpos, zposShape, xneg, xnegShape, yneg, ynegShape, zneg, znegShape);
        if (shapeData[x, y, z] == 4)
            mesh_genzposSlope(x, y, z, max, xpos, xposShape, ypos, yposShape, zpos, zposShape, xneg, xnegShape, yneg, ynegShape, zneg, znegShape);
        if (shapeData[x, y, z] == 5)
            mesh_genznegSlope(x, y, z, max, xpos, xposShape, ypos, yposShape, zpos, zposShape, xneg, xnegShape, yneg, ynegShape, zneg, znegShape);
        if (shapeData[x, y, z] == 6)
            mesh_genxpzpOuterCornerCube(x, y, z, max, xpos, xposShape, ypos, yposShape, zpos, zposShape, xneg, xnegShape, yneg, ynegShape, zneg, znegShape);  
        if (shapeData[x, y, z] == 7)
            mesh_genxnzpOuterCornerCube(x, y, z, max, xpos, xposShape, ypos, yposShape, zpos, zposShape, xneg, xnegShape, yneg, ynegShape, zneg, znegShape);        
        if (shapeData[x, y, z] == 8)
            mesh_genxnznOuterCornerCube(x, y, z, max, xpos, xposShape, ypos, yposShape, zpos, zposShape, xneg, xnegShape, yneg, ynegShape, zneg, znegShape);        
        if (shapeData[x, y, z] == 9)
            mesh_genxpznOuterCornerCube(x, y, z, max, xpos, xposShape, ypos, yposShape, zpos, zposShape, xneg, xnegShape, yneg, ynegShape, zneg, znegShape);        
        if (shapeData[x, y, z] == 10)
            mesh_genxpzpInnerCornerCube(x, y, z, max, xpos, xposShape, ypos, yposShape, zpos, zposShape, xneg, xnegShape, yneg, ynegShape, zneg, znegShape);
        if (shapeData[x, y, z] == 11)
            mesh_genxnzpInnerCornerCube(x, y, z, max, xpos, xposShape, ypos, yposShape, zpos, zposShape, xneg, xnegShape, yneg, ynegShape, zneg, znegShape);
        if (shapeData[x, y, z] == 12)
            mesh_genxnznInnerCornerCube(x, y, z, max, xpos, xposShape, ypos, yposShape, zpos, zposShape, xneg, xnegShape, yneg, ynegShape, zneg, znegShape);
        if (shapeData[x, y, z] == 13)
            mesh_genxpznInnerCornerCube(x, y, z, max, xpos, xposShape, ypos, yposShape, zpos, zposShape, xneg, xnegShape, yneg, ynegShape, zneg, znegShape);

    }

    public void mesh_water_genSurface(int x, int y, int z, int max, int vmax)
    {
		float min = 0.1f;
		if((fluidData[x,y,z].saturation > min&&y<vmax-2&& fluidData[x, y + 1, z].saturation < min)||(fluidData[x, y, z].saturation > min && y == vmax-1))
		//test
        //if((waterData[x,y,z]>0.1f&&y<vmax-2)||(waterData[x, y, z] > 0.1f && y == vmax-1))
        {
            newWaterVertices.Add(new Vector3(x + 0, y + fluidData[x, y, z].saturation, z + 0));

            if(x<max-1)
			if(y<=vmax-2&& fluidData[x + 1, y + 1, z].saturation > min)
                    newWaterVertices.Add(new Vector3(x + 1, y + fluidData[x+1, y+1, z].saturation + 1.0f, z + 0));
			else if (fluidData[x + 1, y, z].saturation > min)
                    newWaterVertices.Add(new Vector3(x + 1, y + fluidData[x + 1, y, z].saturation, z + 0));
			else if (y > 0 && fluidData[x + 1, y - 1, z].saturation > min)
                    newWaterVertices.Add(new Vector3(x + 1, y + fluidData[x + 1, y - 1, z].saturation - 1.0f, z + 0));
                else
                    newWaterVertices.Add(new Vector3(x + 1, y, z + 0));
            else
                newWaterVertices.Add(new Vector3(x + 1, y + fluidData[x, y, z].saturation, z + 0));

            if (x < max - 1 && z < max - 1)
			if (y <= vmax - 2 && fluidData[x + 1, y + 1, z + 1].saturation > min)
                    newWaterVertices.Add(new Vector3(x + 1, y + fluidData[x + 1, y + 1, z + 1].saturation + 1.0f, z + 1));
			else if (fluidData[x + 1, y, z+1].saturation > min)
                    newWaterVertices.Add(new Vector3(x + 1, y + fluidData[x + 1, y, z+1].saturation, z + 1));
			else if (y > 0 && fluidData[x + 1, y - 1, z+1].saturation > min)
                    newWaterVertices.Add(new Vector3(x + 1, y + fluidData[x + 1, y - 1, z+1].saturation - 1.0f, z + 1));
                else
                    newWaterVertices.Add(new Vector3(x + 1, y, z + 1));
            else
                newWaterVertices.Add(new Vector3(x + 1, y + fluidData[x, y, z].saturation, z + 1));

            if (z < max - 1)
			if (y <= vmax - 2 && fluidData[x, y + 1, z + 1].saturation > min)
                    newWaterVertices.Add(new Vector3(x + 0, y + fluidData[x, y + 1, z + 1].saturation + 1.0f, z + 1));
			else if (fluidData[x + 0, y, z + 1].saturation > min)
                    newWaterVertices.Add(new Vector3(x + 0, y + fluidData[x + 0, y, z + 1].saturation, z + 1));
			else if (y > 0 && fluidData[x + 0, y - 1, z].saturation > min)
                    newWaterVertices.Add(new Vector3(x + 0, y + fluidData[x + 0, y - 1, z + 1].saturation - 1.0f, z + 1));
                else
                    newWaterVertices.Add(new Vector3(x + 0, y, z + 1));
            else
                newWaterVertices.Add(new Vector3(x + 0, y + fluidData[x, y, z].saturation, z + 1));

            newWaterUV.Add(new Vector2(0, 0));
            newWaterUV.Add(new Vector2(1.0f, 0));
            newWaterUV.Add(new Vector2(1.0f, 1.0f));
            newWaterUV.Add(new Vector2(0, 1.0f));
            newWaterTriangles.Add(currentIndex + 0);
            newWaterTriangles.Add(currentIndex + 2);
            newWaterTriangles.Add(currentIndex + 1);
            newWaterTriangles.Add(currentIndex + 0);
            newWaterTriangles.Add(currentIndex + 3);
            newWaterTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }
    }

    public void mesh_genCube(int x, int y, int z, int max,
    byte xpos,
    byte xposShape,
    byte ypos,
    byte yposShape,
    byte zpos,
    byte zposShape,
    byte xneg,
    byte xnegShape,
    byte yneg,
    byte ynegShape,
    byte zneg,
    byte znegShape)
    {
        if (ypos == 0 || yposShape > 5)
        {
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (yneg == 0 || ynegShape > 1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (xpos == 0 || xposShape > 2)
        {
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (xneg == 0 || (xnegShape > 1 && xnegShape != 3))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (zpos == 0 || (zposShape > 1 && zposShape !=4))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (zneg == 0 || (znegShape > 1 && znegShape != 5))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }
    }

    public void mesh_genxposSlope(int x, int y, int z, int max,
    byte xpos,
    byte xposShape,
    byte ypos,
    byte yposShape,
    byte zpos,
    byte zposShape,
    byte xneg,
    byte xnegShape,
    byte yneg,
    byte ynegShape,
    byte zneg,
    byte znegShape)
    {
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        

        if (yneg==0||ynegShape!=1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (xneg==0||xnegShape!=1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (zpos==0||(zposShape != 1&& zposShape != 2))
        {
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            currentIndex += 3;
        }

        if (zneg==0||(znegShape != 1 && znegShape != 2))
        {
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 3;
        }
    }


    public void mesh_genxnegSlope(int x, int y, int z, int max,
    byte xpos,
    byte xposShape,
    byte ypos,
    byte yposShape,
    byte zpos,
    byte zposShape,
    byte xneg,
    byte xnegShape,
    byte yneg,
    byte ynegShape,
    byte zneg,
    byte znegShape)
    {
        newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
        newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
        newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
        newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
        indexIntoAtlas(data[x, y, z]);
        newTriangles.Add(currentIndex + 0);
        newTriangles.Add(currentIndex + 2);
        newTriangles.Add(currentIndex + 1);
        newTriangles.Add(currentIndex + 0);
        newTriangles.Add(currentIndex + 3);
        newTriangles.Add(currentIndex + 2);
        currentIndex += 4;


        if (yneg == 0 || ynegShape != 1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (xpos == 0 || xposShape != 1)
        {
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (zpos == 0 || (zposShape != 1 && zposShape != 3))
        {
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            currentIndex += 3;
        }

        if (zneg == 0 || (znegShape != 1 && znegShape != 3))
        {
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 3;
        }
    }

    public void mesh_genzposSlope(int x, int y, int z, int max,
    byte xpos,
    byte xposShape,
    byte ypos,
    byte yposShape,
    byte zpos,
    byte zposShape,
    byte xneg,
    byte xnegShape,
    byte yneg,
    byte ynegShape,
    byte zneg,
    byte znegShape)
    {
        newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
        newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
        newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
        newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
        indexIntoAtlas(data[x, y, z]);
        newTriangles.Add(currentIndex + 0);
        newTriangles.Add(currentIndex + 1);
        newTriangles.Add(currentIndex + 2);
        newTriangles.Add(currentIndex + 0);
        newTriangles.Add(currentIndex + 2);
        newTriangles.Add(currentIndex + 3);
        currentIndex += 4;


        if (yneg == 0 || ynegShape != 1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (zneg == 0 || znegShape != 1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (xpos == 0 || (xposShape != 1 && xposShape != 4))
        {
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 3;
        }

        if (xneg == 0 || (xnegShape != 1 && xnegShape != 4))
        {
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            currentIndex += 3;
        }
    }

    public void mesh_genznegSlope(int x, int y, int z, int max,
    byte xpos,
    byte xposShape,
    byte ypos,
    byte yposShape,
    byte zpos,
    byte zposShape,
    byte xneg,
    byte xnegShape,
    byte yneg,
    byte ynegShape,
    byte zneg,
    byte znegShape)
    {
        newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
        newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
        newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
        newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
        indexIntoAtlas(data[x, y, z]);
        newTriangles.Add(currentIndex + 0);
        newTriangles.Add(currentIndex + 1);
        newTriangles.Add(currentIndex + 2);
        newTriangles.Add(currentIndex + 0);
        newTriangles.Add(currentIndex + 2);
        newTriangles.Add(currentIndex + 3);
        currentIndex += 4;


        if (yneg == 0 || ynegShape != 1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (zpos == 0 || zposShape != 1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (xpos == 0 || (xposShape != 1 && xposShape != 4))
        {
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            currentIndex += 3;
        }

        if (xneg == 0 || (xnegShape != 1 && xnegShape != 4))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 3;
        }
    }


    public void mesh_genxpzpOuterCornerCube(int x, int y, int z, int max,
    byte xpos,
    byte xposShape,
    byte ypos,
    byte yposShape,
    byte zpos,
    byte zposShape,
    byte xneg,
    byte xnegShape,
    byte yneg,
    byte ynegShape,
    byte zneg,
    byte znegShape)
    {
        if (ypos == 0 || yposShape > 5)
        {
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (yneg == 0 || ynegShape > 1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (xneg == 0 || (xnegShape != 1 && xnegShape != 4))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 3;
        }

        if (zneg == 0 || (znegShape != 1 && znegShape != 3))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            currentIndex += 3;
        }
    }

    public void mesh_genxnzpOuterCornerCube(int x, int y, int z, int max,
    byte xpos,
    byte xposShape,
    byte ypos,
    byte yposShape,
    byte zpos,
    byte zposShape,
    byte xneg,
    byte xnegShape,
    byte yneg,
    byte ynegShape,
    byte zneg,
    byte znegShape)
    {
        if (ypos == 0 || yposShape > 5)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (yneg == 0 || ynegShape > 1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (xpos == 0 || (xposShape != 1 && xposShape != 4))
        {
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            currentIndex += 3;
        }

        if (zneg == 0 || (znegShape != 1 && znegShape != 3))
        {
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 3;
        }

    }


    public void mesh_genxnznOuterCornerCube(int x, int y, int z, int max,
    byte xpos,
    byte xposShape,
    byte ypos,
    byte yposShape,
    byte zpos,
    byte zposShape,
    byte xneg,
    byte xnegShape,
    byte yneg,
    byte ynegShape,
    byte zneg,
    byte znegShape)
    {
        
        if (ypos == 0 || yposShape > 13)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }
        
        if (yneg == 0 || ynegShape > 1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }
        
        if (xpos == 0 || (xposShape != 1 && xposShape != 4))
        {
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            currentIndex += 3;
        }
        
        if (zpos == 0 || (zposShape != 1 && zposShape != 3))
        {
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            currentIndex += 3;
        }
        
        
    }

    public void mesh_genxpznOuterCornerCube(int x, int y, int z, int max,
    byte xpos,
    byte xposShape,
    byte ypos,
    byte yposShape,
    byte zpos,
    byte zposShape,
    byte xneg,
    byte xnegShape,
    byte yneg,
    byte ynegShape,
    byte zneg,
    byte znegShape)
    {
        if (ypos == 0 || yposShape > 5)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (yneg == 0 || ynegShape > 1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        
        if (xneg == 0 || (xnegShape != 1 && xnegShape != 4))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 3;
        }

        if (zpos == 0 || (zposShape != 1 && zposShape != 3))
        {
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            currentIndex += 3;
        }
    }

    public void mesh_genxpzpInnerCornerCube(int x, int y, int z, int max,
    byte xpos,
    byte xposShape,
    byte ypos,
    byte yposShape,
    byte zpos,
    byte zposShape,
    byte xneg,
    byte xnegShape,
    byte yneg,
    byte ynegShape,
    byte zneg,
    byte znegShape)
    {
        if (ypos == 0 || yposShape > 5)
        {
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (yneg == 0 || ynegShape > 1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (xneg == 0 || (xnegShape > 1 && xnegShape != 3))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (zneg == 0 || (znegShape > 1 && znegShape != 5))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (xpos == 0 || (xposShape != 1 && xposShape != 4))
        {
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            currentIndex += 3;
        }

        if (zpos == 0 || (zposShape != 1 && zposShape != 3))
        {
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            currentIndex += 3;
        }
    }


    public void mesh_genxnzpInnerCornerCube(int x, int y, int z, int max,
    byte xpos,
    byte xposShape,
    byte ypos,
    byte yposShape,
    byte zpos,
    byte zposShape,
    byte xneg,
    byte xnegShape,
    byte yneg,
    byte ynegShape,
    byte zneg,
    byte znegShape)
    {
        if (ypos == 0 || yposShape > 5)
        {
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (yneg == 0 || ynegShape > 1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }
        
        if (xpos == 0 || xposShape > 2)
        {
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (zneg == 0 || (znegShape > 1 && znegShape != 5))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (xneg == 0 || (xnegShape != 1 && xnegShape != 4))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 3;
        }

        if (zpos == 0 || (zposShape != 1 && zposShape != 3))
        {
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            currentIndex += 3;
        }

    }


    public void mesh_genxnznInnerCornerCube(int x, int y, int z, int max,
    byte xpos,
    byte xposShape,
    byte ypos,
    byte yposShape,
    byte zpos,
    byte zposShape,
    byte xneg,
    byte xnegShape,
    byte yneg,
    byte ynegShape,
    byte zneg,
    byte znegShape)
    {
        if (ypos == 0 || yposShape > 5)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (yneg == 0 || ynegShape > 1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }
        
        if (xpos == 0 || xposShape > 2)
        {
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }

        if (zpos == 0 || (zposShape > 1 && zposShape != 4))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (xneg == 0 || (xnegShape != 1 && xnegShape != 4))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 3;
        }

        if (zneg == 0 || (znegShape != 1 && znegShape != 3))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            currentIndex += 3;
        }


    }


    public void mesh_genxpznInnerCornerCube(int x, int y, int z, int max,
    byte xpos,
    byte xposShape,
    byte ypos,
    byte yposShape,
    byte zpos,
    byte zposShape,
    byte xneg,
    byte xnegShape,
    byte yneg,
    byte ynegShape,
    byte zneg,
    byte znegShape)
    {
        if (ypos == 0 || yposShape > 5)
        {
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (yneg == 0 || ynegShape > 1)
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 3);
            currentIndex += 4;
        }
  
        if (xneg == 0 || (xnegShape > 1 && xnegShape != 3))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (zpos == 0 || (zposShape > 1 && zposShape != 4))
        {
            newVertices.Add(new Vector3(x + 0, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 0, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            indexIntoAtlas(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 3);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 4;
        }

        if (xpos == 0 || (xposShape != 1 && xposShape != 4))
        {
            newVertices.Add(new Vector3(x + 1, y + 0, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 1, z + 1));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 2);
            newTriangles.Add(currentIndex + 1);
            currentIndex += 3;
        }

        if (zneg == 0 || (znegShape != 1 && znegShape != 3))
        {
            newVertices.Add(new Vector3(x + 0, y + 1, z + 0));
            newVertices.Add(new Vector3(x + 1, y + 0, z + 0));
            newVertices.Add(new Vector3(x + 0, y + 0, z + 0));
            indexIntoAtlasTriangle(data[x, y, z]);
            newTriangles.Add(currentIndex + 0);
            newTriangles.Add(currentIndex + 1);
            newTriangles.Add(currentIndex + 2);
            currentIndex += 3;
        }

    }



}
