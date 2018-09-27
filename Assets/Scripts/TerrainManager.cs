using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;

public class TerrainManager{

	private float lastTerrainUpdateTime = 0;

	public bool abortAllThreads = false;

    public Object ErrorQueueLock = new Object();
    private Queue<string> generationErrorQueue = null;

    public Object terrainChunksLock = new Object();
    //private List<Chunk> terrainChunks = null;
	private Dictionary<Vector3Int,Chunk> terrainChunksDict;
	private Queue<Chunk> generationQueue = null;
	private List<Chunk> generationInProgressList = null;
	private Queue<Chunk> meshUpdateQueue = null;
	private List<Chunk> meshUpdateInProgressList = null;
	public Object waterMeshUpdateQueueLock = new Object ();
    private Queue<Chunk> waterMeshUpdateQueue = null;
    private List<Chunk> waterMeshUpdateInProgressList = null;

	private Queue<Chunk> fluidDynamicsUpdateQueue = null;
    private List<ChunkDynamicsProcessor> fluidDynamicsGPUUpdateList = null;
    private Object fluidDynamicsGPUUpdateListLock = new Object();
    private List<ChunkDynamicsProcessor> fluidDynamicsGPURemoveList = null;

    private static TerrainManager terrainManagerSingleton = null;

	public Vector3 CameraLocation = Vector3.zero;

	private int lastUpdateTestX = -GlobalConfig.Config_ChunkLODDistance;
	private int lastUpdateTestY = -GlobalConfig.Config_ChunkLODDistance;
	private int lastUpdateTestZ = -GlobalConfig.Config_ChunkLODDistance;
	private int totalMeshesGenerated = 0;
	private int currentGeneratorCheckTicker = 0;
	private int currentMeshCheckTicker = 0;
    private int currentWaterMeshCheckTicker = 0;
    private bool initialStartupComplete = false;


	public TerrainManager()
	{
		CameraLocation = new Vector3 (0, 128.0f, 0);
		//terrainChunks = new List<Chunk> ();
		terrainChunksDict = new Dictionary<Vector3Int, Chunk> ();
		generationQueue = new Queue<Chunk> ();
		generationInProgressList = new List<Chunk> ();

		meshUpdateQueue = new Queue<Chunk> ();
		meshUpdateInProgressList = new List<Chunk> ();

        waterMeshUpdateQueue = new Queue<Chunk>();
        waterMeshUpdateInProgressList = new List<Chunk>();
       
        generationErrorQueue = new Queue<string>();
		fluidDynamicsUpdateQueue = new Queue<Chunk>();
        fluidDynamicsGPUUpdateList = new List<ChunkDynamicsProcessor>();
        fluidDynamicsGPURemoveList = new List<ChunkDynamicsProcessor>();
}

	public static TerrainManager getSingleton()
	{
		if(terrainManagerSingleton==null)
		{
			terrainManagerSingleton = new TerrainManager();
		}
		return terrainManagerSingleton;
	}

    public void logError(string error)
    {
        lock(ErrorQueueLock)
        {
            generationErrorQueue.Enqueue(error);
        }
    }

    private void logErrors()
    {
        lock (ErrorQueueLock)
        {
            if (generationErrorQueue.Count > 0)
                Debug.LogError(generationErrorQueue.Dequeue());
        }
    }

    public void cruiseOverLocalTerrain()
    {
		lastUpdateTestY = 0;
        if(++lastUpdateTestX> GlobalConfig.Config_ChunkLODDistance + worldPositionToChunkGrid(CameraLocation).x)
        {
            lastUpdateTestX = -GlobalConfig.Config_ChunkLODDistance + worldPositionToChunkGrid(CameraLocation).x;
            if(++lastUpdateTestZ> GlobalConfig.Config_ChunkLODDistance + worldPositionToChunkGrid(CameraLocation).z)
            {
                lastUpdateTestZ = -GlobalConfig.Config_ChunkLODDistance + worldPositionToChunkGrid(CameraLocation).z;
                
            }
        }

        Chunk c = getChunkByGrid(new Vector3Int(lastUpdateTestX, lastUpdateTestY, lastUpdateTestZ));
        if(c!=null)
        {
			if (!c.dynamicsUpdateInProgress) {
				if (ChunkDynamicsProcessor.updateThreads < SystemInfo.processorCount) {
					if (c.lastFluidUpdateTime + GlobalConfig.Config_ChunkWaterUpdateDelay < Time.realtimeSinceStartup) {
						if (!fluidDynamicsUpdateQueue.Contains (c))
							c.doWaterUpdate ();
						else
							Debug.Log ("not currently doing a dynamics update but fluidDynamicsUpdateQueue contains chunk");
						//Debug.Log("Chunk " + c.gridPosition + " queued for water update");
						//waterMeshUpdateQueue.Enqueue(c);
					}
				}
			}
        }
    }

