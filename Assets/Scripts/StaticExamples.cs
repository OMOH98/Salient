using UnityEngine;

using System.Collections;
using System.Collections.Generic;

public class StaticExamples : MonoBehaviour
{
    public List<TextAsset> examples;

    private void Awake()
    {
        if (list == null)
            list = examples;
    }

    public static List<TextAsset> list { get; private set; }
}
