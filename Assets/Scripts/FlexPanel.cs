using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class FlexPanel : MonoBehaviour
{

    [Range(0f, 1f)]
    public float widthCoef=1f;
    [Range(0f, 1f)]
    public float heightCoef=1f;
    public FlexAlignment _alignment = FlexAlignment.left;
    
    [EnumFlags]
    public FlexAlignment allowedAlignmens;// = FlexAlignment.left|FlexAlignment.right;
    public float paddingLeft, paddingRight;
    public bool changeableWidth = true;
    public bool changeableHeight = false;
    public bool allowHide = true;

    public GameObject buttonPrefab;
    public GameObject widthSliderPrefab;

    public Transform trayButtonsParent;
    public Transform controlsParent = null;

    private RectTransform rt;
    private Button trayButton;
    private Button hideButton;
    private Button switchAlignmenButton;
    private Text switchAlignmentCaption;
    private Slider widthSlider;

    private Coroutine applyWidthCanges = null;

    public FlexAlignment alignment
    {
        get { return _alignment; }
        set
        {
            if(allowedAlignmens<0||(value&allowedAlignmens)!=0)
            {
                _alignment = value;
                Align();
            }
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;

        Align();

        if (allowHide)
        {
            trayButton = Instantiate(buttonPrefab).GetComponent<Button>();
            trayButton.GetComponentInChildren<Text>().text = "Show " + gameObject.name;

            trayButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(true);
                trayButton.gameObject.SetActive(false);
            });

            hideButton = Instantiate(buttonPrefab).GetComponent<Button>();
            hideButton.GetComponentInChildren<Text>().text = "Hide";
            hideButton.onClick.AddListener(() =>
            {
                trayButton.gameObject.SetActive(true);
                gameObject.SetActive(false);
            });

            trayButton.transform.SetParent(trayButtonsParent);
            hideButton.transform.SetParent(controlsParent == null ? transform : controlsParent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(trayButtonsParent as RectTransform);
            trayButton.gameObject.SetActive(false);
        }
        if(allowedAlignmens!=0&&allowedAlignmens!=_alignment)
        {
            switchAlignmenButton = Instantiate(buttonPrefab).GetComponent<Button>();
            switchAlignmentCaption = switchAlignmenButton.GetComponentInChildren<Text>();
            switchAlignmentCaption.text = "Align >";
            switchAlignmenButton.transform.SetParent(controlsParent == null ? transform : controlsParent);
            switchAlignmenButton.onClick.AddListener(() =>
            {
                var a = NextAllowedAlignment();
                if (a == FlexAlignment.right)
                    switchAlignmentCaption.text = "Align <";
                else switchAlignmentCaption.text = "Align >";
                alignment = a;
            });
        }
        if(changeableWidth)
        {
            widthSlider = Instantiate(widthSliderPrefab).GetComponent<Slider>();
            widthSlider.minValue = 0f;
            widthSlider.maxValue = 1f;
            widthSlider.value = widthCoef;
            widthSlider.onValueChanged.AddListener((v) =>
            {
                widthCoef = v;
                if (applyWidthCanges==null)
                {
                    applyWidthCanges = StartCoroutine(DelayActionWhile(Align, () =>
                    {
                        if (Input.GetMouseButton(0))
                            return true;
                        else
                        {
                            applyWidthCanges = null;
                            return false;
                        }
                    }));
                }
            });
            widthSlider.transform.SetParent(controlsParent == null ? transform : controlsParent);
            LayoutRebuilder.ForceRebuildLayoutImmediate((controlsParent == null ? transform : controlsParent) as RectTransform);
        }
    }

    private FlexAlignment NextAllowedAlignment()
    {
        if (allowedAlignmens == 0)
            return _alignment;

        var values = System.Enum.GetValues(typeof(FlexAlignment)).Cast<FlexAlignment>().ToList();
        values.Sort((lhs, rhs) => { return (int)lhs - (int)rhs; });
        var max = values[values.Count - 1];

        var prospective = (int)_alignment;

        do
        {
            prospective *= 2;
            if (prospective > (int)max)
                prospective = (int)values[0];
        }
        while (allowedAlignmens>0&&(prospective & (int)allowedAlignmens) == 0);

        return (FlexAlignment)prospective;
    }

    private void Align()
    {
        float apx = 0f, apy = 0f, sdx = 0f, sdy = 0f, hcoef = 1f;

        if (_alignment == FlexAlignment.left)
        {
            hcoef = -1f;
        }
        switch (_alignment)
        {
            case FlexAlignment.left:
                hcoef = -1f;
                break;
            case FlexAlignment.right:
                hcoef = 1f;
                break;
            case FlexAlignment.middle:
                hcoef = 0f;
                break;
            default:
                break;
        }

        apx = hcoef * Screen.width * (1f - widthCoef) * 0.5f;
        switch (_alignment)
        {
            case FlexAlignment.left:
                apx += paddingLeft;
                break;
            case FlexAlignment.right:
                apx -= paddingRight;
                break;
            default:
                break;
        }

        sdx = -Screen.width * (1f - widthCoef);
        sdy = -Screen.height * (1f - heightCoef);

        rt.anchoredPosition = new Vector2(apx, apy);
        rt.sizeDelta = new Vector2(sdx, sdy);
    }

    public static IEnumerator DelayAction(float seconds, System.Action action)
    {
        yield return new WaitForSeconds(seconds);
        action();
        yield break;
    }
    public static IEnumerator DelayActionWhile(System.Action action, System.Func<bool> holdCondition)
    {
        while (holdCondition())
            yield return null;
        action();
        yield break;
    }

    [System.Flags]
    public enum FlexAlignment
    {
        //nothing - 0, everything =-1
        left = 1, middle = 2, right = 4
    }
}

public class EnumFlagsAttribute : PropertyAttribute
{
    public EnumFlagsAttribute() { }
}