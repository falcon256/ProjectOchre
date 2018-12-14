using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ToastOcRigIfNotLocal : NetworkBehaviour {

    public GameObject OcStuff = null;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if(!isLocalPlayer)
        {
            if (OcStuff != null)
                Destroy(OcStuff);
        }
	}
}
