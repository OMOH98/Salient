using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class EditedTank : Tank
{
    public const string scriptNames = "scriptNames";
    public const char scriptSeparator = ';';
    public const string scriptExtention = ".js";
    public const string scriptNameTaboo = ";?,.";

    [Header("UI")]
    public TextAsset scriptTemplate;
    public TMP_InputField codeField;
    public TMP_InputField logField;
    public Button saveAsButton;
    public TMP_InputField saveAsNameField;
    public Button loadButton;
    public TMP_Dropdown loadDropdown;
    public ProgressBar healthBar;
    public ProgressBar heatBar;


    protected override void Start()
    {
        StartScripting(new UserLogger(logField));

        StaticStart();

        StartCoroutine(FlexPanel.DelayAction(0.1f, FillInScriptTemplate));
    }

    public void FillInScriptTemplate()
    {
        if (codeField.text.Length <= 3)
            codeField.text = scriptTemplate.text;
    }

    #region ScriptIO
    public static List<string> GetScriptNames()
    {
        try
        {
            var names = PlayerPrefs.GetString(scriptNames).Split(scriptSeparator).ToList();
            names.RemoveAll((toTest) => { return string.IsNullOrEmpty(toTest); });
            return names;
        }
        catch (PlayerPrefsException)
        {
            return new List<string>();
        }
    }
    public static void SetScriptNames(List<string> names)
    {
        var tail = new StringBuilder();
        for (int i = 0; i < names.Count; i++)
        {
            if(names.IndexOf(names[i])==i)
            {
                tail.AppendFormat("{0}{1}", names[i], scriptSeparator);
            }
        }
        PlayerPrefs.SetString(scriptNames, tail.ToString());
    }
    public static  void AddScriptNames(IEnumerable<string> toAdd)
    {
        var names = GetScriptNames();
        names.AddRange(toAdd);
        SetScriptNames(names);
    }
    public static bool RemoveScriptName(string n)
    {
        var names = GetScriptNames();
        var ret = names.Remove(n);
        SetScriptNames(names);
        return ret;
    }
    
    public static void PopulateSavedScriptDropdown(TMP_Dropdown loadDropdown)
    {
        loadDropdown.ClearOptions();
        if (!PlayerPrefs.HasKey(scriptNames))
            return;
        var names = GetScriptNames();
        var ops = loadDropdown.options;// new List<TMP_Dropdown.OptionData>();
        foreach (var n in names)
        {
            ops.Add(new TMP_Dropdown.OptionData(n));
        }

        loadDropdown.value = 1;
        loadDropdown.value = 0;
    }

    public static bool IsValidFileName(string s)
    {

        var ret = !string.IsNullOrEmpty(s);
        if (ret)
        {
            foreach (var c in s)
            {
                if(scriptNameTaboo.Contains(c.ToString()))
                {
                    ret = false;
                    break;
                }
            }
        }
        return ret;
    }

    public static void SaveScript(string name, string code, Logger logger)
    {
        var n = name;
        if (IsValidFileName(n))
        {
            try
            {
                AddScriptNames(new string[] { n });
                File.WriteAllText(n + scriptExtention, code);
                logger.Log($"Current script is successfuly saved as \"{n}\"!");
            }
            catch (IOException e)
            {
                logger.Log($"Script saving failed due to an error risen with message \"{e.Message}\"");
            }
        }
        else
        {
            logger.Log($"Set name is not valid because it contains taboo chars \"{scriptNameTaboo}\". Try other name.");
        }
    }
    
    public static string LoadScript(string requestedName, Logger logger)
    {

        var names = GetScriptNames();
        if(names.Contains(requestedName))
        {
            string content = "";
            try
            {
                content = File.ReadAllText(requestedName + scriptExtention);
            }
            catch (IOException e)
            {
                logger.Log($"Error has occured while loading \"{requestedName}\" script with message \"{e.Message}\". The name would be deleted from selection dropdown.");
                RemoveScriptName(requestedName);
                //PopulateSavedScriptDropdown(loadDropdown);
                return "";
            }
            logger.Log($"Script \"{requestedName }\" was successfuly loaded!");
            return content;            
        }
        return "";
    }
    #endregion

    public void Flash()
    {
        code = codeField.text;
        RestartScripting();
    }

    protected override void StaticStart()
    {
        base.StaticStart();
        saveAsButton.onClick.AddListener(() => {
            SaveScript(saveAsNameField.text, codeField.text, logger);
            PopulateSavedScriptDropdown(loadDropdown);
        });
        loadButton.onClick.AddListener(()=> {
            if (!PlayerPrefs.HasKey(scriptNames) || loadDropdown.value < 0 || loadDropdown.options.Count <= 0)
            {
                logger.Log("There are no saved scripts to load");
                return;
            }
            var requestedName = loadDropdown.options[loadDropdown.value].text;
            var content = LoadScript(requestedName, logger);
            codeField.text = content;
            saveAsNameField.text = requestedName;
            PopulateSavedScriptDropdown(loadDropdown);
        });
        PopulateSavedScriptDropdown(loadDropdown);
    }

    protected virtual void Update()
    {
        healthBar.value = hb.Health01();
        heatBar.value = heat;
    }

    public class UserLogger:Logger
    {
        private TMP_InputField logField;
        public UserLogger(TMP_InputField ui)
        {
            logField = ui;
            ui.readOnly = true;
        }
        public void Log(string msg)
        {
            logField.text = msg + "\n--------------------------------------------------\n" + logField.text;
        }
    }
}


