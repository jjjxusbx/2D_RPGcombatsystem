using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UGUIEventListener : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler,
    IInitializePotentialDragHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler,
    IScrollHandler,
    ISelectHandler,
    IDeselectHandler,
    IMoveHandler,
    ISubmitHandler,
    ICancelHandler
{
    [Serializable] public class PointerEvent : UnityEvent<PointerEventData> { }
    [Serializable] public class BaseEvent : UnityEvent<BaseEventData> { }
    [Serializable] public class AxisEvent : UnityEvent<AxisEventData> { }
    #region 属性
    public PointerEvent onPointerEnter = new PointerEvent();
    public PointerEvent onPointerExit = new PointerEvent();
    public PointerEvent onPointerDown = new PointerEvent();
    public PointerEvent onPointerUp = new PointerEvent();
    public PointerEvent onPointerClick = new PointerEvent();
    public PointerEvent onInitializePotentialDrag = new PointerEvent();
    public PointerEvent onBeginDrag = new PointerEvent();
    public PointerEvent onDrag = new PointerEvent();
    public PointerEvent onEndDrag = new PointerEvent();
    public PointerEvent onDrop = new PointerEvent();
    public PointerEvent onScroll = new PointerEvent();
    public BaseEvent onSelect = new BaseEvent();
    public BaseEvent onDeselect = new BaseEvent();
    public AxisEvent onMove = new AxisEvent();
    public BaseEvent onSubmit = new BaseEvent();
    public BaseEvent onCancel = new BaseEvent();
    #endregion
    public static UGUIEventListener Get(GameObject target)
    {
        UGUIEventListener listener = target.GetComponent<UGUIEventListener>();
        if (listener == null)
        {
            listener = target.AddComponent<UGUIEventListener>();
        }

        return listener;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onPointerEnter.Invoke(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onPointerExit.Invoke(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        onPointerDown.Invoke(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        onPointerUp.Invoke(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onPointerClick.Invoke(eventData);
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        onInitializePotentialDrag.Invoke(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        onBeginDrag.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        onDrag.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        onEndDrag.Invoke(eventData);
    }

    public void OnDrop(PointerEventData eventData)
    {
        onDrop.Invoke(eventData);
    }

    public void OnScroll(PointerEventData eventData)
    {
        onScroll.Invoke(eventData);
    }

    public void OnSelect(BaseEventData eventData)
    {
        onSelect.Invoke(eventData);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        onDeselect.Invoke(eventData);
    }

    public void OnMove(AxisEventData eventData)
    {
        onMove.Invoke(eventData);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        onSubmit.Invoke(eventData);
    }

    public void OnCancel(BaseEventData eventData)
    {
        onCancel.Invoke(eventData);
    }
}
