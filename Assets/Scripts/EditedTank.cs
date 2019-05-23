using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditedTank : Tank
{
    [Header("UI")]
    public InputField codeField;
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
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
}
