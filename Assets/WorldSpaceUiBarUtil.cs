using UnityEngine;
using UnityEngine.UI;

/// <summary>世界空间血条：仅用内置 <see cref="Texture2D.whiteTexture"/> + <see cref="Image"/>，无额外美术资源。</summary>
public static class WorldSpaceUiBarUtil
{
    static Sprite _white;

    public static Sprite WhiteSprite()
    {
        if (_white != null)
            return _white;
        Texture2D t = Texture2D.whiteTexture;
        _white = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        _white.name = "BuiltinWhiteSprite";
        return _white;
    }

    /// <summary>在父物体下创建世界空间 Canvas + 底条 + 横向填充条，返回填充 <see cref="Image"/>。</summary>
    public static Image CreateHorizontalBar(Transform parent, string rootName, Vector3 localPosition, Vector3 localScale,
        Vector2 sizeDelta, Color bgColor, Color fillColor, int sortingOrder)
    {
        Transform old = parent.Find(rootName);
        if (old != null)
        {
            Canvas oldCanvas = old.GetComponent<Canvas>();
            Image oldBg = old.Find("Bg") != null ? old.Find("Bg").GetComponent<Image>() : null;
            Image oldFill = old.Find("Fill") != null ? old.Find("Fill").GetComponent<Image>() : null;
            RectTransform oldRt = old.GetComponent<RectTransform>();
            old.localPosition = localPosition;
            old.localRotation = Quaternion.identity;
            old.localScale = localScale;
            if (oldCanvas != null)
            {
                oldCanvas.renderMode = RenderMode.WorldSpace;
                oldCanvas.worldCamera = Camera.main;
                oldCanvas.sortingOrder = sortingOrder;
            }
            if (oldRt != null)
                oldRt.sizeDelta = sizeDelta;
            if (oldBg != null)
            {
                oldBg.sprite = WhiteSprite();
                oldBg.raycastTarget = false;
                oldBg.color = bgColor;
            }
            if (oldFill != null)
            {
                oldFill.sprite = WhiteSprite();
                oldFill.raycastTarget = false;
                oldFill.color = fillColor;
                oldFill.type = Image.Type.Filled;
                oldFill.fillMethod = Image.FillMethod.Horizontal;
                oldFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                return oldFill;
            }
        }

        GameObject root = new GameObject(rootName);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = localScale;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = sortingOrder;
        // Canvas 已自带 RectTransform，禁止再 AddComponent<RectTransform>()
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = sizeDelta;

        GameObject bgGo = new GameObject("Bg");
        bgGo.transform.SetParent(root.transform, false);
        Image bg = bgGo.AddComponent<Image>();
        RectTransform bgRt = bg.rectTransform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.sprite = WhiteSprite();
        bg.raycastTarget = false;
        bg.color = bgColor;

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(root.transform, false);
        Image fill = fillGo.AddComponent<Image>();
        RectTransform fillRt = fill.rectTransform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(1f, 1f);
        fillRt.offsetMax = new Vector2(-1f, -1f);
        fill.sprite = WhiteSprite();
        fill.raycastTarget = false;
        fill.color = fillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        return fill;
    }
}