    public void doUpdate()
    {
		if (lastTerrainUpdateTime + 0.25f < Time.realtimeSinceStartup) 
		{
			lastTerrainUpdateTime = Time.realtimeSinceStartup;
			if (!initialStartupComplete) {
				initialStartupComplete = true;
				//	generateInitialTerrain ();
			}




			logErrors ();
			UnityEngine.Profiling.Profiler.BeginSample ("Testing for new chunks to generate(testForGen())");
			testForGen ();
			UnityEngine.Profiling.Profiler.EndSample ();
			UnityEngine.Profiling.Profiler.BeginSample ("Testing for generating new tergenthreads(handleGenerationQueue())");
			handleGenerationQueue ();
			UnityEngine.Profiling.Profiler.EndSample ();
			UnityEngine.Profiling.Profiler.BeginSample ("Testing for mesh updates(handleMeshQueue())");
			handleMeshQueue ();
			UnityEngine.Profiling.Profiler.EndSample ();
			UnityEngine.Profiling.Profiler.BeginSample ("Testing for water mesh updates(handleWaterMeshQueue())");
			handleWaterMeshQueue ();
			UnityEngine.Profiling.Profiler.EndSample ();
			UnityEngine.Profiling.Profiler.BeginSample ("Fluid Dynamics Thread Creation(handleFluidDynamicsUpdateQueue())");
			handleFluidDynamicsUpdateQueue ();
			UnityEngine.Profiling.Profiler.EndSample ();
			UnityEngine.Profiling.Profiler.BeginSample ("Nearby terrain update testing(cruiseOverLocalTerrain())");
			cruiseOverLocalTerrain ();
			UnityEngine.Profiling.Profiler.EndSample ();
			//Debug.Log (meshUpdateQueue.Count + " " + meshUpdateInProgressList.Count + " " + waterMeshUpdateQueue.Count + " " + waterMeshUpdateInProgressList.Count);
		}
    }
    /*
	public void generateInitialTerrain()
	{
		for (int x = -2; x <= 2; x++)			
			for (int z = -2; z <= 2; z++) {
				Vector3 offsetVector = CameraLocation;
				Vector3Int grid = Vector3Int.zero();
				grid = worldPositionToChunkGrid (offsetVector);
				grid.x += x;
				grid.y = 0;
				grid.z += z;
				Chunk c = getChunkByGrid (grid);
				if (c == null) 
				{

							createChunkAtGrid (grid);
				}

				}

	}
    */


    public void testForGen()
    {
        bool done = false;
        int iterations = 0;
        Vector3Int startPos = worldPositionToChunkGrid(CameraLocation);
        Vector3Int curPos = startPos;

        //Random rand = new Random ();
        if ((ChunkTerrainGenerator.Threads < SystemInfo.processorCount))
        {
            while (!done)
            {
                int x = (int)Random.Range(-1, 2);//max is exclusive, so offset it.
                int z = (int)Random.Range(-1, 2);
                curPos.x += x;
				curPos.y = 0;
                curPos.z += z;         

                Chunk c = getChunkByGrid(curPos);
                if (c == null)
                {
                    createChunkAtGrid(curPos);
                    done = true;
                }
                
                if (iterations++ > totalMeshesGenerated || iterations > GlobalConfig.Config_ChunkLODDistance)
                    done = true;
            }
        }
    }

