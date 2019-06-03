using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankSpawner : MonoBehaviour, Pausable
{
    public List<Spawnpoint> spawnpoints;
    public GameObject tankPrefab;


    protected void Start()
    {
        foreach (var point in spawnpoints)
        {
            point.center.SetParent(null);
        }
    }

    private bool timeFlows = true;
    public void Resume() { timeFlows = true; }
    public void Pause() { timeFlows = false; }

    public void Spawn(int side, string code)
    {
        var tank = Instantiate(tankPrefab);
        tank.transform.SetParent(null);
        var point = spawnpoints.Find((p) => { return p.sideId == side; });
        if (point == null)
            return;

        var rnd = Random.insideUnitCircle * point.radius;
        tank.transform.SetPositionAndRotation(point.center.position + new Vector3(rnd.x, 0f, rnd.y), point.center.rotation);
        var rnds = tank.GetComponentsInChildren<MeshRenderer>();
        foreach (var item in rnds)
        {
            List<Material> lst = new List<Material>();
            for (int i = 0; i < item.materials.Length; i++)
                lst.Add(point.sideMaterial);
            item.materials = lst.ToArray();
        }

        var tankScript = tank.GetComponent<Tank>();
        tankScript.sideIdentifier = side;
        tankScript.code = code;
        
        if (!timeFlows)
        {
            tankScript.StartCoroutine(FlexPanel.DelayAction(0.2f, tankScript.Pause));
        }
            
    }

    [System.Serializable]
    public class Spawnpoint
    {
        public Transform center;
        public float radius = 50f;
        public Material sideMaterial;
        public int sideId;
        public bool attachCamera;
    }
}
