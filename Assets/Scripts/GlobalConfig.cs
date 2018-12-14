using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalConfig {
		
	//terrain configs
	public const int Config_ChunkDimensionality = 32;
	public const int Config_ChunkVerticalDimensionality = 512;
	public static int Config_ChunkLODDistance =16;
	public static double Config_TerrainFrequencyDivisor = 128.0f;
    public static float Config_ChunkWaterUpdateDelay = 1.0f;

    //GPGPU configs
    public static bool Config_UseComputeShaders = false;


}