    public void updateFluidNeighbors(Chunk c)
    {
        Vector3Int pos = c.gridPosition;
        Vector3Int pospx = pos;
        Vector3Int posnx = pos;
        Vector3Int pospz = pos;
        Vector3Int posnz = pos;
        pospx.x += 1;
        pospz.z += 1;
        posnx.x -= 1;
        posnz.z -= 1;
        Chunk cpx = getChunkByGrid(pospx);
        Chunk cnx = getChunkByGrid(posnx);
        Chunk cpz = getChunkByGrid(pospz);
        Chunk cnz = getChunkByGrid(posnz);
        
        FlowPoint[,,] cw = c.getFlowVolumeData();

        if (cpx!=null&&cpx.getFlowVolumeData() != null)
        {
            FlowPoint[,,] cpxw = cpx.getFlowVolumeData();
            for (int y = 0; y < GlobalConfig.Config_ChunkVerticalDimensionality; y++)
            {
                for (int z = 0; z < GlobalConfig.Config_ChunkDimensionality + 2; z++)
                {
                    //since these structs are value types we get copies.
                    cpxw[0, y, z] = cw[GlobalConfig.Config_ChunkDimensionality, y, z];
                    cpxw[1, y, z] = cw[GlobalConfig.Config_ChunkDimensionality + 1, y, z];
                }
            }
        }
        
        if (cpz != null&&cpz.getFlowVolumeData() != null)
        {
            FlowPoint[,,] cpzw = cpz.getFlowVolumeData();
            for (int x = 0; x < GlobalConfig.Config_ChunkDimensionality + 2; x++)
            {
                for (int y = 0; y < GlobalConfig.Config_ChunkVerticalDimensionality; y++)
                {
                    cpzw[x, y, 0] = cw[x, y, GlobalConfig.Config_ChunkDimensionality];
                    cpzw[x, y, 1] = cw[x, y, GlobalConfig.Config_ChunkDimensionality + 1];
                }
            }
        }

        if (cnx != null&&cnx.getFlowVolumeData() != null)
        {
            FlowPoint[,,] cnxw = cnx.getFlowVolumeData();
            for (int y = 0; y < GlobalConfig.Config_ChunkVerticalDimensionality; y++)
            {
                for (int z = 0; z < GlobalConfig.Config_ChunkDimensionality + 2; z++)
                {
                    cnxw[GlobalConfig.Config_ChunkDimensionality, y, z] = cw[0, y, z];
                    cnxw[GlobalConfig.Config_ChunkDimensionality + 1, y, z] = cw[1, y, z];
                }
            }
        }
        if (cnz != null&&cnz.getFlowVolumeData() != null)
        {
            FlowPoint[,,] cnzw = cnz.getFlowVolumeData();
            for (int x = 0; x < GlobalConfig.Config_ChunkDimensionality + 2; x++)
            {
                for (int y = 0; y < GlobalConfig.Config_ChunkVerticalDimensionality; y++)
                {
                    cnzw[x, y, GlobalConfig.Config_ChunkDimensionality] = cw[x, y, 0];
                    cnzw[x, y, GlobalConfig.Config_ChunkDimensionality + 1] = cw[x, y, 1];
                }
            }
        }
        
    }

	private void handleFluidDynamicsUpdateQueue()
	{
		for (int i = 0; i < SystemInfo.processorCount; i++) {
			if (fluidDynamicsUpdateQueue.Count > 0) {
				Chunk c = fluidDynamicsUpdateQueue.Dequeue ();
				//lock (waterMeshUpdateQueueLock) {
					if (!c.dynamicsUpdateInProgress && !waterMeshUpdateQueue.Contains (c)) {
					
						waterMeshUpdateQueue.Enqueue (c);
						TerrainManager.getSingleton ().logError ("Water mesh enqueued " + fluidDynamicsUpdateQueue.Count + " " + waterMeshUpdateQueue.Count);
						c.lastFluidUpdateTime = Time.realtimeSinceStartup;
					} else
						fluidDynamicsUpdateQueue.Enqueue (c);
				//}
			}
		}
	}

