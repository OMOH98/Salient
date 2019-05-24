using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditedTank : Tank
{
    [Header("UI")]
    public TMPro.TMP_InputField codeField;
    public TMPro.TMP_InputField logField;
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        this.logger = new UserLogger(logField);
        System.Action<string> logger = this.logger.Log;
        engine.SetGlobalFunction("log", logger);
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
