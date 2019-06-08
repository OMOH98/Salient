using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MultipleCamControl : CamControl
{
    public List<GameObject> targets;
    int currentInx = 0;

    protected override void Start()
    {
        if (targets.Count > 0)
            base.player = targets[currentInx];
        base.Start();
    }
    // Update is called once per frame
    protected override void Update()
    {
        if (player == null && targets.Count > 0)
            player = targets[0];
        
        if (readInput && followPlayer && Input.GetButtonDown("Follow"))
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

    public void UpdateTargets(IEnumerable<GameObject> newTargets)
    {
        var set = new HashSet<GameObject>(newTargets);
        for (int i = 0; i < targets.Count; i++)
        {
            if(set.Contains(targets[i]))
            {
                set.Remove(targets[i]);
            }
            else
            {
                targets.RemoveAt(i--);
            }
        }
        targets.AddRange(set);
        player = null;
    }
}
