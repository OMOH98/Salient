using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;


[RequireComponent(typeof(InputField))]
public class CodeInputSelector : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public UnityEvent OnSelectField;
    public UnityEvent OnDeselectField;

    InputField field;
    bool selected = false;
    Color caretColor;
    private Selectable dummySelectable;
    static int count = 0;
    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (prev, curr) => { count = 0; };
        count++;
        if(count>1)
        {
            Debug.LogWarning($"More than one {nameof(CodeInputSelector)} detected on scene. Works oly one.");
        }

        field = GetComponent<InputField>();
        caretColor = field.caretColor;
        var go = new GameObject("DummySelectable");
        dummySelectable = go.AddComponent<Selectable>();
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
        if (Input.GetButtonDown("Code") && field != null)
        {
            if (!selected)
                field.Select();
            else dummySelectable.Select();
        }
    }
}
