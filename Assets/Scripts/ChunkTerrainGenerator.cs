using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

public class ChunkTerrainGenerator{

	public volatile static int Threads = 0;
	Chunk chunk = null;
	public volatile bool isStarted = false;
	public volatile bool isDone = false;
	private double solidTerrainThreshold=1.0;
	public ChunkTerrainGenerator(Chunk c)
	{
		if (c == null) {
			Debug.LogError ("Null chunk passed to chunk terrain generator");
			return;
		}
		chunk = c;
	}

    //test
    /* //TODO low priority, update for new chunk data dimensions
    public void blockTestGenerate()
    {
        try
        {
            byte ticker = 0;
            byte[,,] voxels = chunk.getVoxelData();
            byte[,,] voxelShapes = chunk.getVoxelShapeData();
            for (int y = 0; y < GlobalConfig.Config_ChunkVerticalDimensionality; y++)
            {
                for (int z = 0; z < GlobalConfig.Config_ChunkDimensionality; z++)
                {
                    for (int x = 0; x < GlobalConfig.Config_ChunkDimensionality; x++)
                    {
                        if (ticker++ % 4 == 0 && x % 4 == 0 && y % 4 == 0 && z % 4 == 0)
                        {
                            voxels[x, y, z] = 1;
                            voxelShapes[x, y, z] = (byte)(ticker / 4);
                        }
                        else
                        {
                            voxels[x, y, z] = 0;
                            voxelShapes[x, y, z] = 0;
                        }
                    }
                }
            }
            chunk.setVoxelData(voxels);
            chunk.setEmpty(false);
            chunk.setFull(false);
            chunk.setVoxelShapeData(voxelShapes);
            chunk.completeGeneration();
        }catch(System.Exception e)
        {
            TerrainManager.getSingleton().logError(e.Message);
        }
    }
    */
    
    public void Generate()
    {
		UnityEngine.Profiling.Profiler.BeginSample ("Chunk Generation Thread");
        try
        {
            Threads++;
            isStarted = true;
			//TerrainManager.getSingleton().logError(""+chunk.gridPosition+" stage 0 initialized");
            //blockTestGenerate();
            byte[,,] voxels = chunk.getVoxelData();
          
                for (int y = 0; y < GlobalConfig.Config_ChunkVerticalDimensionality; y++)
                {
                    for (int z = 0; z < GlobalConfig.Config_ChunkDimensionality + 2; z++)
                    {
                        for (int x = 0; x < GlobalConfig.Config_ChunkDimensionality + 2; x++)
                        {
                            voxels[x, y, z] = fillGridPosition(x, y, z);
                            
                        }
                    }
                }
            
            chunk.setVoxelData(voxels);
			//TerrainManager.getSingleton().logError(""+chunk.gridPosition+" stage 1 generated");

			//TerrainManager.getSingleton().logError(""+chunk.gridPosition+" stage 2 started");
         
            byte[,,] voxelShapes = chunk.getVoxelShapeData();


			for (int y = GlobalConfig.Config_ChunkVerticalDimensionality-1; y >=0 ; y--)
                {
                    for (int z = 0; z < GlobalConfig.Config_ChunkDimensionality + 2; z++)
                    {
                        for (int x = 0; x < GlobalConfig.Config_ChunkDimensionality + 2; x++)
                        {
                            voxelShapes[x, y, z] = fillGridShape(voxels, x, y, z, GlobalConfig.Config_ChunkDimensionality + 1);
                    }
                    }
                }

            FlowPoint[,,] fluidData = chunk.getFlowVolumeData();//transfer here by reference for modification

            for (int y = 0; y < GlobalConfig.Config_ChunkVerticalDimensionality; y++)
            {
                for (int z = 0; z < GlobalConfig.Config_ChunkDimensionality + 2; z++)
                {
                    for (int x = 0; x < GlobalConfig.Config_ChunkDimensionality + 2; x++)
                    {
						if (y > 1 && y < 80)
                            fluidData[x, y, z].saturation = 1.0f;

						//if(x>0&&z>0&&y>0&&x<GlobalConfig.Config_ChunkDimensionality&&z<GlobalConfig.Config_ChunkDimensionality)
						//{
						//	if(voxelShapes[x-1,y,z-1]>0)
						//		waterData[x, y, z] = 1.0f;
						//}
                    }
                }
            }
            //chunk.setWaterVolumeData(waterData);//redundant

            chunk.setVoxelShapeData(voxelShapes);
            chunk.completeGeneration();
            
            isDone = true;
            Threads--;
			UnityEngine.Profiling.Profiler.EndSample ();
        }
        catch (System.Exception e)
        {
            TerrainManager.getSingleton().logError(e.Message + e.StackTrace);
        }
    }

