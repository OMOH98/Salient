using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Provides an ability to store and display log messages to user. Is used by <see cref="Tank"/>
/// </summary>
public class UserLogger : MonoBehaviour, Tank.Logger
{
    public InputField partialLog;

    public InputField fullLog;
    public float initialTime;
    //public int maxSymbolsToDisplay = 1000;
    public int messagesToDisplay = 10;

    public const string logUpdateFrequency = nameof(logUpdateFrequency);
    public const string logFileName = "UserTankLog.txt";


    private List<string> messages = new List<string>();
    private List<float> timestamps = new List<float>();

    private float updatePeriod;
    private int previousCount = 0;
    private StringBuilder sb;

    void Start()
    {
        partialLog.readOnly = true;
        initialTime = Time.time;
        sb = new StringBuilder();

        if (Options.TryGetOption(logUpdateFrequency, out float up))
            updatePeriod = up;
        if (fullLog != null)
            fullLog.text = "";
    }

    float nextTimeToUpdate = 0f;
    private void Update()
    {
        if (Time.time >= nextTimeToUpdate)
        {
            sb.Clear();
            for (int i = previousCount; i < messages.Count; i++)
            {
                sb.AppendFormat("[Time: {0:F2}] {1}\n", timestamps[i], messages[i]);
            }
            var s = sb.ToString();
            if (fullLog != null)
                fullLog.text += s;
            File.AppendAllText(logFileName, s);
            previousCount = messages.Count;


            sb.Clear();
            for (int i = messages.Count - 1; i >= 0 && i > messages.Count - messagesToDisplay; i--)
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
        timestamps.Add(Time.time - initialTime);
    }
}