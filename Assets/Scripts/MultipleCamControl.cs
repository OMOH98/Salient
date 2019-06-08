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
        base.Update();
        if (Input.GetButtonDown("Follow"))
        {
            currentInx++;
            if(currentInx == targets.Count)
            {
                currentInx = -1;
            }
            else
            {
                player = targets[currentInx];
                followPlayer = true;
            }
        }
    }
}
