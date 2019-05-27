using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class DocumentationRenderer : MonoBehaviour
{
    public TMP_Dropdown selector;
    public TMP_Text text;
    public Image image;
    public List<DocPart> parts;

    private HeightExpander expander;
    // Use this for initialization
    void Start()
    {
        expander = image.GetComponent<HeightExpander>();

        PopulateDropdown();
        ShowDoc(selector.value);
        selector.onValueChanged.AddListener(inx => ShowDoc(inx));
    }

    protected void PopulateDropdown()
    {
        selector.options.Clear();
        foreach (var item in parts)
        {
            selector.options.Add(new TMP_Dropdown.OptionData(item.name));
        }
        selector.value = -1;
        selector.value = 0;
    }

    public void ShowDoc(int index)
    {
        text.text = parts[index].text.text;
        image.sprite = parts[index].image;
        expander.RefreshHeight();
    }

    [System.Serializable]
    public class DocPart
    {
        public string name;
        public TextAsset text;
        public Sprite image;
    }
}
