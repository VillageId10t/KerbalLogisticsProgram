using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace KerbalLogisticsProgram
{
    public sealed class KlpResourceRow :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        private KlpPalette palette;

        private Image background;
        private Image selectionIndicator;

        private Text resourceLabel;
        private Text firstValueLabel;
        private Text secondValueLabel;

        private Color accent;
        private Color selectionColor;
        private Color hoverColor;
        private Color normalColor;

        private bool selected;
        private bool hovered;

        private Action<KlpResourceRow> selectionCallback;

        public string ResourceName
        {
            get;
            private set;
        }

        public void Initialize(
            KlpUIFactory factory,
            KlpPalette theme,
            bool shipping,
            Action<KlpResourceRow> onSelected)
        {
            palette = theme;
            selectionCallback = onSelected;

            background = GetComponent<Image>();

            accent = shipping
                ? palette.ShippingAccent
                : palette.MailboxAccent;

            selectionColor = shipping
                ? palette.ShippingSelection
                : palette.MailboxSelection;

            hoverColor = shipping
                ? palette.ShippingHover
                : palette.MailboxHover;

            normalColor = palette.RowBackground;

            RectTransform row =
                GetComponent<RectTransform>();

            // Resource name

            resourceLabel = factory.CreateText(
                "ResourceName",
                row,
                "",
                16,
                palette.PrimaryText,
                TextAnchor.MiddleLeft);

            PlaceColumn(
                resourceLabel.rectTransform,
                0f, 0.46f);

            // First quantity

            firstValueLabel = factory.CreateText(
                "FirstValue",
                row,
                "",
                16,
                palette.PrimaryText,
                TextAnchor.MiddleRight);

            PlaceColumn(
                firstValueLabel.rectTransform,
                0.46f, 0.73f);

            // Second quantity

            secondValueLabel = factory.CreateText(
                "SecondValue",
                row,
                "",
                16,
                accent,
                TextAnchor.MiddleRight);

            PlaceColumn(
                secondValueLabel.rectTransform,
                0.73f, 1f);

            // Non-color-dependent selection indicator

            RectTransform indicator = factory.CreatePanel(
                "SelectionIndicator",
                row,
                accent,
                false);

            indicator.anchorMin = new Vector2(0, 0);
            indicator.anchorMax = new Vector2(0, 1);
            indicator.pivot = new Vector2(0, 0.5f);

            indicator.sizeDelta = new Vector2(4, 0);
            indicator.anchoredPosition = Vector2.zero;

            selectionIndicator =
                indicator.GetComponent<Image>();

            UpdateAppearance();
        }

        private void PlaceColumn(
            RectTransform rect,
            float start,
            float end)
        {
            rect.anchorMin = new Vector2(start, 0);
            rect.anchorMax = new Vector2(end, 1);

            rect.offsetMin = new Vector2(10, 0);
            rect.offsetMax = new Vector2(-10, 0);
        }

        public void Bind(
            string resourceName,
            string firstValue,
            string secondValue,
            int rowIndex)
        {
            ResourceName = resourceName;

            normalColor = rowIndex % 2 == 0
                ? palette.RowBackground
                : palette.AlternateRowBackground;

            resourceLabel.text = resourceName;

            UpdateValues(firstValue, secondValue);

            hovered = false;
            selected = false;

            UpdateAppearance();
        }

        public void UpdateValues(
            string firstValue,
            string secondValue)
        {
            if (firstValueLabel.text != firstValue)
            {
                firstValueLabel.text = firstValue;
            }

            if (secondValueLabel.text != secondValue)
            {
                secondValueLabel.text = secondValue;
            }
        }

        public void SetSelected(bool value)
        {
            if (selected == value)
                return;

            selected = value;

            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            background.color = hovered
                ? hoverColor
                : selected
                    ? selectionColor
                    : normalColor;

            selectionIndicator.enabled = selected;

            resourceLabel.fontStyle = selected
                ? FontStyle.Bold
                : FontStyle.Normal;
        }

        public void OnPointerEnter(
            PointerEventData eventData)
        {
            hovered = true;

            UpdateAppearance();
        }

        public void OnPointerExit(
            PointerEventData eventData)
        {
            hovered = false;

            UpdateAppearance();
        }

        public void OnPointerClick(
            PointerEventData eventData)
        {
            if (eventData.button !=
                PointerEventData.InputButton.Left)
            {
                return;
            }

            if (selectionCallback != null)
            {
                selectionCallback(this);
            }
        }

        private void OnDisable()
        {
            hovered = false;

            if (background != null &&
                selectionIndicator != null &&
                resourceLabel != null)
            {
                UpdateAppearance();
            }
        }
    }
}