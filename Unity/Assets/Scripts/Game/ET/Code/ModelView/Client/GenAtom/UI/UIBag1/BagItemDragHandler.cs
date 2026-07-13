using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ET.Client
{
    [EnableClass]
    [DisallowMultipleComponent]
    public sealed class BagItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private bool sourceIsDrop;
        private RectTransform dropArea;
        private RectTransform bagArea;
        private Action<BagItemDragHandler, bool> itemDropped;
        private GameObject dragVisual;
        private RectTransform dragCanvasRect;
        private Canvas dragCanvas;
        private Selectable selectable;
        private bool selectableWasInteractable;

        public object UserData { get; private set; }

        public void Initialize(
            bool isDropItem,
            RectTransform dropGridArea,
            RectTransform bagGridArea,
            object userData,
            Action<BagItemDragHandler, bool> onItemDropped)
        {
            sourceIsDrop = isDropItem;
            dropArea = dropGridArea;
            bagArea = bagGridArea;
            UserData = userData;
            itemDropped = onItemDropped;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            dragCanvasRect = dragCanvas != null ? dragCanvas.transform as RectTransform : null;
            if (dragCanvasRect == null)
            {
                return;
            }

            selectable = GetComponent<Selectable>();
            if (selectable != null)
            {
                selectableWasInteractable = selectable.interactable;
                selectable.interactable = false;
            }

            dragVisual = Instantiate(gameObject, dragCanvasRect, false);
            dragVisual.name = "BagItemDragVisual";

            BagItemDragHandler visualHandler = dragVisual.GetComponent<BagItemDragHandler>();
            if (visualHandler != null)
            {
                visualHandler.enabled = false;
            }

            Selectable visualSelectable = dragVisual.GetComponent<Selectable>();
            if (visualSelectable != null)
            {
                visualSelectable.interactable = false;
            }

            CanvasGroup canvasGroup = dragVisual.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = dragVisual.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0.8f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            RectTransform sourceRect = transform as RectTransform;
            RectTransform visualRect = dragVisual.transform as RectTransform;
            if (sourceRect != null && visualRect != null)
            {
                visualRect.anchorMin = new Vector2(0.5f, 0.5f);
                visualRect.anchorMax = new Vector2(0.5f, 0.5f);
                visualRect.pivot = sourceRect.pivot;
                visualRect.sizeDelta = sourceRect.rect.size;
                visualRect.localScale = Vector3.one;
                visualRect.SetAsLastSibling();
            }

            UpdateDragVisual(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateDragVisual(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            bool droppedInOtherGrid = sourceIsDrop
                    ? IsPointerInside(bagArea, eventData)
                    : IsPointerInside(dropArea, eventData);

            FinishDrag();
            if (droppedInOtherGrid)
            {
                itemDropped?.Invoke(this, !sourceIsDrop);
            }
        }

        private void UpdateDragVisual(PointerEventData eventData)
        {
            if (dragVisual == null || dragCanvasRect == null)
            {
                return;
            }

            Camera eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : dragCanvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragCanvasRect,
                    eventData.position,
                    eventCamera,
                    out Vector2 localPoint))
            {
                dragVisual.transform.localPosition = localPoint;
            }
        }

        private static bool IsPointerInside(RectTransform area, PointerEventData eventData)
        {
            return area != null && RectTransformUtility.RectangleContainsScreenPoint(
                area,
                eventData.position,
                eventData.pressEventCamera);
        }

        private void FinishDrag()
        {
            if (selectable != null)
            {
                selectable.interactable = selectableWasInteractable;
                selectable = null;
            }

            if (dragVisual != null)
            {
                Destroy(dragVisual);
                dragVisual = null;
            }

            dragCanvas = null;
            dragCanvasRect = null;
        }

        private void OnDisable()
        {
            FinishDrag();
            dropArea = null;
            bagArea = null;
            UserData = null;
            itemDropped = null;
        }
    }
}