    private void handleWaterMeshQueue()
    {
        if (waterMeshUpdateQueue.Count > 0)
        {
            Chunk gen = waterMeshUpdateQueue.Dequeue();
			//lock (waterMeshUpdateQueueLock) {
				if (ChunkMeshGenerator.Threads < SystemInfo.processorCount && gen.chunkMeshGenerator == null) {
					gen.isGenerating = true;
					ChunkMeshGenerator generator = new ChunkMeshGenerator (gen);
					gen.chunkMeshGenerator = generator;
                
					if (gen.chunkMeshGenerator == null)
						logError ("wtf2?");
					Thread t = new Thread (generator.fluidGenerate);
					t.Start ();
					waterMeshUpdateInProgressList.Add (gen);
					if (!waterMeshUpdateInProgressList.Contains (gen))
						Debug.Log ("wtf3");
					//Debug.Log("Chunk " + gen.gridPosition + " tossed into update queue");
				} else {
					waterMeshUpdateQueue.Enqueue (gen);
					if (gen.chunkMeshGenerator != null)
						Debug.Log ("Chunk " + gen.gridPosition + " returned to queue with a non-null mesh generator");
				}
			//}
        }

        if (waterMeshUpdateInProgressList.Count > 0)
        {
			currentWaterMeshCheckTicker++;
			if (currentWaterMeshCheckTicker > (waterMeshUpdateInProgressList.Count - 1))
				currentWaterMeshCheckTicker = 0;
			Chunk c = waterMeshUpdateInProgressList[currentWaterMeshCheckTicker];
			if (c == null)
				Debug.Log ("Got a bad chunk with an index we thought was good");
            if (c != null && c.chunkMeshGenerator != null)
            {
                if (c.chunkMeshGenerator.isDone)
                {
                    c.getChunkWaterMesh().Clear();
                    c.getChunkWaterMesh().vertices = c.chunkMeshGenerator.getWaterVertices();
                    c.getChunkWaterMesh().uv = c.chunkMeshGenerator.getWaterUVs();
                    c.getChunkWaterMesh().triangles = c.chunkMeshGenerator.getWaterTriangles();
                    c.getChunkWaterMesh().RecalculateBounds();
                    c.getChunkWaterMesh().RecalculateNormals();
                    c.getChunkWaterMesh().RecalculateTangents();
                    c.setChunkWaterMesh(c.getChunkWaterMesh());

                    c.chunkMeshGenerator = null;
                    c.lastFluidUpdateTime = Time.realtimeSinceStartup;
                    updateFluidNeighbors(c);


					waterMeshUpdateInProgressList.Remove(c);
                    c.isGenerating = false;
                    Debug.Log("Water mesh updated " + c.gridPosition);
                }
            }
            else
            {
                logError("water wtf? c.chunkMeshGenerator != null = " + (c.chunkMeshGenerator != null));
                waterMeshUpdateInProgressList.RemoveAt(currentWaterMeshCheckTicker);
            }
        }
    }