	private double getPerlin(int x, int y, int z)
	{
		Vector3 worldPos = chunk.localGridPosToWorldPos (new Vector3Int (x, y, z));
		float heightDiv = Mathf.Max(0.001f,worldPos.y/100.0f);//TODO Magic Number
		double offset = ((double)GlobalConfig.Config_TerrainFrequencyDivisor/2.0);
		double strength = Perlin.OctavePerlin(
			offset+(worldPos.x/GlobalConfig.Config_TerrainFrequencyDivisor), 
			offset+(worldPos.y/GlobalConfig.Config_TerrainFrequencyDivisor), 
			offset+(worldPos.z/GlobalConfig.Config_TerrainFrequencyDivisor), 
			4, 0.5)/heightDiv;
		return strength;
	}

    private byte fillGridPosition(int x, int y, int z)
	{
		double strength = getPerlin (x-1, y, z-1);
		//test
		//double strength = Perlin.perlin(
		//	offset+(worldPos.x/(double)GlobalConfig.Config_TerrainFrequencyDivisor),
		//	offset+(worldPos.y/(double)GlobalConfig.Config_TerrainFrequencyDivisor),
		//	offset+(worldPos.z/(double)GlobalConfig.Config_TerrainFrequencyDivisor));

		//test
		//double strength = 1.0-((worldPos.x+worldPos.y+worldPos.z)*0.01);

		if (strength < solidTerrainThreshold) {
			return 0;
		} else if (strength < 1.1) {
			return 11;
		}else if (strength < 1.11) {
			return 10;
		}else if (strength < 1.12) {
			return 9;
		}else if (strength < 1.13) {
			return 5;
		}else if (strength < 1.15) {
			return 4;
		}else if (strength < 1.2) {
			return 3;
		}else if (strength < 1.3) {
			return 1;
		}else if (strength >= 1.3) {
			return 2;
		}
		return 0;
	}

	private byte fillGridShape(byte[,,] voxels,int x, int y, int z, int max)
	{



		if (voxels [x, y, z] == 0)
			return 0;



		byte ypos = 0;

		if (y < GlobalConfig.Config_ChunkVerticalDimensionality - 1)
			ypos = voxels [x, y + 1, z];	
		else
			ypos = getShapeAtGrid (x, y + 1, z);
	
		byte xpos = 1;
		if (x < max)
			xpos = voxels [x + 1, y, z];
		else
			xpos = getShapeAtGrid (x + 1, y, z);
		
		byte xneg = 1;
		if (x > 0)
			xneg = voxels [x - 1, y, z];
		else
			xneg = getShapeAtGrid (x - 1, y, z);
		
		byte zpos = 1;
		if (z < max)
			zpos = voxels [x, y, z + 1];
		else
			zpos = getShapeAtGrid (x, y, z + 1);
		
		byte zneg = 1;
		if (z > 0)
			zneg = voxels [x, y, z -1];
		else
			zneg = getShapeAtGrid (x, y, z - 1);


        byte xpzp = 1;
        if(x < max && z < max)
            xpzp = voxels[x + 1, y, z + 1];
		else
			xpzp = getShapeAtGrid (x + 1, y, z + 1);
		
        byte xnzp = 1;
        if (x > 0 && z < max)
            xnzp = voxels[x - 1, y, z + 1];
		else
			xnzp = getShapeAtGrid (x - 1, y, z + 1);
		
        byte xnzn = 1;
        if (x > 0 && z > 0)
            xnzn = voxels[x - 1, y, z - 1];
		else
			xnzn = getShapeAtGrid (x - 1, y, z - 1);
		
		byte xpzn = 1;
        if (x < max && z > 0)
            xpzn = voxels[x + 1, y, z - 1];
		else
			xpzn = getShapeAtGrid (x + 1, y, z - 1);
		
		if (xpos == 0 && xneg > 0 && zpos > 0 && zneg > 0 && ypos == 0)
            return 2;
		if (xpos > 0 && xneg == 0 && zpos > 0 && zneg > 0 && ypos == 0)
            return 3;
		if (xpos > 0 && xneg > 0 && zpos == 0 && zneg > 0 && ypos == 0)
            return 4;
		if (xpos > 0 && xneg > 0 && zpos > 0 && zneg == 0 && ypos == 0)
            return 5;
        
		if (xpos == 0 && xneg > 0 && zpos == 0 && zneg > 0 && ypos == 0)
            return 6;
		if (xpos > 0 && xneg == 0 && zpos == 0 && zneg > 0 && ypos == 0)
            return 7;
		if (xpos > 0 && xneg == 0 && zpos > 0 && zneg == 0 && ypos == 0)
            return 8;
		if (xpos == 0 && xneg > 0 && zpos > 0 && zneg == 0 && ypos == 0)
            return 9;
            
        
		if (xpos > 1 && xneg > 0 && zpos > 1 && zneg > 0 && xpzp == 0 && ypos == 0)
            return 10;
		if (xpos > 0 && xneg > 1 && zpos > 1 && zneg > 0 && xnzp == 0 && ypos == 0)
            return 11;
		if (xpos > 0 && xneg > 1 && zpos > 0 && zneg > 1 && xnzn == 0 && ypos == 0)
            return 12;
		if (xpos > 1 && xneg > 0 && zpos > 0 && zneg > 1 && xpzn == 0 && ypos == 0)
            return 13;
            

        return 1;
	}

	


