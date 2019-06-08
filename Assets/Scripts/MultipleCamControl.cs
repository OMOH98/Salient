using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MultipleCamControl : CamControl
{
    public List<GameObject> targets;
    int currentInx = 0;

    protected override void Start()
    {
        base.player = targets[currentInx];
        base.Start();
    }
    // Update is called once per frame
    protected override void Update()
    {
        
        if (followPlayer && Input.GetButtonDown("Follow"))
        {
                currentInx++;
                if (currentInx == targets.Count)
                {
                    currentInx = 0;
                }

                player = targets[currentInx];
                followPlayer = false;
                followPlayer = true;
        }
        else base.Update();
    }
}
