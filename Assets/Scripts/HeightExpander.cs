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
    // Start is called before the first frame update
    void Start()
    {
        img = GetComponent<Image>();
        el = GetComponent<LayoutElement>();
        var coef = img.sprite.texture.width / img.sprite.texture.height;
        el.preferredHeight = img.rectTransform.sizeDelta.x * coef;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
