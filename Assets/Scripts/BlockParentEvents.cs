using UnityEngine;
using UnityEngine.EventSystems;

// Attach to any UI element to stop pointer events from bubbling to parent
public class BlockParentEvents : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerEnter(PointerEventData e) => e.Use();
    public void OnPointerExit(PointerEventData e) => e.Use();
    public void OnPointerClick(PointerEventData e) => e.Use();
    public void OnPointerDown(PointerEventData e) => e.Use();
    public void OnPointerUp(PointerEventData e) => e.Use();
}
