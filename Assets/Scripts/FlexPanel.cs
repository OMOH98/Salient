using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class FlexPanel : MonoBehaviour
{

    [Range(0f, 1f)]
    public float widthCoef=1f;
    [Range(0f, 1f)]
    public float heightCoef=1f;
    public FlexAlignment alignment = FlexAlignment.left;
    
    [EnumFlags]
    public FlexAlignment allowedAlignmens;// = FlexAlignment.left|FlexAlignment.right;
    public float paddingLeft, paddingRight;
    public bool changeableWidth = true;
    public bool changeableHeight = false;
    public bool allowHide = true;

    public Transform trayButtonsParent;
    public GameObject buttonPrefab;
    public Transform controlsParent = null;

    //[Header("Padding")]
    //public float pLeft, pTop, pRight, pBottom;

    private RectTransform rt;
    private Button trayButton;
    private Button hideButton;
    // Start is called before the first frame update
    void Start()
    {
        rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        float apx = 0f, apy = 0f, sdx = 0f, sdy = 0f, hcoef = 1f;

        if(alignment == FlexAlignment.left)
        {
            hcoef = -1f;
        }
        switch (alignment)
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
        switch (alignment)
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
        sdy = -Screen.height *(1f - heightCoef);

        rt.anchoredPosition = new Vector2(apx, apy);
        rt.sizeDelta = new Vector2(sdx, sdy);

        if (allowHide)
        {
            trayButton = Instantiate(buttonPrefab).GetComponent<Button>();
            trayButton.GetComponentInChildren<Text>().text = "Show " + gameObject.name;
            trayButton.gameObject.SetActive(false);
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

            StartCoroutine(DelayAction(0.05f, () =>
            {
                trayButton.transform.SetParent(trayButtonsParent);
                hideButton.transform.SetParent(controlsParent == null ? transform : controlsParent);
            }));
        }
    }

    private IEnumerator DelayAction(float seconds, System.Action action)
    {
        yield return new WaitForSeconds(seconds);
        action();
        yield break;
    }

    [System.Flags]
    public enum FlexAlignment
    {
        //nothing - 0, everything =-1
        left = 1, right = 2, middle = 4
    }
}

public class EnumFlagsAttribute : PropertyAttribute
{
    public EnumFlagsAttribute() { }
}