using UnityEngine;
using System.Collections;

using System.Linq;
using System.Text;

public class ClearScriptCR : MonoBehaviour
{
    public int treshold = 3;

    const string text = "text";
    int prevLength = 0;

    dynamic tmpField;
    //System.Reflection.PropertyInfo textProp;
    private void Start()
    {
        var components = (from c in GetComponents(typeof(Component))
                          where c.GetType().GetProperties().ToList().Find((inf) =>
                          {
                              return inf.CanRead && inf.CanWrite && inf.PropertyType == typeof(string) && inf.Name == text;
                          }) != null
                          select c).ToArray();
        if (components.Length > 0)
        {
            tmpField = components[0];
            //textProp = tmpField.GetType().GetProperty(text);
        }
    }

    bool works = true;
    private void Update()
    {
        if (!works)
            return;
        if (tmpField == null)
        {
            Debug.LogWarning($"No component with text property was found on {gameObject.name} game object");
            works = false;
            return;
        }

        if (tmpField != null && tmpField.text.Length - prevLength > treshold)
        {
            tmpField.text = ClearCR(tmpField.text);
        }
        prevLength = tmpField.text.Length;
    }
    public static string ClearCR(string input)
    {
        var sb = new StringBuilder(input);
        sb.Replace("\r", "");
        return sb.ToString();
    }
}

