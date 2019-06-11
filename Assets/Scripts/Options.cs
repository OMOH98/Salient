using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Governs UI that displays global game options and persistent storing of said options. Provides static interface for getting an option by name. 
/// </summary>
public class Options : MonoBehaviour
{
    public GameObject itemsContent;
    public GameObject optionPrefab;
    public string optionFieldName = "InputField";
    public string optionCaptionName = "Caption";
    public List<Option> options;



    public const string prefix = "opt";


    List<RenderedOption> renderedOptions;

    #region EventCallbacks

    private void Awake()
    {
        options.AddRange(ConstantOptions.options);
        FetchValues(options);

        if (staticOptions == null)
        {
            staticOptions = new Dictionary<string, Option>(options.Count);
        }
        else staticOptions.Clear();
        for (int i = 0; i < options.Count; i++)
        {
            staticOptions.Add(options[i].name, options[i]);
        }

    }

    private void OnDestroy()
    {
        foreach (var o in options)
        {
            PlayerPrefs.SetFloat(prefix + o.name, o.value);
        }
    }



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

    #endregion

    #region StaticInterface
    private static void FetchValues(IEnumerable<Option> options)
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

    static Dictionary<string, Option> staticOptions;
    public static bool TryGetOption(string optionName, out float value)
    {
        if(staticOptions==null)
        {
            FetchValues(ConstantOptions.options);
            staticOptions = new Dictionary<string, Option>(ConstantOptions.options.Count);
            for (int i = 0; i < ConstantOptions.options.Count; i++)
            {
                var item = ConstantOptions.options[i];
                staticOptions.Add(item.name, item);
            }
        }

        if (staticOptions.ContainsKey(optionName))
        {
            value = staticOptions[optionName].value;
            return true;
        }
        value = 0f;
        return false;
    }
    #endregion

    #region Nested
    public static class ConstantOptions
    {
        public static List<Option> options;
        static ConstantOptions()
        {
            options = new List<Option>()
            {
                new Option(){defaultValue = 1000f, name = nameof(Tank.recursionDepth), value = 1000f},
                new Option(){defaultValue = 1f, name = nameof(Tank.physicsFramesToExecuteLoop), value = 1f},
                new Option(){defaultValue = 0.5f, name = nameof(UserLogger.logUpdateFrequency), value = 1f},
            };
            foreach (var item in options)
            {
                item.value = item.defaultValue;
            }
        }
    }

    class RenderedOption
    {
        public int optionInx;
        public InputField field;
        public Text caption;
    }

    [System.Serializable]
    public class Option
    {
        public string name;
        public float value;
        public float defaultValue;
    }

    #endregion

    public static string NicifyOptionName(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "No name";

        var sb = new StringBuilder(input);
        sb[0] = char.ToUpper(sb[0]);
        for (int i = 1; i < sb.Length; i++)
        {
            if(char.IsUpper(sb[i]))
            {
                sb[i] = char.ToLower(sb[i]);
                sb.Insert(i, ' ');
            }
        }
        return sb.ToString();
    }

    private void RenderValues()
    {
        
        foreach (var ro in renderedOptions)
        {
            var item = options[ro.optionInx];
            ro.field.text = string.Format("{0:F2}", item.value);
            ro.caption.text = NicifyOptionName(item.name)+": ";
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
}
