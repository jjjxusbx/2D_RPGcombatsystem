using UnityEngine;
using UnityEngine.EventSystems;

public class Test4 : MonoBehaviour, IPointerEnterHandler
{
    public void PointerEnter(BaseEventData eventData)
    {
        Debug.Log("Pointer Enter: " + gameObject.name);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnter(eventData);
    }
}
