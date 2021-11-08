using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bulb : MonoBehaviour
{
    const float bulbRange = 0.02f;
    public const float fadeTime = 0.5f;

    private static readonly Dictionary<LightColors, Color> colorLookup = new Dictionary<LightColors, Color>()
    {
        { LightColors.Red, Color.red },
        { LightColors.Green, Color.green },
        { LightColors.Blue, Color.blue },
        { LightColors.Cyan, Color.cyan },
        { LightColors.Magenta, Color.magenta },
        { LightColors.Yellow, Color.yellow },
        { LightColors.Off, Color.black },
        { LightColors.White, Color.white },
    };
    private Light _light;

    private LightColors _color;
    private bool _lit;
    public bool animating { get; private set; }
    private float? _scalar = null;

    public float scalar
    {
        get { return _scalar.Value; }
        set
        {
            _scalar = value;
            _light.range *= value;
        }
    }

    public bool lightState 
    {
        get { return _light.enabled; }
        set { _light.enabled = value; } 
    }

    private void Awake()
    {
        _light = GetComponentInChildren<Light>();
    }

    public LightColors color
    {
        get { return _color; }
        set
        {
            animating = true;
            if (value == LightColors.Off)
                StartCoroutine(FadeOut());
            else if (_lit)
                StartCoroutine(SwitchColor(_color, value));
            else
                StartCoroutine(FadeIn(value));
            _color = value;
        }
    }



    IEnumerator FadeOut()
    {
        if (!_lit)
            yield break;
        _lit = false;
        float delta = 0;
        while (delta < 1)
        {
            yield return null;
            delta += Time.deltaTime / fadeTime;
            _light.range = Mathf.Lerp(_scalar.Value * bulbRange, 0, delta);
        }
        _light.enabled = false;
        animating = false;
    }
    IEnumerator FadeIn(LightColors newCol)
    {
        _light.enabled = true;
        _lit = true;
        _light.color = colorLookup[newCol];
        float delta = 0;
        while (delta < 1)
        {
            yield return null;
            delta += Time.deltaTime / fadeTime;
            _light.range = Mathf.Lerp(0, _scalar.Value * bulbRange, delta);
        }
        animating = false;
    }
    IEnumerator SwitchColor(LightColors prev, LightColors newCol)
    {
        float delta = 0;
        while (delta < 1)
        {
            yield return null;
            delta += Time.deltaTime / fadeTime;
            _light.color = Color.Lerp(colorLookup[prev], colorLookup[newCol], delta);
        }
        animating = false;  
    }

}
