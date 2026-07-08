using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LinkShot.UI
{
    /// <summary>
    /// UI/層の共通ヘルパー。Kenney UI Packのロードと、uGUI要素の実行時生成を担う（ARCHITECTURE.md 2.1章のUI/層）。
    /// Resources.Load を使うため、対象スプライトは Assets/Resources/UI/Kenney 以下に置くこと。
    /// </summary>
    public static class UITheme
    {
        private const string KenneySpritePath = "UI/Kenney/PNG/Blue/Default/";
        private const string GoldRankPath = "UI/Kenney/Ranks/Gold/";

        public static Sprite LoadButtonSprite(string name) => Resources.Load<Sprite>(KenneySpritePath + name);

        public static Sprite LoadGoldRank(int index) => Resources.Load<Sprite>(GoldRankPath + $"rank{index:000}");

        private static Font _defaultFont;

        public static Font DefaultFont => _defaultFont != null ? _defaultFont : (_defaultFont = Resources.Load<Font>("Fonts/NotoSansJP-Subset"));

        public static GameObject CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = go.GetComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            return go;
        }

        public static Canvas CreateCanvas(string name, Transform parent, int sortOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); // 横持ち16:9基準
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            if (sprite != null)
            {
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, UnityAction onClick)
        {
            Image image = CreateImage(parent, name, LoadButtonSprite("button_rectangle_depth_gradient"), Color.white);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            Text text = CreateText(image.transform, "Label", label, 32, Color.black, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);

            return button;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void SetRect(RectTransform rt, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
        }
    }
}
