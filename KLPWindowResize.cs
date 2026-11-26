using UnityEngine;
using UnityEngine.EventSystems;

namespace KerbalLogisticsProgram
{
    public sealed class KlpWindowResize :
        MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        public RectTransform Target;

        public Vector2 MinimumSize =
            new Vector2(1000f, 640f);

        public Vector2 MaximumSize =
            new Vector2(1600f, 950f);

        private RectTransform coordinateSpace;

        private Vector2 pointerStart;
        private Vector2 windowStartSize;
        private Vector2 windowStartPosition;

        private bool resizing;

        public void OnBeginDrag(
            PointerEventData eventData)
        {
            resizing = false;

            if (eventData.button !=
                PointerEventData.InputButton.Left)
            {
                return;
            }

            if (Target == null)
                return;

            coordinateSpace =
                Target.parent as RectTransform;

            if (coordinateSpace == null)
                return;

            // Capture mouse position in the
            // window parent's coordinate system.

            if (!RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    coordinateSpace,
                    eventData.position,
                    eventData.pressEventCamera,
                    out pointerStart))
            {
                return;
            }

            windowStartSize = Target.rect.size;

            windowStartPosition =
                Target.anchoredPosition;

            resizing = true;
        }

        public void OnDrag(
            PointerEventData eventData)
        {
            if (!resizing ||
                Target == null ||
                coordinateSpace == null)
            {
                return;
            }

            Vector2 pointerCurrent;

            if (!RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    coordinateSpace,
                    eventData.position,
                    eventData.pressEventCamera,
                    out pointerCurrent))
            {
                return;
            }

            Vector2 delta =
                pointerCurrent - pointerStart;

            // Rightward movement increases width.
            // Downward movement increases height.

            float newWidth = Mathf.Clamp(
                windowStartSize.x + delta.x,
                MinimumSize.x,
                MaximumSize.x);

            float newHeight = Mathf.Clamp(
                windowStartSize.y - delta.y,
                MinimumSize.y,
                MaximumSize.y);

            Vector2 newSize = new Vector2(
                newWidth,
                newHeight);

            Vector2 sizeChange =
                newSize - windowStartSize;

            // Compensate for the window pivot so
            // the top-left corner remains stationary.

            Vector2 positionChange = new Vector2(
                sizeChange.x * Target.pivot.x,
                -sizeChange.y * (1f - Target.pivot.y));

            Target.sizeDelta = newSize;

            Target.anchoredPosition =
                windowStartPosition + positionChange;
        }

        public void OnEndDrag(
            PointerEventData eventData)
        {
            resizing = false;
            coordinateSpace = null;
        }

        private void OnDisable()
        {
            resizing = false;
            coordinateSpace = null;
        }
    }
}