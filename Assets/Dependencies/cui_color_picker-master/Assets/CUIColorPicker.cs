using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ErgoShop.Managers;

public class CUIColorPicker : MonoBehaviour
{
    public Color Color { get { return _color; } set { Setup(value); } }

    public bool EditField { get => editField; set => editField = value; }

    public void SetOnValueChangeCallback(Action<Color> onValueChange)
    {
        _onValueChange = onValueChange;
    }
    private Color _color = Color.red;
    private Action<Color> _onValueChange;
    private Action _update;

    public GameObject satvalGO; //GO("SaturationValue");
    public GameObject satvalKnob; //GO("SaturationValue/Knob");
    public GameObject hueGO;// GO("Hue");
    public GameObject hueKnob; //GO("Hue/Knob");
    public GameObject result; //GO("Result");

    public TMP_InputField[] RGBAInputField;
    private bool editField = false;

    public Image ToggleBtnImage;

    private static void RGBToHSV(Color color, out float h, out float s, out float v)
    {
        var cmin = Mathf.Min(color.r, color.g, color.b);
        var cmax = Mathf.Max(color.r, color.g, color.b);
        var d = cmax - cmin;
        if (d == 0)
        {
            h = 0;
        }
        else if (cmax == color.r)
        {
            h = Mathf.Repeat((color.g - color.b) / d, 6);
        }
        else if (cmax == color.g)
        {
            h = (color.b - color.r) / d + 2;
        }
        else
        {
            h = (color.r - color.g) / d + 4;
        }
        s = cmax == 0 ? 0 : d / cmax;
        v = cmax;
    }

    private static bool GetLocalMouse(GameObject go, out Vector2 result)
    {
        var rt = (RectTransform)go.transform;
        var mp = rt.InverseTransformPoint(Input.mousePosition);
        result.x = Mathf.Clamp(mp.x, rt.rect.min.x, rt.rect.max.x);
        result.y = Mathf.Clamp(mp.y, rt.rect.min.y, rt.rect.max.y);
        return rt.rect.Contains(mp);
    }

    private static Vector2 GetWidgetSize(GameObject go)
    {
        var rt = (RectTransform)go.transform;
        return rt.rect.size;
    }

    //private GameObject GO(string name)
    //{
    //    return transform.Find(name).gameObject;
    //}

