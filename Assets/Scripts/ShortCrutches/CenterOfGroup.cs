using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class CenterOfGroup : MonoBehaviour
{
    public List<GameObject> group;
    private void Awake()
    {
        if (group == null)
            group = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (group == null || group.Count == 0)
            return;

        while (group[0] == null)
        {
            group.RemoveAt(0);
            if (group.Count == 0)
                return;
        }
        
        var pos = group[0].transform.position;
        for(int i=1; i<group.Count; i++)
        {
            if (group[i] == null)
            {
                group.RemoveAt(i--);
                continue;
            }
            pos = pos + (group[i].transform.position - pos) * 0.5f;
        }
        transform.position = pos;
    }
}
