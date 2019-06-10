using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using System.Text;


/// <summary>
/// Overrides preferred width/height of an <see cref="InputField"/> so that it is certainly bigger than its text content.
/// Clears <see cref="InputField.text"/> from Carriage return chars.
/// </summary>
public class InputFieldContentSizeFitter : MonoBehaviour
{
    public float padding = 5;
    LayoutElement element;
    InputField field;
    // Use this for initialization
    void Start()
    {
        element = gameObject.GetComponent<LayoutElement>();
        if (element == null)
            element = gameObject.AddComponent<LayoutElement>();

        field = GetComponent<InputField>();
        int prevLength = field.text.Length;
        int maxWidth = 0, c = 0, linesCount = 1;
        int maxAdvance = 0;
        foreach (var inf in field.textComponent.font.characterInfo)
        {
            if (inf.advance > maxAdvance)
                maxAdvance = inf.advance;
        }
        RectTransform viewportRT = field.transform.parent.parent.GetComponent<RectTransform>();
        field.onValueChanged.AddListener((strParam) =>
        {
            var s = field.text; //just in case
            var editDistance = System.Math.Abs(s.Length - prevLength);

            if (s.Length == 0)
            {
                maxWidth = c = 0;
                linesCount = 1;
            }
            else if (editDistance > 1 || editDistance == 0)
            {
                if (editDistance > 3)
                {
                    field.text = ClearCR(field.text);
                    s = field.text;
                }
                //full check
                maxWidth = c = 0;
                linesCount = 1;
                for (int i = 0; i < s.Length; i++, c++)
                {
                    if (s[i] == '\n')
                    {
                        if (c > maxWidth)
                            maxWidth = c;
                        c = 0;
                        linesCount++;
                    }
                }

            }
            else if (field.caretPosition > 0)
            {
                //last symbol check

                var chr = s[field.caretPosition - 1];
                if (chr == '\n')
                    linesCount++;
                c = 1;
                for (int i = field.caretPosition - 1; i >= 0; i--, c++)
                {
                    if (s[i] == '\n')
                    {
                        break;
                    }
                }
                if (maxWidth < c)
                {
                    maxWidth = c;
                }
            }
            prevLength = field.text.Length;
            element.preferredWidth = System.Math.Max(maxAdvance * maxWidth + 20 + 2 * padding, viewportRT.rect.width);
            element.preferredHeight = Mathf.Max(1.2f * field.textComponent.fontSize * (linesCount + 2) + 2 * padding, viewportRT.rect.height);
        });
        StartCoroutine(FlexPanel.DelayAction(0f, ExecuteValueChanged));
    }

    public static string ClearCR(string input)
    {
        var sb = new StringBuilder(input);
        sb.Replace("\r", "");
        return sb.ToString();
    }

    public void ExecuteValueChanged()
    {
        if (field != null && element != null) //start method was executed
            StartCoroutine(FlexPanel.DelayAction(0f, () => field.onValueChanged.Invoke(field.text))); //sic! without a coroutine Viewport`s RectTransform.rect is not updated in time.
    }
}
