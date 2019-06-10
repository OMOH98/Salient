
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

public class UserLogger : MonoBehaviour, Tank.Logger
{
    public InputField partialLog;
    public Text fullLog;
    public int maxSymbolsToDisplay = 1000;

    private List<string> messages = new List<string>();
    private float initialTime;
    private int previousCount = 0;
    private StringBuilder sb;

    void Start()
    {
        partialLog.readOnly = true;
        sb = new StringBuilder();
        initialTime = Time.time;
        previousCount = 0;

        fullLog.text = "";
    }

    private void Update()
    {
        if(messages.Count>previousCount)
        {
            sb.Clear();
            sb.Append(partialLog.text);
            for (int i = previousCount; i < messages.Count; i++)
            {
                fullLog.text += string.Format("[Time: {0:F1}] {1}\n", Time.time - initialTime, messages[i]);
                sb.Insert(0, messages[i] + "\n-------------------------------------------------------\n");
            }
            previousCount = messages.Count;
            if (sb.Length > maxSymbolsToDisplay)
                sb.Remove(maxSymbolsToDisplay, sb.Length - maxSymbolsToDisplay);
            partialLog.text = sb.ToString();
        }
    }

    public void Log(string msg)
    {
        messages.Add(msg);
    }
}