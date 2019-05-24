using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EditedTank : Tank
{
    public const string scriptNames = "scriptNames";
    public const char scriptSeparator = ';';
    public const string scriptExtention = ".js";

    [Header("UI")]
    public TMP_InputField codeField;
    public TMP_InputField logField;
    public Button saveAsButton;
    public TMP_InputField saveAsNameField;
    public Button loadButton;
    public TMP_Dropdown loadDropdown;

    
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        this.logger = new UserLogger(logField);
        System.Action<string> logger = this.logger.Log;
        engine.SetGlobalFunction("log", logger);

        saveAsButton.onClick.AddListener(SaveScript);
    }


    private bool IsValidFileName(string s)
    {
        const string taboo = ";?,.";
        var ret = !string.IsNullOrEmpty(s);
        if (ret)
        {
            foreach (var c in s)
            {
                if(taboo.Contains(c.ToString()))
                {
                    ret = false;
                    break;
                }
            }
        }

        if (ret)
        {
            try
            {
                var names = PlayerPrefs.GetString(scriptNames).Split(scriptSeparator).ToList();
                names.RemoveAll((toTest) => { return string.IsNullOrEmpty(toTest); });
                foreach (var name in names)
                {
                    if (s.Equals(name, System.StringComparison.Ordinal))
                    {
                        //TODO: check if string comparison works properly
                        ret = false;
                        break;
                    }
                }
            }
            catch (PlayerPrefsException)
            {
                ;
            }
        }

        return ret;
    }
    public void SaveScript()
    {
        var n = saveAsNameField.text;
        if (IsValidFileName(n))
        {
            string names = "";
            if(PlayerPrefs.HasKey(scriptNames))
            {
                names = PlayerPrefs.GetString(scriptNames);
            }
            PlayerPrefs.SetString(scriptNames, names + n + scriptSeparator.ToString());

            File.WriteAllText(n + scriptExtention, codeField.text);
        }
        else
        {
            logger.Log("Set name is not valid either because it is already used or it contains taboo chars. Try other name.");
        }
    }

    public void ExecOnce()
    {
        try
        {
            code = codeField.text;
            compiledCode.Execute(engine);
        }
        catch (Jurassic.JavaScriptException e)
        {
            logger.Log($"JavaScript error has occured at line {e.LineNumber} with message: {e.Message}");
        }
    }

    public class UserLogger:Logger
    {
        private TMPro.TMP_InputField logField;
        public UserLogger(TMPro.TMP_InputField ui)
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


