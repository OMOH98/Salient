using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TMP_InputField))]
public class CodeInputSelector : MonoBehaviour
{
    TMP_InputField field;
    bool selected = false;
    // Start is called before the first frame update
    void Start()
    {
        field = GetComponent<TMP_InputField>();
        field.onSelect.AddListener((s) => {
            selected = true;
            Debug.Log(s);
        });
        field.onDeselect.AddListener((s) =>
        {
            selected = false;
        });
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Code") && field!=null)
        {
            if (!selected)
                field.Select();
            else EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
