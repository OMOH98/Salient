using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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
    public InputField codeField;
    public InputField logField;
    public Button saveAsButton;
    public InputField saveAsNameField;
    public Button loadButton;
    public Dropdown loadDropdown;
    public ProgressBar healthBar;
    public ProgressBar heatBar;
    public RectTransform deathCanvas;
    public Button invisibilityButton;
    public Button scriptPlayPauseButton;

    private Tank tank;
    private UserLogger logger;

    protected void Start()
    {
        logger = new UserLogger(logField);
        tank = GetComponent<Tank>();
        tank.StartScripting(logger);
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

    public static void PopulateSavedScriptDropdown(Dropdown loadDropdown)
    {
        loadDropdown.ClearOptions();
        var ops = loadDropdown.options;
        if (PlayerPrefs.HasKey(scriptNames))
        {
            var names = GetScriptNames();
            foreach (var n in names)
            {
                ops.Add(new Dropdown.OptionData(n));
            }
        }
        foreach (var item in StaticExamples.list)
        {
            ops.Add(new Dropdown.OptionData("i.e. " + item.name));
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

    public static List<Dropdown> toRepopulate = new List<Dropdown>();
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
            foreach (var item in StaticExamples.list)
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
    private void OnApplicationQuit()
    {
        PlayerPrefs.SetString(autosavedScript, codeField.text);
    }
    public void Exit()
    {
        deathCanvas.SetParent(null);

        var canvases = GetComponentsInChildren<Canvas>();
        foreach (var item in canvases)
        {
            item.gameObject.SetActive(false);
        }


        deathCanvas.gameObject.SetActive(true);
        var b = deathCanvas.gameObject.AddComponent<DummyBehaviour>();
        float startTime = Time.time;
        OnApplicationQuit();
        b.StartCoroutine(FlexPanel.DelayActionWhile(() =>
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneMenu);
        },
        () =>
        {
            return Time.time < startTime + 2f || !Input.anyKey;
        }));
    }

    protected void StaticStart()
    {
        deathCanvas.gameObject.SetActive(false);
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
            if (loadDropdown.value < 0 || loadDropdown.options.Count <= 0)
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

        var invBtnText = invisibilityButton.GetComponentInChildren<Text>();
        invisibilityButton.onClick.AddListener(() =>
        {
            tank.ToggleIdVisibility();

            if (invBtnText != null)
            {
                if (tank.SideId() == Tank.invisibleId)
                {
                    invBtnText.text = "Set me visible";
                }
                else
                {
                    invBtnText.text = "Set me invisible";
                }
            }
        });
        do
        {
            invisibilityButton.onClick.Invoke();
        } while (tank.SideId() == Tank.invisibleId);

        var scrPlPuBtnText = scriptPlayPauseButton.GetComponentInChildren<Text>();
        bool enabled = true;
        scriptPlayPauseButton.onClick.AddListener(() =>
        {
            var rgos = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            enabled = !enabled;
            if (enabled)
            {
                scrPlPuBtnText.text = "Pause scripts";
                foreach (var go in rgos)
                {
                    var tanks = go.GetComponentsInChildren<Pausable>();
                    foreach (var t in tanks)
                    {
                        t.Resume();
                    }
                }
            }
            else
            {
                scrPlPuBtnText.text = "Resume scripts";
                foreach (var go in rgos)
                {
                    var tanks = go.GetComponentsInChildren<Pausable>();
                    foreach (var t in tanks)
                    {
                        t.Pause();
                    }
                }
            }
        });
        StartCoroutine(FlexPanel.DelayAction(0.2f, () =>
        {
            do
            {
                scriptPlayPauseButton.onClick.Invoke();
            } while (!enabled);
        }));
        
    }

    protected virtual void Update()
    {
        healthBar.value = tank.healthCare.Health01();
        heatBar.value = tank.heat;
    }

    public class UserLogger : Tank.Logger
    {
        private InputField logField;
        public UserLogger(InputField ui)
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


