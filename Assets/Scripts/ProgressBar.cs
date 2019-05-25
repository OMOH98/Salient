using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Slider))]
public class ProgressBar : MonoBehaviour
{

    [Range(0f, 1f)] public float value;
    public Color zeroColor = Color.red;
    public Color halfColor = Color.yellow;
    public Color fullColor = Color.green;

    Slider slider;
    List<Image> bground;
    // Use this for initialization
    void Start()
    {
        slider = gameObject.GetComponent<Slider>();
        bground = new List<Image>();
        gameObject.GetComponentsInChildren<Image>(bground);

        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

    }

    // Update is called once per frame
    private void LateUpdate()
    {
        slider.value = value;

        Color bottom, ceil;
        float lerp;
        if (value > 0.5f)
        {
            bottom = halfColor;
            ceil = fullColor;
            lerp = (value - 0.5f) * 2f;
        }
        else
        {
            bottom = zeroColor;
            ceil = halfColor;
            lerp = value * 2f;
        }


        var c = Color.Lerp(bottom, ceil, lerp);
        foreach (var item in bground)
        {
            item.color = c;
        }
    }
    bool blocked = false;
    public void ChangeValueToWithin(float eventualValue, float time)
    {
        if (!blocked)
            StartCoroutine(ChangeValOverTime(eventualValue, time));
    }
    private IEnumerator ChangeValOverTime(float endValue, float timeForWork)
    {
        float startTime = Time.time;
        float endTime = startTime + timeForWork;
        float startValue = value;
        blocked = true;

        while (Time.time < endTime)
        {
            value = Mathf.Lerp(startValue, endValue, (endTime - startTime) / (Time.time - startTime));
            yield return null;
        }

        blocked = false;
        yield break;
    }
}
