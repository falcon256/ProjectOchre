using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    private static GameManager gameManagerSingleton = null;
    public ComputeShader terrainFlowComputeShader = null;
	private TerrainManager terrainManager = null;

    public GameManager()
    {
        gameManagerSingleton = this;
    }

    public static GameManager getSingleton()
    {
        return gameManagerSingleton;
    }

	// Use this for initialization
	void Start () {
        GlobalConfig.Config_UseComputeShaders = SystemInfo.supportsComputeShaders;
        if (!GlobalConfig.Config_UseComputeShaders)
            Debug.LogError("Compute shaders not supported on this platform.");

        terrainManager = TerrainManager.getSingleton ();

	}



    // Update is called once per frame
    public void Update()
    {
        terrainManager.doPerFrameUpdate();
    }




    void FixedUpdate () {
		if (terrainManager != null) {
			terrainManager.doUpdate ();
		} else {
			Debug.LogError("terrainManager was null on GameManager Update()");
		}
	}
	void OnApplicationQuit()
	{
		terrainManager.abortAllThreads = true;
	}

}