	public byte getShapeAtGrid(int x, int y,int z)
	{
        x -= 1;
        z -= 1;
		if (getPerlin (x, y, z) < solidTerrainThreshold)
			return 0;

		byte xpos = (byte)((getPerlin (x + 1, y, z)>=solidTerrainThreshold)?1:0);
		//byte ypos = (byte)((getPerlin (x, y + 1, z)>=solidTerrainThreshold)?1:0);
		byte zpos = (byte)((getPerlin (x, y, z + 1)>=solidTerrainThreshold)?1:0);
		byte xneg = (byte)((getPerlin (x - 1, y, z)>=solidTerrainThreshold)?1:0);
		//byte yneg = (byte)((getPerlin (x, y - 1, z)>=solidTerrainThreshold)?1:0);
		byte zneg = (byte)((getPerlin (x, y, z - 1)>=solidTerrainThreshold)?1:0);
		byte xpzp = (byte)((getPerlin (x + 1, y, z + 1)>=solidTerrainThreshold)?1:0);
		byte xnzp = (byte)((getPerlin (x - 1, y, z + 1)>=solidTerrainThreshold)?1:0);
		byte xpzn = (byte)((getPerlin (x + 1, y, z - 1)>=solidTerrainThreshold)?1:0);
		byte xnzn = (byte)((getPerlin (x - 1, y, z - 1)>=solidTerrainThreshold)?1:0);

		if (xpos == 0 && xneg > 0 && zpos > 0 && zneg > 0)
			return 2;
		if (xpos > 0 && xneg == 0 && zpos > 0 && zneg > 0)
			return 3;
		if (xpos > 0 && xneg > 0 && zpos == 0 && zneg > 0)
			return 4;
		if (xpos > 0 && xneg > 0 && zpos > 0 && zneg == 0)
			return 5;

		if (xpos == 0 && xneg > 0 && zpos == 0 && zneg > 0)
			return 6;
		if (xpos > 0 && xneg == 0 && zpos == 0 && zneg > 0)
			return 7;
		if (xpos > 0 && xneg == 0 && zpos > 0 && zneg == 0)
			return 8;
		if (xpos == 0 && xneg > 0 && zpos > 0 && zneg == 0)
			return 9;

		if (xpos > 1 && xneg > 0 && zpos > 1 && zneg > 0 && xpzp == 0)
			return 10;
		if (xpos > 0 && xneg > 1 && zpos > 1 && zneg > 0 && xnzp == 0)
			return 11;
		if (xpos > 0 && xneg > 1 && zpos > 0 && zneg > 1 && xnzn == 0)
			return 12;
		if (xpos > 1 && xneg > 0 && zpos > 0 && zneg > 1 && xpzn == 0)
			return 13;

		return 1;
	}

}




