using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public interface CamTarget
{
    bool IsInteresting();
}

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
            var prevInx = currentInx;
            bool interesting = false;
            do
            {
                currentInx++;
                if (currentInx == targets.Count)
                {
                    currentInx = 0;
                }
                interesting = true;//sic! if no CamTarget attached then GO is considered interesting (i.e. UBT)
                var t = targets[currentInx].GetComponent<CamTarget>();
                if (t != null)
                    interesting = t.IsInteresting();
            } while (currentInx != prevInx && !interesting);
            if (currentInx == prevInx)
                base.Update();
            else
            {
                player = targets[currentInx];
                followPlayer = false;
                followPlayer = true;
            }
        }
        else base.Update();
    }

    public void UpdateTargets(IEnumerable<GameObject> newTargets)
    {
        var set = new HashSet<GameObject>(newTargets);
        for (int i = 0; i < targets.Count; i++)
        {
            if (set.Contains(targets[i]))
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
