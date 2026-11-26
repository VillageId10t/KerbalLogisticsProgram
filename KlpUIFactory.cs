using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace KerbalLogisticsProgram
{
    public sealed class KlpUIFactory
    {
        private readonly KlpPalette palette;
        private readonly Font font;

        public KlpUIFactory(KlpPalette palette, Font font)
        {
            this.palette = palette;
            this.font = font;
        }

        // ==============================
        // PANEL
        // ==============================

        public RectTransform CreatePanel(string objectName,Transform parent,Color color,bool raycastTarget = true)
        {
            GameObject obj = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image));

            obj.transform.SetParent(parent, false);

            Image image = obj.GetComponent<Image>();

            image.color = color;
            image.raycastTarget = raycastTarget;

            return obj.GetComponent<RectTransform>();
        }

        // ==============================
        // TEXT
        // ==============================

        public Text CreateText(string objectName,Transform parent,string value,int fontSize,Color color,TextAnchor alignment,FontStyle fontStyle = FontStyle.Normal)
        {
            GameObject obj = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Text));

            obj.transform.SetParent(parent, false);

            Text text = obj.GetComponent<Text>();

            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = fontStyle;

            text.raycastTarget = false;

            return text;
        }

        // ==============================
        // BUTTON
        // ==============================

        public Button CreateButton(string objectName,Transform parent,string label,Color background,Color textColor,UnityAction onClick,int fontSize = 16)
        {
            RectTransform rect = CreatePanel(
                objectName,
                parent,
                Color.white);

            Button button =
                rect.gameObject.AddComponent<Button>();

            button.targetGraphic =
                rect.GetComponent<Image>();

            button.transition =
                Selectable.Transition.ColorTint;

            // Derive interaction colors from the palette.
            
            ColorBlock colors = button.colors;

            colors.normalColor = background;

            colors.highlightedColor = Color.Lerp(
                background,
                palette.PrimaryText,
                0.15f);

            colors.pressedColor = Color.Lerp(
                background,
                palette.WindowBackground,
                0.25f);

            colors.disabledColor =
                palette.DisabledBackground;

            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;

            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            // Button label

            Text text = CreateText(
                objectName + "_Label",
                rect,
                label,
                fontSize,
                textColor,
                TextAnchor.MiddleCenter,
                FontStyle.Bold);

            Stretch(text.rectTransform);

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            return button;
        }
        // ==============================
        // INPUT FIELD
        // ==============================

        public InputField CreateInputField(string objectName,Transform parent,string initialValue,string placeholder,Color accent,int fontSize = 16)
        {
            RectTransform rect = CreatePanel(
                objectName,
                parent,
                palette.RowBackground);

            InputField input =
                rect.gameObject.AddComponent<InputField>();

            input.targetGraphic = rect.GetComponent<Image>();

            input.transition = Selectable.Transition.None;
            input.lineType = InputField.LineType.SingleLine;
            input.contentType = InputField.ContentType.Standard;

            // Editable text

            Text text = CreateText(
                objectName + "_Text",
                rect,
                "",
                fontSize,
                palette.PrimaryText,
                TextAnchor.MiddleLeft);

            Stretch(text.rectTransform);

            text.rectTransform.offsetMin = new Vector2(10, 4);
            text.rectTransform.offsetMax = new Vector2(-10, -4);

            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            // Placeholder

            Text placeholderText = CreateText(
                objectName + "_Placeholder",
                rect,
                placeholder,
                fontSize,
                palette.SecondaryText,
                TextAnchor.MiddleLeft,
                FontStyle.Italic);

            Stretch(placeholderText.rectTransform);

            placeholderText.rectTransform.offsetMin =
                new Vector2(10, 4);

            placeholderText.rectTransform.offsetMax =
                new Vector2(-10, -4);

            // Input configuration

            input.textComponent = text;
            input.placeholder = placeholderText;

            input.customCaretColor = true;
            input.caretColor = accent;

            input.selectionColor = new Color(
                accent.r,
                accent.g,
                accent.b,
                0.35f);

            input.text = initialValue;

            return input;
        }

        // ==============================
        // PROGRESS BAR
        // ==============================

        public RectTransform CreateProgressBar(
            string objectName,
            Transform parent,
            Color accent,
            out RectTransform fill)
        {
            RectTransform track = CreatePanel(
                objectName,
                parent,
                palette.DisabledBackground,
                false);

            fill = CreatePanel(
                objectName + "_Fill",
                track,
                accent,
                false);

            SetProgress(fill, 0f);

            return track;
        }

        public void SetProgress(
            RectTransform fill,
            float value)
        {
            fill.anchorMin = Vector2.zero;

            fill.anchorMax = new Vector2(
                Mathf.Clamp01(value),
                1f);

            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }

        // ==============================
        // SCROLL VIEW
        // ==============================

        public ScrollRect CreateScrollView(
      string objectName,
      Transform parent,
      out RectTransform content)
        {
            const float scrollbarWidth = 14f;
            const float scrollbarGap = 4f;

            RectTransform root = CreatePanel(
                objectName,
                parent,
                palette.RowBackground);

            ScrollRect scroll =
                root.gameObject.AddComponent<ScrollRect>();

            // Visible viewport

            RectTransform viewport = CreatePanel(
                "Viewport",
                root,
                palette.RowBackground);

            Stretch(viewport);

            // Reserve space for the scrollbar.
            viewport.offsetMax = new Vector2(
                -(scrollbarWidth + scrollbarGap),
                0);

            viewport.gameObject.AddComponent<RectMask2D>();

            // Scrollable content

            GameObject contentObject = new GameObject(
                "Content",
                typeof(RectTransform));

            contentObject.transform.SetParent(
                viewport,
                false);

            content = contentObject.GetComponent<RectTransform>();

            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);

            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            // Scrollbar track

            RectTransform scrollbarRect = CreatePanel(
                "Scrollbar",
                root,
                palette.DisabledBackground);

            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);

            scrollbarRect.sizeDelta = new Vector2(
                scrollbarWidth,
                0);

            scrollbarRect.anchoredPosition = Vector2.zero;

            Scrollbar scrollbar =
                scrollbarRect.gameObject.AddComponent<Scrollbar>();

            // Sliding area

            GameObject slidingObject = new GameObject(
                "SlidingArea",
                typeof(RectTransform));

            slidingObject.transform.SetParent(
                scrollbarRect,
                false);

            RectTransform slidingArea =
                slidingObject.GetComponent<RectTransform>();

            Stretch(slidingArea);

            slidingArea.offsetMin = new Vector2(2, 2);
            slidingArea.offsetMax = new Vector2(-2, -2);

            // Scrollbar handle

            RectTransform handle = CreatePanel(
                "Handle",
                slidingArea,
                Color.white);

            Stretch(handle);

            scrollbar.handleRect = handle;

            scrollbar.targetGraphic =
                handle.GetComponent<Image>();

            scrollbar.direction =
                Scrollbar.Direction.BottomToTop;

            // Scrollbar interaction colors

            ColorBlock colors = scrollbar.colors;

            colors.normalColor = palette.SecondaryText;
            colors.highlightedColor = palette.PrimaryText;
            colors.pressedColor = palette.MailboxAccent;
            colors.disabledColor = palette.DisabledBackground;

            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            colors.selectedColor = colors.highlightedColor;

            scrollbar.colors = colors;

            // Scroll behavior

            scroll.viewport = viewport;
            scroll.content = content;

            scroll.horizontal = false;
            scroll.vertical = true;

            scroll.movementType =
                ScrollRect.MovementType.Clamped;

            scroll.inertia = false;
            scroll.scrollSensitivity = 30f;

            // Connect scrollbar to scroll view.

            scroll.verticalScrollbar = scrollbar;

            scroll.verticalScrollbarVisibility =
                ScrollRect.ScrollbarVisibility.Permanent;

            scrollbar.value = 1f;

            return scroll;
        }

        // ==============================
        // DROPDOWN
        // ==============================

        public Dropdown CreateDropdown(
            string objectName,
            Transform parent,
            Color accent,
            int fontSize = 16)
        {
            // Main dropdown control

            RectTransform root = CreatePanel(
                objectName,
                parent,
                palette.RowBackground);

            Dropdown dropdown =
                root.gameObject.AddComponent<Dropdown>();

            dropdown.targetGraphic =
                root.GetComponent<Image>();

            dropdown.transition =
                Selectable.Transition.None;

            // Selected destination label

            Text caption = CreateText(
                "Caption",
                root,
                "",
                fontSize,
                palette.PrimaryText,
                TextAnchor.MiddleLeft);

            Stretch(caption.rectTransform);

            caption.rectTransform.offsetMin =
                new Vector2(10, 0);

            caption.rectTransform.offsetMax =
                new Vector2(-36, 0);

            dropdown.captionText = caption;

            // Dropdown arrow

            Text arrow = CreateText(
                "Arrow",
                root,
                "\u25BC",
                14,
                accent,
                TextAnchor.MiddleCenter);

            RectTransform arrowRect = arrow.rectTransform;

            arrowRect.anchorMin = new Vector2(1, 0);
            arrowRect.anchorMax = new Vector2(1, 1);
            arrowRect.pivot = new Vector2(1, 0.5f);

            arrowRect.sizeDelta = new Vector2(32, 0);
            arrowRect.anchoredPosition = Vector2.zero;

            // Dropdown template and scroll view

            RectTransform content;

            ScrollRect scroll = CreateScrollView(
                "Template",
                root,
                out content);

            RectTransform template =
                scroll.GetComponent<RectTransform>();

            template.anchorMin = new Vector2(0, 0);
            template.anchorMax = new Vector2(1, 0);
            template.pivot = new Vector2(0.5f, 1);

            template.sizeDelta = new Vector2(0, 200);
            template.anchoredPosition = new Vector2(0, -2);

            // Individual dropdown item template

            const float itemHeight = 32f;

            content.sizeDelta = new Vector2(
                0,
                itemHeight);

            RectTransform item = CreatePanel(
                "Item",
                content,
                Color.white);

            item.anchorMin = new Vector2(0, 0);
            item.anchorMax = new Vector2(1, 1);
            item.pivot = new Vector2(0.5f, 0.5f);

            item.sizeDelta = Vector2.zero;
            item.anchoredPosition = Vector2.zero;

            Toggle toggle =
                item.gameObject.AddComponent<Toggle>();

            toggle.targetGraphic =
                item.GetComponent<Image>();

            toggle.transition =
                Selectable.Transition.ColorTint;

            // Item interaction colors

            ColorBlock colors = toggle.colors;

            colors.normalColor = palette.RowBackground;
            colors.highlightedColor = palette.ShippingHover;
            colors.selectedColor = palette.ShippingHover;
            colors.pressedColor = palette.ShippingSelection;

            colors.disabledColor =
                palette.DisabledBackground;

            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;

            toggle.colors = colors;

            // Selected item indicator

            RectTransform indicator = CreatePanel(
                "SelectedIndicator",
                item,
                accent,
                false);

            indicator.anchorMin = new Vector2(0, 0);
            indicator.anchorMax = new Vector2(0, 1);
            indicator.pivot = new Vector2(0, 0.5f);

            indicator.sizeDelta = new Vector2(4, -8);
            indicator.anchoredPosition = new Vector2(3, 0);

            toggle.graphic =
                indicator.GetComponent<Image>();

            // Item label

            Text itemLabel = CreateText(
                "ItemLabel",
                item,
                "",
                fontSize,
                palette.PrimaryText,
                TextAnchor.MiddleLeft);

            Stretch(itemLabel.rectTransform);

            itemLabel.rectTransform.offsetMin =
                new Vector2(14, 0);

            itemLabel.rectTransform.offsetMax =
                new Vector2(-8, 0);

            // Connect dropdown components

            dropdown.template = template;
            dropdown.itemText = itemLabel;

            template.gameObject.SetActive(false);

            return dropdown;
        }


        // ==============================
        // LAYOUT HELPERS
        // ==============================

        public void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        
        public void PlaceTop(RectTransform rect,float top,float height,float left = 0f,float right = 0f)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);

            rect.sizeDelta = new Vector2(
                -(left + right),
                height);

            rect.anchoredPosition = new Vector2(
                (left - right) * 0.5f,
                -top);
        }
        // ==============================
        // RESOURCE ROW
        // ==============================

        public KlpResourceRow CreateResourceRow(
            string objectName,
            Transform parent,
            bool shipping,
            System.Action<KlpResourceRow> onSelected)
        {
            RectTransform rect = CreatePanel(
                objectName,
                parent,
                palette.RowBackground,
                true);

            KlpResourceRow row =
                rect.gameObject.AddComponent<KlpResourceRow>();

            row.Initialize(
                this,
                palette,
                shipping,
                onSelected);

            return row;
        }
        public void PlaceSplitTop(RectTransform rect, bool rightSide, float top, float height, float outerPadding = 10f, float gap = 8f)
        {
            rect.anchorMin = new Vector2(
                rightSide ? 0.5f : 0f,
                1f);

            rect.anchorMax = new Vector2(
                rightSide ? 1f : 0.5f,
                1f);

            rect.pivot = new Vector2(0.5f, 1f);

            rect.offsetMin = new Vector2(
                rightSide ? gap * 0.5f : outerPadding,
                -(top + height));

            rect.offsetMax = new Vector2(
                rightSide ? -outerPadding : -gap * 0.5f,
                -top);
        }
        public void PlaceHorizontalPanels(RectTransform left,RectTransform right,float rightWidth,float padding = 12f,float gap = 12f)
        {
            // Left panel expands with available space.

            Stretch(left);

            left.offsetMin = new Vector2(
                padding,
                padding);

            left.offsetMax = new Vector2(
                -(rightWidth + padding + gap),
                -padding);

            // Right panel maintains a fixed width.

            right.anchorMin = new Vector2(1, 0);
            right.anchorMax = new Vector2(1, 1);
            right.pivot = new Vector2(1, 0.5f);

            right.offsetMin = new Vector2(
                -(rightWidth + padding),
                padding);

            right.offsetMax = new Vector2(
                -padding,
                -padding);
        }

        public RectTransform CreateResizeHandle(string objectName,Transform parent,Color accent,float size = 24f)
        {
            RectTransform handle = CreatePanel(
                objectName,
                parent,
                palette.HeaderBackground);

            handle.sizeDelta = new Vector2(size, size);

            // Construct a conventional diagonal resize grip
            // using three small UI image elements.

            for (int i = 0; i < 3; i++)
            {
                RectTransform line = CreatePanel(
                    "GripLine_" + i,
                    handle,
                    accent,
                    false);

                line.anchorMin = new Vector2(1f, 0f);
                line.anchorMax = new Vector2(1f, 0f);

                line.pivot = new Vector2(0.5f, 0.5f);

                float length = 6f + i * 5f;
                float distance = 5f + i * 3.5f;

                line.sizeDelta = new Vector2(
                    length,
                    2f);

                line.anchoredPosition = new Vector2(
                    -distance,
                    distance);

                line.localRotation =
                    Quaternion.Euler(0f, 0f, 45f);
            }

            return handle;
        }
        public InputField CreateBorderedInputField(string objectName,Transform parent,string initialValue,string placeholder,Color accent,out RectTransform frame,int fontSize = 18)
        {
            // Visible outer border.

            frame = CreatePanel(
                objectName + "_Frame",
                parent,
                accent,
                false);

            // Editable inner field.

            InputField input = CreateInputField(
                objectName,
                frame,
                initialValue,
                placeholder,
                accent,
                fontSize);

            RectTransform inputRect =
                input.GetComponent<RectTransform>();

            Stretch(inputRect);

            inputRect.offsetMin = new Vector2(2f, 2f);
            inputRect.offsetMax = new Vector2(-2f, -2f);

            input.GetComponent<Image>().color =
                palette.WindowBackground;

            // Ensure sufficient vertical space for
            // editable text and caret rendering.

            Text editableText = input.textComponent;

            editableText.rectTransform.offsetMin =
                new Vector2(10f, 0f);

            editableText.rectTransform.offsetMax =
                new Vector2(-10f, 0f);

            editableText.verticalOverflow =
                VerticalWrapMode.Overflow;

            editableText.alignment =
                TextAnchor.MiddleLeft;

            // Apply the same layout to placeholder text.

            Text placeholderText = input.placeholder as Text;

            if (placeholderText != null)
            {
                placeholderText.rectTransform.offsetMin =
                    new Vector2(10f, 0f);

                placeholderText.rectTransform.offsetMax =
                    new Vector2(-10f, 0f);

                placeholderText.verticalOverflow =
                    VerticalWrapMode.Overflow;

                placeholderText.alignment =
                    TextAnchor.MiddleLeft;
            }

            return input;
        }

    }
}