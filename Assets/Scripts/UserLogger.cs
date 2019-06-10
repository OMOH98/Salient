
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

public class UserLogger : MonoBehaviour, Tank.Logger
{
    public InputField partialLog;
    public Text fullLog;
    public int messagesToDisplay = 10;

    private List<string> messages;
    private float initialTime;
    private int previousCount = 0;
    private StringBuilder sb;

    void Start()
    {
        partialLog.readOnly = true;
        sb = new StringBuilder();
        initialTime = Time.time;
        messages = new List<string>();
        previousCount = messages.Count;
    }

    private void Update()
    {
        if(messages.Count>previousCount)
        {
            sb.Clear();
            sb.Append(partialLog.text);
            for (int i = previousCount; i < messages.Count; i++)
            {
                fullLog.text += string.Format("[{0:F1}] {1}\n", Time.time - initialTime, messages[i]);
                sb.Remove(sb.Length - messages[i].Length, messages[i].Length);
                sb.Insert(0, messages[i] + "\n-------------------------------------------------------\n");
            }
            previousCount = messages.Count;
            partialLog.text = sb.ToString();
        }
    }

    public void Log(string msg)
    {
        messages.Add(msg);
    }
}