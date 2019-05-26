﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
//using System.Runtime.Serialization;
//using System.Runtime.Serialization;


[RequireComponent(typeof(RectTransform))]
public class FlexPanel : MonoBehaviour
{

    //[Range(0f, 1f)]
    //public float widthCoef=1f;
    //[Range(0f, 1f)]
    //public float heightCoef=1f;
    //public FlexAlignment _alignment = FlexAlignment.left;

    public Data data;
    public uint id; //id unique to all flexPanels

    [EnumFlags]
    public FlexAlignment allowedAlignmens;// = FlexAlignment.left|FlexAlignment.right;
    public float paddingLeft, paddingRight;
    public bool changeableWidth = true;
    public float minWidth = 0.2f;
    public float maxWidth = 1f;
    //public bool changeableHeight = false;
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
        get { return data.alignment; }
        set
        {
            if (allowedAlignmens < 0 || (value & allowedAlignmens) != 0)
            {
                data.alignment = value;
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
        BakeIds();

        var k = prefsKey;
        if(PlayerPrefs.HasKey(k))
        {
            data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(k));
        }

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
        if(allowedAlignmens!=0&&allowedAlignmens!=data.alignment)
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
                SaveConfiguration();
            });
        }
        if(changeableWidth)
        {
            widthSlider = Instantiate(widthSliderPrefab).GetComponent<Slider>();
            widthSlider.minValue = minWidth;
            widthSlider.maxValue = maxWidth;
            widthSlider.value = data.widthCoef;
            widthSlider.onValueChanged.AddListener((v) =>
            {
                data.widthCoef = v;
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
    #region Alignment
    private FlexAlignment NextAllowedAlignment()
    {
        if (allowedAlignmens == 0)
            return data.alignment;

        var values = System.Enum.GetValues(typeof(FlexAlignment)).Cast<FlexAlignment>().ToList();
        values.Sort((lhs, rhs) => { return (int)lhs - (int)rhs; });
        var max = values[values.Count - 1];

        var prospective = (int)data.alignment;

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
        SaveConfiguration();
        float apx = 0f, apy = 0f, sdx = 0f, sdy = 0f, hcoef = 1f;

        if (data.alignment == FlexAlignment.left)
        {
            hcoef = -1f;
        }
        switch (data.alignment)
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

        apx = hcoef * Screen.width * (1f - data.widthCoef) * 0.5f;
        switch (data.alignment)
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

        sdx = -Screen.width * (1f - data.widthCoef);
        sdy = -Screen.height * (1f - data.heightCoef);

        rt.anchoredPosition = new Vector2(apx, apy);
        rt.sizeDelta = new Vector2(sdx, sdy);
    }
    #endregion
    #region DelayCoroutines
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
    #endregion
    #region SavingConfigurationAndIdMaking
    private string prefsKey { get { return "FlexPanel" + id.ToString(); } }
    protected virtual void SaveConfiguration()
    {
        PlayerPrefs.SetString(prefsKey, JsonUtility.ToJson(data));
    }
    private static bool IdMaked = false;
    private const int IdSeed = 45184;
    private static void BakeIds()
    {
        if (IdMaked)
            return;
        IdMaked = true;

        List<uint> usedIds = new List<uint>(16);
        System.Random r = new System.Random(IdSeed);
        var rgos = SceneManager.GetActiveScene().GetRootGameObjects();
        var flexPanels = from o in rgos select o.GetComponentsInChildren<FlexPanel>();
        foreach (var item in flexPanels)
        {
            foreach (var panel in item)
            {
                var id = 0u;
                do
                {
                    id = (uint)r.Next();
                } while (usedIds.Contains(id));
                panel.id = id;
                usedIds.Add(id);
            }
        }
        
    }

    #endregion


    #region NestedDeclarations
    [System.Flags]
    public enum FlexAlignment
    {
        //nothing - 0, everything =-1
        left = 1, middle = 2, right = 4
    }
    [System.Serializable]
    public class Data
    {
        public FlexAlignment alignment;
        [Range(0f, 1f)]
        public float widthCoef;
        [Range(0f, 1f)]
        public float heightCoef;
    }
    #endregion


}

public class EnumFlagsAttribute : PropertyAttribute
{
    public EnumFlagsAttribute() { }
}