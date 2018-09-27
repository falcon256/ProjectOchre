using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderEnabler : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		Vector3 dist = transform.position - TerrainManager.getSingleton ().CameraLocation;
		if (dist.magnitude < GlobalConfig.Config_ChunkVerticalDimensionality)
			this.GetComponent<MeshCollider> ().enabled = true;
		else
			this.GetComponent<MeshCollider> ().enabled = false;
	}
}
