using UnityEngine;
using System.Collections;


/// <summary>
/// Maintains vertices of <see cref="LineRenderer"/> at positions of nesting game object and destination game object.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class DrawALine : MonoBehaviour
{
    public GameObject destination;

    LineRenderer lr;
    // Use this for initialization
    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        Update();
    }

    // Update is called once per frame
    void Update()
    {
        lr.SetPosition(0, transform.position);
        lr.SetPosition(1, destination.transform.position);
    }
}
