using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankSpawner : MonoBehaviour, Pausable
{
    public List<Spawnpoint> spawnpoints;
    public GameObject tankPrefab;

    Dictionary<Spawnpoint, CenterOfGroup> centres;

    protected void Start()
    {
        centres = new Dictionary<Spawnpoint, CenterOfGroup>(spawnpoints.Count);
        for (int i = 0; i < spawnpoints.Count; i++)
        {
            spawnpoints[i].center.SetParent(null);
            var c = new GameObject(spawnpoints[i].sideMaterial.name + " center");
            c.transform.SetPositionAndRotation(spawnpoints[i].center.position, Quaternion.identity);
            centres.Add(spawnpoints[i], c.AddComponent<CenterOfGroup>());
        }

        var advcam = Camera.main.GetComponent<MultipleCamControl>();
        if(advcam!=null)
        {
            advcam.targets.AddRange(from c in centres.Values select c.gameObject);
        }
    }

    private bool timeFlows = true;
    public void Resume() { timeFlows = true; }
    public void Pause() { timeFlows = false; }

    public GameObject Spawn(int side, string code = "")
    {
        var tank = Instantiate(tankPrefab);
        tank.transform.SetParent(null);
        var point = spawnpoints.Find((p) => { return p.sideId == side; });
        if (point == null)
            point = spawnpoints[0];

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
        if (string.IsNullOrEmpty(code))
            tankScript.code = point.defaultCode;
        else tankScript.code = code;

        if (!timeFlows)
        {
            tankScript.StartCoroutine(FlexPanel.DelayAction(0.2f, tankScript.Pause));
        }

        centres[point].group.Add(tank);

        return tank;
    }

    public void SpawnFromBattle(Battle info)
    {
        for (int i = 0; i < spawnpoints.Count && i < info.sides.Count; i++)
        {
            spawnpoints[i].FetchFromSide(info.sides[i]);
        }
        for (int i = 0; i < spawnpoints.Count && i<info.sides.Count; i++)
        {
            for (int j = 0; j < info.sides[i].count; j++)
            {
                Spawn(spawnpoints[i].sideId);
            }
        }
    }

    [System.Serializable]
    public class Spawnpoint
    {
        [Header("Inspector-defined")]
        public Transform center;
        public float radius = 50f;
        public Material sideMaterial;

        [Header("Auto-generated")]
        public int sideId;
        public string defaultCode;
        public bool attachCamera;

        public void FetchFromSide(Battle.Side s)
        {
            sideId = s.sideId;
            defaultCode = s.code;
            attachCamera = s.attachCamera;
        }
    }
}
