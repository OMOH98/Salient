using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

[RequireComponent(typeof(Tank))]
public class EditedTank : MonoBehaviour
{
    public const string scriptNames = "scriptNames";
    public const string autosavedScript = "asvScript";
    public const char scriptSeparator = ';';
    public const string scriptExtention = ".js";
    public const string scriptNameTaboo = ";:?,.";
    public const string sceneMenu = "Menu";
    public const string sceneSimulation = "WorkingScene";

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
    public RectTransform deathCanvas;
    public Button invisibilityButton;
    [Header("Example scripts")]
    public List<TextAsset> exampleScripts;

    private Tank tank;
    private UserLogger logger;

    protected void Awake()
    {
        if (staticExamples == null)
            staticExamples = exampleScripts;

    }

    protected void Start()
    {
        logger = new UserLogger(logField);
        tank = GetComponent<Tank>();
        tank.StartScripting(logger, true);
        StaticStart();

        StartCoroutine(FlexPanel.DelayAction(0.1f, FillInScriptTemplate));
    }

    public void FillInScriptTemplate()
    {
        if (codeField.text.Length <= 3)
            codeField.text = scriptTemplate.text;
    }
    int prevLength = 0;
    public void ClearScriptCR()
    {
        if (codeField.text.Length - prevLength > 3)
        {
            var sb = new StringBuilder(codeField.text);
            sb.Replace("\r", "");
            codeField.text = sb.ToString();
        }
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
            if (names.IndexOf(names[i]) == i)
            {
                tail.AppendFormat("{0}{1}", names[i], scriptSeparator);
            }
        }
        PlayerPrefs.SetString(scriptNames, tail.ToString());
    }
    public static void AddScriptNames(IEnumerable<string> toAdd)
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

    public static List<TextAsset> staticExamples = null;
    public static void PopulateSavedScriptDropdown(TMP_Dropdown loadDropdown)
    {
        loadDropdown.ClearOptions();
        if (!PlayerPrefs.HasKey(scriptNames))
            return;
        var names = GetScriptNames();
        var ops = loadDropdown.options;
        foreach (var n in names)
        {
            ops.Add(new TMP_Dropdown.OptionData(n));
        }
        foreach (var item in staticExamples)
        {
            ops.Add(new TMP_Dropdown.OptionData("i.e. " + item.name));
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
                if (scriptNameTaboo.Contains(c.ToString()))
                {
                    ret = false;
                    break;
                }
            }
        }
        return ret;
    }

    public static void SaveScript(string name, string code, Tank.Logger logger)
    {
        var n = name;
        if (IsValidFileName(n))
        {
            try
            {
                AddScriptNames(new string[] { n });
                StringBuilder sb = new StringBuilder(code);
                sb.Replace("\r", "");
                File.WriteAllText(n + scriptExtention, sb.ToString());
                logger.Log($"Current script is successfuly saved as \"{n}\"!");
                RepopulateDropdowns();
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

    public static List<TMP_Dropdown> toRepopulate = new List<TMP_Dropdown>();
    private static void RepopulateDropdowns()
    {
        for (int i = 0; i < toRepopulate.Count; i++)
        {
            try
            {
                PopulateSavedScriptDropdown(toRepopulate[i]);
            }
            catch
            {
                toRepopulate.RemoveAt(i);
            }
        }
    }
    public static string LoadScript(string requestedName, Tank.Logger logger)
    {

        var names = GetScriptNames();
        if (names.Contains(requestedName))
        {
            string content = "";
            try
            {
                var sb = new StringBuilder(File.ReadAllText(requestedName + scriptExtention));
                sb.Replace("\r", "");
                content = sb.ToString();
            }
            catch (IOException e)
            {
                logger.Log($"Error has occured while loading \"{requestedName}\" script with message \"{e.Message}\". The name would be deleted from selection dropdown.");
                RemoveScriptName(requestedName);
                RepopulateDropdowns();
                return "";
            }
            logger.Log($"Script \"{requestedName }\" was successfuly loaded!");
            return content;
        }
        else
        {
            foreach (var item in staticExamples)
            {
                if (requestedName.Contains(item.name))
                {
                    logger.Log($"Example script \"{item.name}\" was successfuly loaded!");
                    return item.text;
                }
            }
        }
        return "";
    }
    #endregion

    public void Flash()
    {
        tank.code = codeField.text;
        tank.RestartScripting();
    }


    private class DummyBehaviour : MonoBehaviour { }
    public void Exit()
    {
        deathCanvas.SetParent(null);

        var canvases = GetComponentsInChildren<Canvas>();
        foreach (var item in canvases)
        {
            item.gameObject.SetActive(false);
        }

        PlayerPrefs.SetString(autosavedScript, codeField.text);
        deathCanvas.gameObject.SetActive(true);
        var b = deathCanvas.gameObject.AddComponent<DummyBehaviour>();
        float startTime = Time.time;

        b.StartCoroutine(FlexPanel.DelayActionWhile(() =>
        {
            var s = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneMenu);
            if (s == null || s.buildIndex < 0)
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(s.name);

        },
        () =>
        {
            return Time.time < startTime + 2f || !Input.anyKey;
        }));
    }

    protected void StaticStart()
    {

        if (PlayerPrefs.HasKey(autosavedScript))
        {
            var lastCode = PlayerPrefs.GetString(autosavedScript);
            codeField.text = lastCode;
            logger.Log("Autosaved script was successfuly recovered");
        }

        saveAsButton.onClick.AddListener(() =>
        {
            SaveScript(saveAsNameField.text, codeField.text, logger);
            PopulateSavedScriptDropdown(loadDropdown);
        });
        loadButton.onClick.AddListener(() =>
        {
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
        toRepopulate.Add(loadDropdown);

        var t = invisibilityButton.GetComponentInChildren<Text>();
        invisibilityButton.onClick.AddListener(() =>
        {
            tank.ToggleIdVisibility();
            
            if (t != null)
            {
                if (tank.SideId() == Tank.invisibleId)
                {
                    t.text = "Set me visible";
                }
                else
                {
                    t.text = "Set me invisible";
                }
            }
        });
        do
        {
            invisibilityButton.onClick.Invoke();
        } while (tank.SideId() == Tank.invisibleId);
    }

    protected virtual void Update()
    {
        healthBar.value = tank.healthCare.Health01();
        heatBar.value = tank.heat;
    }

    public class UserLogger : Tank.Logger
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


