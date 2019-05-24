using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_InputField))]
public class CodeInputSelector : MonoBehaviour
{
    TMP_InputField field;
    // Start is called before the first frame update
    void Start()
    {
        field = GetComponent<TMP_InputField>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Code") && field!=null)
        {
            field.Select();
        }
    }
}