    private void handleMeshQueue() 
	{
		

        if (meshUpdateQueue.Count > 0) {
            Chunk gen = meshUpdateQueue.Dequeue();
            
			if (ChunkMeshGenerator.Threads< SystemInfo.processorCount && gen.meshUpdateRequired && gen.chunkMeshGenerator==null){
                //lock (gen.chunkLock)
                {
                    gen.isGenerating = true;
                    gen.meshUpdateRequired = false;
                    ChunkMeshGenerator generator = new ChunkMeshGenerator(gen);
                    gen.chunkMeshGenerator = generator;
                   
                    if (gen.chunkMeshGenerator == null)
                        logError("wtf2?");
                    Thread t = new Thread(generator.Generate);
                    t.Start ();
                    meshUpdateInProgressList.Add(gen);

                }
            } else {
                //if ((gen.xposVoxels != null && gen.yposVoxels != null && gen.zposVoxels != null && gen.xnegVoxels != null && gen.ynegVoxels != null && gen.znegVoxels != null))
                //    logError("Shit was null!");

				meshUpdateQueue.Enqueue (gen);
			}
		}
		
		if (meshUpdateInProgressList.Count > 0) {
			currentMeshCheckTicker++;
			if (currentMeshCheckTicker > (meshUpdateInProgressList.Count - 1))
				currentMeshCheckTicker = 0;
			Chunk c = meshUpdateInProgressList [currentMeshCheckTicker];
			if (c != null && c.chunkMeshGenerator != null) {
				if (c.chunkMeshGenerator.isDone) {
					c.getChunkMesh ().Clear ();
					c.getChunkMesh ().vertices = c.chunkMeshGenerator.getVertices ();
					c.getChunkMesh ().uv = c.chunkMeshGenerator.getUVs ();
					c.getChunkMesh ().triangles = c.chunkMeshGenerator.getTriangles ();
					c.getChunkMesh ().RecalculateBounds ();
					c.getChunkMesh ().RecalculateNormals ();
					c.getChunkMesh ().RecalculateTangents ();
					                
					

                    c.getChunkWaterMesh().Clear();
                    c.getChunkWaterMesh().vertices = c.chunkMeshGenerator.getWaterVertices();
                    c.getChunkWaterMesh().uv = c.chunkMeshGenerator.getWaterUVs();
                    c.getChunkWaterMesh().triangles = c.chunkMeshGenerator.getWaterTriangles();
                    c.getChunkWaterMesh().RecalculateBounds();
                    c.getChunkWaterMesh().RecalculateNormals();
                    c.getChunkWaterMesh().RecalculateTangents();               
                    c.setChunkWaterMesh(c.getChunkWaterMesh());//this just looks wrong, Hah!

                    
                    c.ChunkGO.GetComponent<MeshCollider>().sharedMesh = c.getChunkMesh();
					c.chunkMeshGenerator = null;
                    c.lastUpdateTime = Time.realtimeSinceStartup;
					meshUpdateInProgressList.Remove(c);
					c.isGenerating = false;
					totalMeshesGenerated++;
				}
			} else {
				logError("wtf? c.chunkMeshGenerator != null = "+(c.chunkMeshGenerator != null));
			}
		}


    }

    private bool chunkHasAllNeighbors(Chunk c)
    {
        if (c.xposNeighbor == null || c.zposNeighbor == null || c.xnegNeighbor == null || c.znegNeighbor == null)
            return false;
        return true;
    }

	private void handleGenerationQueue()
	{

            if (generationQueue.Count > 0)
            {
                Chunk gen = generationQueue.Dequeue();
			if ((ChunkTerrainGenerator.Threads < 2))
				//GlobalConfig.Config_ChunkLODDistance * GlobalConfig.Config_ChunkLODDistance))
                {
					

                    ChunkTerrainGenerator generator = new ChunkTerrainGenerator(gen);
                    gen.chunkTerrainGenerator = generator;
                    Thread t = new Thread(generator.Generate);
					Vector3Int dif = new Vector3Int(gen.gridPosition.x,gen.gridPosition.y,gen.gridPosition.z);
                    dif.x -= (int)CameraLocation.x;
                    dif.y -= (int)CameraLocation.y;
                    dif.z -= (int)CameraLocation.z;
                    if (dif.y == 0)
					t.Priority = System.Threading.ThreadPriority.BelowNormal;
                    else if (dif.y <= 1 && dif.y >= -1)
					t.Priority = System.Threading.ThreadPriority.Lowest;
                    else
                        t.Priority = System.Threading.ThreadPriority.Lowest;
                    generationInProgressList.Add(gen);
                    t.Start();
                }
                else
                {
                    if (!chunkHasAllNeighbors(gen))
                    {
                        setupChunkNeighbors(gen);
                     //   gen.requestNeighborUpdates();
                    }
                    generationQueue.Enqueue(gen);
                }
            }
        
		if(generationInProgressList.Count>0)
		{
			Chunk c = generationInProgressList [++currentGeneratorCheckTicker % generationInProgressList.Count];
			if (c != null && c.chunkTerrainGenerator.isDone) {
				c.chunkTerrainGenerator = null;
				generationInProgressList.RemoveAt (currentGeneratorCheckTicker % generationInProgressList.Count);
				meshUpdateQueue.Enqueue (c);
				c.meshUpdateRequired = true;
			}
		}

	}