    private void Setup(Color inputColor)
    {

        Color[] hueColors = new Color[] {
            Color.red,
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.blue,
            Color.magenta,
        };

        Color[] satvalColors = new Color[] {
            new Color( 0, 0, 0 ),
            new Color( 0, 0, 0 ),
            new Color( 1, 1, 1 ),
            hueColors[0],
        };

        Texture2D hueTex = new Texture2D(1, 7);

        for (int i = 0; i < 7; i++)
        {
            hueTex.SetPixel(0, i, hueColors[i % 6]);
        }
        hueTex.Apply();
        hueGO.GetComponent<Image>().sprite = Sprite.Create(hueTex, new Rect(0, 0.5f, 1, 6), new Vector2(0.5f, 0.5f));

        Vector2 hueSz = GetWidgetSize(hueGO);
        Texture2D satvalTex = new Texture2D(2, 2);

        satvalGO.GetComponent<Image>().sprite = Sprite.Create(satvalTex, new Rect(0.5f, 0.5f, 1, 1), new Vector2(0.5f, 0.5f));
        Action resetSatValTexture = () =>
        {
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < 2; i++)
                {
                    satvalTex.SetPixel(i, j, satvalColors[i + j * 2]);
                }
            }
            satvalTex.Apply();
        };

        Vector2 satvalSz = GetWidgetSize(satvalGO);
        float Hue, Saturation, Value;

        RGBToHSV(inputColor, out Hue, out Saturation, out Value);

        Action applyHue = () =>
        {
            var i0 = Mathf.Clamp((int)Hue, 0, 5);
            var i1 = (i0 + 1) % 6;
            var resultColor = Color.Lerp(hueColors[i0], hueColors[i1], Hue - i0);
            satvalColors[3] = resultColor;
            resetSatValTexture();
        };

        Action applySaturationValue = () =>
        {
            var sv = new Vector2(Saturation, Value);
            var isv = new Vector2(1 - sv.x, 1 - sv.y);
            var c0 = isv.x * isv.y * satvalColors[0];
            var c1 = sv.x * isv.y * satvalColors[1];
            var c2 = isv.x * sv.y * satvalColors[2];
            var c3 = sv.x * sv.y * satvalColors[3];
            var resultColor = c0 + c1 + c2 + c3;

            //var resImg = result.GetComponent<Image>();
            //resImg.color = resultColor;
            //Debug.Log(resImg.color);

            if (ToggleBtnImage != null)
            {
                ToggleBtnImage.color = resultColor;
            }



            if (_color != resultColor)
            {
                if (_onValueChange != null)
                {
                    _onValueChange(resultColor);
                }
                _color = resultColor;
            }
        };

        applyHue();
        applySaturationValue();

        satvalKnob.transform.localPosition = new Vector2(Saturation * satvalSz.x, Value * satvalSz.y);
        hueKnob.transform.localPosition = new Vector2(hueKnob.transform.localPosition.x, Hue / 6 * satvalSz.y);

        Action dragH = null;
        Action dragSV = null;
        Action idle = () =>
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mp;
                if (GetLocalMouse(hueGO, out mp))
                {
                    _update = dragH;
                }
                else if (GetLocalMouse(satvalGO, out mp))
                {
                    _update = dragSV;
                }
            }
        };

        dragH = () =>
        {
            Vector2 mp;
            GetLocalMouse(hueGO, out mp);
            Hue = mp.y / hueSz.y * 6;
            applyHue();
            applySaturationValue();
            hueKnob.transform.localPosition = new Vector2(hueKnob.transform.localPosition.x, mp.y);
            if (Input.GetMouseButtonUp(0))
            {
                _update = idle;
            }
        };
        dragSV = () =>
        {
            Vector2 mp;
            GetLocalMouse(satvalGO, out mp);
            Saturation = mp.x / satvalSz.x;
            Value = mp.y / satvalSz.y;
            applySaturationValue();
            satvalKnob.transform.localPosition = mp;
            if (Input.GetMouseButtonUp(0))
            {
                _update = idle;
            }
        };
        _update = idle;
    }

    public void SetRandomColor()
    {
        var rng = new System.Random();
        var r = (rng.Next() % 1000) / 1000.0f;
        var g = (rng.Next() % 1000) / 1000.0f;
        var b = (rng.Next() % 1000) / 1000.0f;
        Color = new Color(r, g, b);
    }

    void Awake()
    {
        Color = Color.red;
    }

    void Update()
    {
        _update();

        //remplissage text
        if (EditField == false)
        {
            RGBAInputField[0].text = (Color.r * 255).ToString();
            RGBAInputField[1].text = (Color.g * 255).ToString();
            RGBAInputField[2].text = (Color.b * 255).ToString();
            RGBAInputField[3].text = (Color.a * 255).ToString();
        }
    }

    public void CopiColor()
    {
        UIManager.Instance.copieColeurPicker = Color;
        Debug.Log(UIManager.Instance.copieColeurPicker);
    }

    public void PastColor()
    {
        _color = UIManager.Instance.copieColeurPicker;
        Debug.Log(_color);
    }

    public void ChangValue()
    {
        float r, g, b, a;

        if (float.TryParse(RGBAInputField[0].text, out r))
        {
            Color = new Color(r, Color.g, Color.b, Color.a);
        }

        if (float.TryParse(RGBAInputField[1].text, out g))
        {
            Color = new Color(Color.r, g, Color.b, Color.a);
        }

        if (float.TryParse(RGBAInputField[2].text, out b))
        {
            Color = new Color(Color.r, Color.g, b, Color.a);
        }

        if (float.TryParse(RGBAInputField[3].text, out a))
        {
            Color = new Color(Color.r, Color.g, Color.b, a);
        }
        _color = Color;
    }
}
