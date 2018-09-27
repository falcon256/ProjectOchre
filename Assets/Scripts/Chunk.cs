using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Chunk {

    public Object chunkLock = new Object();

    public GameObject ChunkGO = null;
    public GameObject ChunkWaterGO = null;
    public Vector3Int gridPosition;
    public Vector3 cornerPosition;
    public bool meshUpdateRequired = false;
    public bool isMeshUpdateUrgent = false;
    private int generationStage = -1;

    private Mesh chunkMesh = null;
    private Mesh chunkWaterMesh = null;
    private byte[,,] voxels = null;
    private byte[,,] voxelShapes = null;
    private FlowPoint[,,] fluidDynamicsData = null;
    private GameObject chunkGOPrefab = null;
    public ChunkTerrainGenerator chunkTerrainGenerator = null;
    public ChunkMeshGenerator chunkMeshGenerator = null;
    public volatile bool isGenerating = false;
    private bool isChunkFull = false;
    private bool isChunkEmpty = false;

    public Chunk xposNeighbor = null;
    public Chunk xnegNeighbor = null;
    public Chunk zposNeighbor = null;
    public Chunk znegNeighbor = null;

	public bool dynamicsUpdateInProgress = false;
	public ChunkDynamicsProcessor dynamicsProcessor = null;


    //test
    public bool testChunk = true;
	public int waterUpdateCount = 0;


    //public Texture3D waterFlowTexture = null;
    //public Texture3D airAndTempFlowTexture = null;

    // saturation, permeability, temperature, airdensity, airpollution, airpermeability, landfertility, extra
    // 157216 bytes of data transfered to GPU each frame for 32 size chunks... Not bad at all.

    public double lastUpdateTime = 0;
    public double lastFluidUpdateTime = 0;



    public Chunk(Vector3Int gridp)
	{
		gridPosition = gridp;
		cornerPosition.x = gridPosition.x * GlobalConfig.Config_ChunkDimensionality;
		cornerPosition.y = 0;
		cornerPosition.z = gridPosition.z * GlobalConfig.Config_ChunkDimensionality;
		voxels = new byte[GlobalConfig.Config_ChunkDimensionality + 2, GlobalConfig.Config_ChunkVerticalDimensionality, GlobalConfig.Config_ChunkDimensionality + 2];
		voxelShapes = new byte[GlobalConfig.Config_ChunkDimensionality + 2, GlobalConfig.Config_ChunkVerticalDimensionality, GlobalConfig.Config_ChunkDimensionality + 2];
		fluidDynamicsData = new FlowPoint[GlobalConfig.Config_ChunkDimensionality + 2, GlobalConfig.Config_ChunkVerticalDimensionality, GlobalConfig.Config_ChunkDimensionality + 2];

		chunkGOPrefab = (GameObject)Resources.Load ("Prefabs/TerrainChunk");
		ChunkGO = (GameObject)Object.Instantiate (chunkGOPrefab);
		ChunkGO.name = "" + gridp;
		chunkMesh = new Mesh ();
		ChunkGO.GetComponent<MeshFilter> ().mesh = chunkMesh;
		chunkWaterMesh = new Mesh ();
		ChunkGO.GetComponent<ChunkChildrenScript> ().waterChildGOMeshFilter.mesh = chunkWaterMesh;
		ChunkGO.transform.position = cornerPosition;

		dynamicsProcessor = new ChunkDynamicsProcessor (this);
	}

	public void doWaterUpdate()
	{
		dynamicsProcessor.doWaterUpdate ();
	}

    public bool needsUpdate()
    {
        if (Time.realtimeSinceStartup - lastUpdateTime > 120&&generationStage==1&&chunkMeshGenerator==null)
            return true;
        return false;
    }
		
    public byte[,,] getVoxelData()
    {//TODO (check) not a copy, for speed.//Yep, looks like these pass by reference properly, not an array copy.
        return voxels;
    }

    public void setVoxelData(byte[,,] dat)
    {
        voxels = dat;
    }

    public FlowPoint[,,] getFlowVolumeData()
    {
        return fluidDynamicsData;
    }

    /*
     * I don't think anything like this should be used after startup/init.
    public void setWaterVolumeData(float [,,] dat)
    {
        waterVolume = dat;
    }
    */
    public byte[,,] getVoxelShapeData()
    {
        return voxelShapes;
    }

    public void setVoxelShapeData(byte[,,] dat)
    {
        voxelShapes = dat;
    }

    public Mesh getChunkMesh()
    {
        return chunkMesh;
    }

    public void setChunkMesh(Mesh m)
    {
        chunkMesh = m;
        ChunkGO.GetComponent<MeshFilter>().mesh = chunkMesh;
    }

    public Mesh getChunkWaterMesh()
    {
        return chunkWaterMesh;
    }

    public void setChunkWaterMesh(Mesh m)
    {
        chunkWaterMesh = m;
        ChunkGO.GetComponent<ChunkChildrenScript>().waterChildGOMeshFilter.mesh = chunkWaterMesh;
    }

    public bool isPointInBounds(Vector3 point)
    {
        //Debug.Log ("" + point + " " + cornerPosition + " " + gridPosition);
        if (point.x >= cornerPosition.x && point.y >= cornerPosition.y && point.z >= cornerPosition.z &&
           point.x <= cornerPosition.x + GlobalConfig.Config_ChunkDimensionality &&
           point.y <= cornerPosition.y + GlobalConfig.Config_ChunkVerticalDimensionality &&
           point.z <= cornerPosition.z + GlobalConfig.Config_ChunkDimensionality)
            return true;
        return false;
    }
		
    public Vector3 localGridPosToWorldPos(Vector3Int inp)
    {
		Vector3 outp = cornerPosition;
		outp.x += (int)inp.x;
		outp.y += (int)inp.y;
		outp.z += (int)inp.z;
        return outp;
    }
    


    public void completeGeneration()
    {
        generationStage = 1;
        //Debug.Log("Chunk filled with data at " + gridPosition + ". Generator Threads:" + ChunkTerrainGenerator.Threads + " Mesh Threads:" + ChunkMeshGenerator.Threads);
    }



	public void updateNeighborData()
    {		
		TerrainManager.getSingleton ().setupChunkNeighbors (this);
    }
	public bool isFull()
	{
		return isChunkFull;
	}

	public bool isEmpty()
	{
		return isChunkEmpty;
	}

	public void setFull(bool f)
	{
		isChunkFull = f;
	}

	public void setEmpty(bool e)
	{
		isChunkEmpty = e;
	}


    
}