	public void createChunkAtGrid(Vector3Int grid)
	{
        lock (terrainChunksLock)
        {
            grid.y = 0;
            Chunk c = new Chunk(grid);          
            //terrainChunks.Add(c);
			terrainChunksDict.Add(grid, c);
            setupChunkNeighbors(c);
            generationQueue.Enqueue(c);
        }
	}

	public void setupChunkNeighbors(Chunk c)
	{
		Vector3Int xposNGrid = c.gridPosition;
		Vector3Int xnegNGrid = c.gridPosition;
		Vector3Int zposNGrid = c.gridPosition;
		Vector3Int znegNGrid = c.gridPosition;

		xposNGrid.x += 1;
		xnegNGrid.x -= 1;
		zposNGrid.z += 1;
		znegNGrid.z -= 1;

		if(c.xposNeighbor==null)
		c.xposNeighbor = TerrainManager.getSingleton ().getChunkByGrid (xposNGrid);
		if(c.xnegNeighbor==null)
		c.xnegNeighbor = TerrainManager.getSingleton ().getChunkByGrid (xnegNGrid);
		if(c.zposNeighbor==null)
		c.zposNeighbor = TerrainManager.getSingleton ().getChunkByGrid (zposNGrid);
		if(c.znegNeighbor==null)
		c.znegNeighbor = TerrainManager.getSingleton ().getChunkByGrid (znegNGrid);

		if (c.xposNeighbor != null) {
			c.xposNeighbor.xnegNeighbor = c;
		}
		if (c.xnegNeighbor != null) {
			c.xnegNeighbor.xposNeighbor = c;
		}
		if (c.zposNeighbor != null) {
			c.zposNeighbor.znegNeighbor = c;
		}
		if (c.znegNeighbor != null) {
			c.znegNeighbor.zposNeighbor = c;
		}

	}

	public void Test()
	{
		//terrainChunks.Add (new Chunk ());
	}

	public Vector3Int worldPositionToChunkGrid(Vector3 point)
	{
		Vector3Int grid = Vector3Int.zero();
		//Debug.Log (point);
		point/=GlobalConfig.Config_ChunkDimensionality;
		if(point.x<0)point.x-=1;
		if(point.y<0)point.y-=1;
		if(point.z<0)point.z-=1;
		grid.x=(int)point.x;
		grid.y=0;
		grid.z=(int)point.z;
		//Debug.Log (point+" "+grid);
		return grid;
	}

	public Chunk getChunkByGrid(Vector3Int g)
	{

			Chunk c = null;
			terrainChunksDict.TryGetValue (g, out c);
			return c;//might not be obvious, but c doesn't get set away from null unless TryGetValue returns true, so no if needed.
				

	}




	public Chunk getChunkByWorldPosition(Vector3 pos)
	{ 
		Vector3Int posi = worldPositionToChunkGrid(pos);
		return getChunkByGrid (posi);
	}

	public  Queue<Chunk> getFluidDynamicsUpdateQueue()
	{
		return  fluidDynamicsUpdateQueue;
	}

	public void addChunkToFluidDynamicsUpdateQueue(Chunk c)
	{
		fluidDynamicsUpdateQueue.Enqueue (c);
	}

    public void addDynamicsProcessorToGPURequestList(ChunkDynamicsProcessor CDP)
    {
        lock (fluidDynamicsGPUUpdateListLock)
        {
            fluidDynamicsGPUUpdateList.Add(CDP);
        }
    }
    
    public void doPerFrameUpdate()
    {
        lock (fluidDynamicsGPUUpdateListLock)
        {
            for (int i = 0; i < fluidDynamicsGPUUpdateList.Count; i++)
            {
                ChunkDynamicsProcessor CDS = fluidDynamicsGPUUpdateList[i];
                if (CDS.perFrameGPURequestTest())
                    fluidDynamicsGPURemoveList.Add(CDS);

            }
            for (int i = 0; i < fluidDynamicsGPURemoveList.Count; i++)
            {
                waterMeshUpdateQueue.Enqueue(fluidDynamicsGPURemoveList[i].getParent());
                fluidDynamicsGPUUpdateList.Remove(fluidDynamicsGPURemoveList[i]);

            }
            fluidDynamicsGPURemoveList.Clear();
            }

    }

}
