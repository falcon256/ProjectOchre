using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunkDynamicsProcessor{

	Chunk parent = null;
	public const int computeBufferStride = 4 * 8;
	public const int computeBufferSize = (GlobalConfig.Config_ChunkDimensionality + 2) * (GlobalConfig.Config_ChunkVerticalDimensionality) * (GlobalConfig.Config_ChunkDimensionality + 2);
	public ComputeBuffer flowData = null;
	private byte[,,] voxelShapes = null;
    private byte[,,] voxels = null;
	public static int updateThreads = 0;
    ComputeBuffer flowInData = null;
    ComputeBuffer flowOutData = null;
    FlowPoint[,,] flowPoints = null;
    ComputeShader shader = null;
    AsyncGPUReadbackRequest request;


	public ChunkDynamicsProcessor(Chunk c)
	{
		parent = c;
        flowPoints = c.getFlowVolumeData();
		voxelShapes = c.getVoxelData ();
        voxels = c.getVoxelData();
        flowInData = new ComputeBuffer(computeBufferSize, computeBufferStride);
        flowOutData = new ComputeBuffer(computeBufferSize, computeBufferStride);
        shader = GameManager.getSingleton().terrainFlowComputeShader;
        int kernel = shader.FindKernel("MLD");
        shader.SetBuffer(kernel, "flowInBuffer", flowInData);
        shader.SetBuffer(kernel, "flowOutBuffer", flowOutData);
    }

    public void cleanUp()
    {
        flowInData.Release();
        flowOutData.Release();
    }

    ~ChunkDynamicsProcessor()
    {
        TerrainManager.getSingleton().logError("ChunkDynamicsProcessor disposed of at " + parent.gridPosition);
    }

	public void doWaterUpdate()
	{
		//Debug.Log ("Water Update Triggered");

		parent.dynamicsUpdateInProgress = true;
        if (GlobalConfig.Config_UseComputeShaders)
            doComputeShaderFluidUpdate();
        else
        {
            Thread t = new Thread(this.Execute);
            TerrainManager.getSingleton().addChunkToFluidDynamicsUpdateQueue(parent);
            t.Start();
        }
	}

    

	private void Execute()
	{
		updateThreads++;
		voxelShapes = parent.getVoxelData ();
		try{
		
		doCPUFluidUpdate ();

		TerrainManager.getSingleton().updateFluidNeighbors(parent);
		
		} catch (System.Exception e) {
			TerrainManager.getSingleton ().logError (e.Message + e.StackTrace);
		}

		parent.dynamicsUpdateInProgress = false;
		parent.waterUpdateCount++;
		updateThreads--;
		TerrainManager.getSingleton ().logError ("cpu water complete. "+updateThreads+" remaining in-thread. " + parent.waterUpdateCount);
	}


	public float getShapePerm(byte s)
	{
		if (s == 0)
			return 0.99f;
		if (s == 1)
			return 0.001f;
		return 0.001f;
	}

	private void cpuVoxelTick(int x, int y, int z)
	{
		//if (waterVolume [x, y, z] > 0.001f) 
		{
			float myperm = getShapePerm(voxelShapes [x - 1, y, z - 1]);
			float ypperm = getShapePerm (voxelShapes [x - 1, y + 1, z - 1]);
			float ynperm = getShapePerm(voxelShapes [x - 1, y - 1, z - 1]);

			float downdif = Mathf.Max (0, 1.0f - flowPoints [x, y - 1, z].saturation);
            flowPoints[x, y - 1, z].saturation += (Mathf.Min (downdif, flowPoints[x, y, z].saturation)) * myperm * ynperm;
            flowPoints[x, y, z].saturation -= (Mathf.Min (downdif, flowPoints[x, y, z].saturation)) * myperm * ynperm;

            //a small amount of downward flow is added to allow pressure.
            flowPoints[x, y - 1, z].saturation += flowPoints[x, y, z].saturation * myperm * ynperm * 0.25f;
            flowPoints[x, y, z].saturation -= flowPoints[x, y, z].saturation * myperm * ynperm * 0.25f;
            flowPoints[x, y, z].saturation += flowPoints[x, y + 1, z].saturation * myperm * ypperm * 0.25f;
            flowPoints[x, y + 1, z].saturation -= flowPoints[x, y + 1, z].saturation * myperm * ypperm * 0.25f;

			float pushup = Mathf.Max (0, flowPoints[x, y, z].saturation - 1.0f) * myperm * ypperm * 0.25f;
            flowPoints[x, y + 1, z].saturation += pushup;
            flowPoints[x, y, z].saturation -= pushup;

			float xpflow = 0;
			float xnflow = 0;
			float zpflow = 0;
			float znflow = 0;
			float xpperm = 0.001f;
			float xnperm = 0.001f;
			float zpperm = 0.001f;
			float znperm = 0.001f;

			if (x < GlobalConfig.Config_ChunkDimensionality) {
				xpperm = getShapePerm(voxelShapes [x, y, z - 1]);
				xpflow = (flowPoints[x, y, z].saturation - flowPoints[x + 1, y, z].saturation) * 0.25f * myperm * xpperm;
			}
			if (x > 1) {
				xnperm = getShapePerm(voxelShapes [x - 2, y, z - 1]);
				xnflow = (flowPoints[x, y, z].saturation - flowPoints[x - 1, y, z].saturation) * 0.25f * myperm * xnperm;
			}
			if (z < GlobalConfig.Config_ChunkDimensionality) {
				zpperm = getShapePerm(voxelShapes [x - 1, y, z ]);
				zpflow = (flowPoints[x, y, z].saturation - flowPoints[x, y, z + 1].saturation) * 0.25f * myperm * zpperm;
			}
			if (z > 1) {
				znperm = getShapePerm(voxelShapes [x - 1, y, z - 2]);
				znflow = (flowPoints[x, y, z].saturation - flowPoints[x, y, z - 1].saturation) * 0.25f * myperm * znperm;
			}

            flowPoints[x, y, z].saturation -= (xpflow + xnflow + zpflow + znflow);
            flowPoints[x + 1, y, z].saturation += xpflow;
            flowPoints[x - 1, y, z].saturation += xnflow;
            flowPoints[x, y, z + 1].saturation += zpflow;
            flowPoints[x, y, z - 1].saturation += znflow;
			//test
			//waterVolume [x, y, z] *= 0.5f;
		}
	}

	public void doCPUFluidUpdate()
	{
		//debug
		//TerrainManager.getSingleton ().logError ("cpu water thread update reached");

		//debug
		if (parent.gridPosition.x == 0 && parent.gridPosition.z == 0)
			for (int i =100; i < 111; i++)
                flowPoints[16, i, 16].saturation += 1.0f;

		//for (int i = 0; i < 1; i++) {
			for (int z = 1; z < GlobalConfig.Config_ChunkDimensionality; z++) {
				for (int x = 1; x < GlobalConfig.Config_ChunkDimensionality; x++) {
					for (int y = GlobalConfig.Config_ChunkVerticalDimensionality - 2; y > 1; y--) {
						cpuVoxelTick (x, y, z);
					}
				}
			}
			for (int y = GlobalConfig.Config_ChunkVerticalDimensionality - 2; y > 1; y--) {
				for (int z = 1; z < GlobalConfig.Config_ChunkDimensionality; z++) {
					for (int x = 1; x < GlobalConfig.Config_ChunkDimensionality; x++) {					
						cpuVoxelTick (x, y, z);
					}
				}
			}
			for (int y = GlobalConfig.Config_ChunkVerticalDimensionality - 2; y > 1; y--) {
				for (int z = 1; z < GlobalConfig.Config_ChunkDimensionality; z++) {
					for (int x = GlobalConfig.Config_ChunkDimensionality; x > 1; x--) {					
						cpuVoxelTick (x, y, z);
					}
				}
			}
			for (int y = GlobalConfig.Config_ChunkVerticalDimensionality - 2; y > 1; y--) {
				for (int x = 1; x < GlobalConfig.Config_ChunkDimensionality; x++) {
					for (int z = 1; z < GlobalConfig.Config_ChunkDimensionality; z++) {					
						cpuVoxelTick (x, y, z);
					}
				}
			}
			for (int y = GlobalConfig.Config_ChunkVerticalDimensionality - 2; y > 1; y--) {
				for (int x = 1; x < GlobalConfig.Config_ChunkDimensionality; x++) {
					for (int z = GlobalConfig.Config_ChunkDimensionality; z > 1; z--) {					
						cpuVoxelTick (x, y, z);
					}
				}
			}
		//}
	}



	public void doComputeShaderFluidUpdate()
	{
        if (parent.gridPosition.x == 0 && parent.gridPosition.z == 0)
        {
            flowPoints[16, 100, 16].saturation += 1.1f;
            flowPoints[17, 100, 16].saturation += 1.1f;
            flowPoints[18, 100, 16].saturation += 1.1f;
            flowPoints[19, 100, 16].saturation += 1.1f;
            flowPoints[20, 100, 16].saturation += 1.1f;
            flowPoints[21, 100, 16].saturation += 1.1f;
            flowPoints[22, 100, 16].saturation += 1.1f;
            flowPoints[23, 100, 16].saturation += 1.1f;

        }
        //TODO going to get rewritten so we have a 1:1 mapping with chunk data.
		for (int y = 0; y < GlobalConfig.Config_ChunkVerticalDimensionality; y++)
		{
			for (int z = 0; z < GlobalConfig.Config_ChunkDimensionality + 2; z++)
			{
				for (int x = 0; x < GlobalConfig.Config_ChunkDimensionality + 2; x++)
				{
					flowPoints[x,y,z].permeability = (getShapePerm(voxelShapes[x,y,z]));					
				}
			}
		}

		

		

        int kernel = shader.FindKernel("MLD");
        shader.SetBuffer(kernel, "flowInBuffer", flowInData);
        shader.SetBuffer(kernel, "flowOutBuffer", flowOutData);
        flowInData.SetData(flowPoints);
        //Graphics.SetRandomWriteTarget(0, flowOutData);//not sure if needed
        shader.Dispatch(kernel, 34, 1, 34);
        request = new AsyncGPUReadbackRequest();
        request = AsyncGPUReadback.Request(flowOutData);
        TerrainManager.getSingleton().addDynamicsProcessorToGPURequestList(this);

		//TerrainManager.getSingleton().logError("" + printSaturdationData(flowPoints));
		
		//lastFluidUpdateTime = Time.realtimeSinceStartup;
	}

    public bool perFrameGPURequestTest()
    {
        if(request.done)
        {
            flowOutData.GetData(flowPoints);//does this overwrite our old goodness properly or assign a new location!? - overwrites
            TerrainManager.getSingleton().logError("async gpu request done at " + parent.gridPosition);
            
            /*
            for (int y = 0; y < GlobalConfig.Config_ChunkVerticalDimensionality; y++)
            {
                for (int z = 0; z < GlobalConfig.Config_ChunkDimensionality + 2; z++)
                {
                    for (int x = 0; x < GlobalConfig.Config_ChunkDimensionality + 2; x++)
                    {                       
                        waterVolume[x, y, z] = flowPoints[x,y,z].saturation;
                    }
                }
            }
            */
            parent.dynamicsUpdateInProgress = false;
            return true;
        }
        return false;
    }

	//debug code
	private string printSaturdationData(FlowPoint[] data)
	{
		string eee = "";

		//TODO adjust for new verticaldimension
		for(int i = 0; i < 34*34*3; i+=34)
		{
			//if (i % 34 == 0)
			//    eee += "\n";
			eee += (int)(data[i].saturation * 99.99f) + " ";
			// eee += (int)(data[i].permeability * 9.99f);
			//eee += data[i].saturation + " ";
		}


		return eee;
	}

    public Chunk getParent()
    {
        return parent;
    }
}
