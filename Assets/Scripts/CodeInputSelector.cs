using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;


[RequireComponent(typeof(InputField))]
public class CodeInputSelector : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public bool enableHotkey = true;
    public UnityEvent OnSelectField;
    public UnityEvent OnDeselectField;

    InputField field;
    bool selected = false;
    Color caretColor;
    private static Selectable dummySelectable;
    // Start is called before the first frame update
    void Start()
    {

        field = GetComponent<InputField>();
        caretColor = field.caretColor;
        if (dummySelectable == null)
        {
            var go = new GameObject("DummySelectable");
            dummySelectable = go.AddComponent<Selectable>();
        }
    }
    int lastCaretPosition;
    void ISelectHandler.OnSelect(BaseEventData eventData)
    {
        StartCoroutine(FlexPanel.DelayAction(0f, () => {
            field.caretPosition = field.selectionFocusPosition = field.selectionAnchorPosition = lastCaretPosition;
        }));
        selected = true;
        field.caretColor = caretColor;
        OnSelectField.Invoke();
    }
    void IDeselectHandler.OnDeselect(BaseEventData eventData)
    {
        field.caretColor = Color.clear;
        selected = false;
        OnDeselectField.Invoke();
    }

    void Update()
    {
        if (field.caretPosition != 0)
            lastCaretPosition = field.caretPosition;
        if (enableHotkey && Input.GetButtonDown("Code") && field != null)
        {
            if (!selected)
                field.Select();
            else dummySelectable.Select();
        }
    }
}
