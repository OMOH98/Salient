
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class UserLogger : MonoBehaviour, Tank.Logger
{
    public InputField partialLog;

    public TMP_InputField fullLog;

    //public int maxSymbolsToDisplay = 1000;
    public int messagesToDisplay = 10;

    public const string logUpdateFrequency = nameof(logUpdateFrequency);


    private List<string> messages = new List<string>();
    private float initialTime;
    private float updatePeriod;
    private int previousCount = 0;
    private StringBuilder sb;

    void Start()
    {
        partialLog.readOnly = true;
        initialTime = Time.time;
        sb = new StringBuilder();

        float up;
        if (Options.TryGetOption(logUpdateFrequency, out up))
            updatePeriod = up;
        if (fullLog != null)
            fullLog.text = "";
    }

    float nextTimeToUpdate = 0f;
    private void Update()
    {
        if(Time.time >= nextTimeToUpdate)
        {
            if (fullLog != null)
            {
                sb.Clear();
                for (int i = previousCount; i < messages.Count; i++)
                {
                    sb.AppendFormat("[Time: {0:F1}] {1}\n", Time.time - initialTime, messages[i]);
                }
                fullLog.text += sb.ToString();
                previousCount = messages.Count;
            }

            sb.Clear();
            for (int i = messages.Count-1; i>= 0 && i > messages.Count - messagesToDisplay; i--)
            {
                sb.Insert(0, messages[i] + "\n-------------------------------------------------------\n");
            }
            partialLog.text = sb.ToString();

            nextTimeToUpdate = Time.time + updatePeriod;
        }
    }

    public void Log(string msg)
    {
        messages.Add(msg);
    }
}