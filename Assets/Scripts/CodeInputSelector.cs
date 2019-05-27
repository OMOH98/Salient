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
    Color caretColor;
    // Start is called before the first frame update
    void Start()
    {
        field = GetComponent<TMP_InputField>();
        caretColor = field.caretColor;
        field.onSelect.AddListener((s) => {
            selected = true;
            field.caretColor = caretColor;
            //Debug.Log(s);
        });
        field.onDeselect.AddListener((s) =>
        {
            field.caretColor = Color.clear;
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
