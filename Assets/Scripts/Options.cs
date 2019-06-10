using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    public GameObject itemsContent;
    public GameObject optionPrefab;
    public string optionFieldName = "InputField";
    public string optionCaptionName = "Caption";
    public List<Option> options;

    public const string prefix = "opt";

    private void Awake()
    {
        foreach (var item in options)
        {
            var k = prefix + item.name;
            if (PlayerPrefs.HasKey(k))
            {
                item.value = PlayerPrefs.GetFloat(k);
            }
            else item.value = item.defaultValue;
        }
    }

    private void OnApplicationQuit()
    {
        foreach (var o in options)
        {
            PlayerPrefs.SetFloat(prefix + o.name, o.value);
        }
    }

    class RenderedOption
    {
        public int optionInx;
        public InputField field;
        public Text caption;

    }
    List<RenderedOption> renderedOptions;
    // Start is called before the first frame update
    void Start()
    {
        renderedOptions = new List<RenderedOption>(options.Count);
        for (int i = 0; i < options.Count; i++)
        {
            var item = options[i];
            var o = Instantiate(optionPrefab);
            o.transform.SetParent(itemsContent.transform);
            o.SetActive(transform);
            RenderedOption ro = new RenderedOption()
            {
                field = o.transform.Find(optionFieldName).GetComponent<InputField>(),
                optionInx = i,
                caption = o.transform.Find(optionCaptionName).GetComponent<Text>()
            };

            ro.field.onEndEdit.AddListener((strparam) =>
            {
                float v = options[ro.optionInx].value;
                if (float.TryParse(ro.field.text, out v))
                    options[ro.optionInx].value = v;
            });
            renderedOptions.Add(ro);
        }
        RenderValues();
    }

    private void RenderValues()
    {
        foreach (var ro in renderedOptions)
        {
            var item = options[ro.optionInx];
            ro.field.text = string.Format("{0:F2}", item.value);
            ro.caption.text = item.name;
        }
    }

    public void ResetToDefaults()
    {
        foreach (var item in options)
        {
            item.value = item.defaultValue;
        }
        RenderValues();
    }

    [System.Serializable]
    public class Option
    {
        public string name;
        public float value;
        public float defaultValue;
    }
}
