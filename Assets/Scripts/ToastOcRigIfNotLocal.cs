using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ToastOcRigIfNotLocal : NetworkBehaviour {

    public GameObject OcStuff = null;
    public Camera mc = null;
    public Camera mc1 = null;
    public Camera mc2 = null;
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (!isLocalPlayer)
        {
            if (OcStuff != null)
                Destroy(OcStuff);
        }
        else
        {
            if (mc1.tag!="MainCamera")
            {
                mc1.tag = "MainCamera";
                mc1.enabled = true;             
            }
            if(mc2.tag!="MainCamera")
            {
                mc2.tag = "MainCamera";
                mc2.enabled = true;
            }
        }
    }
}
