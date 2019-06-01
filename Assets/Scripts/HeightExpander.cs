using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(LayoutElement))]
public class HeightExpander : MonoBehaviour
{
    Image img;
    LayoutElement el;
    float ratio;
    // Start is called before the first frame update
    void Start()
    {
        img = GetComponent<Image>();
        el = GetComponent<LayoutElement>();
        ratio = (float)img.sprite.texture.height / img.sprite.texture.width;
    }

    public void RefreshHeight()
    {
        if (img == null)
            Start();

        ratio = (float)img.sprite.texture.height / img.sprite.texture.width;
        el.preferredHeight = img.rectTransform.sizeDelta.x * ratio;
    }

    float prevDeltaX;
    void Update()
    {
        var x = img.rectTransform.sizeDelta.x;
        if (prevDeltaX != x)
        {
            RefreshHeight();
            prevDeltaX = x;
        }
    }
}
