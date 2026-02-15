using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonAnim : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    public Animator animator;

    public void OnPointerEnter(PointerEventData eventData)
    {
        animator.SetBool("isHover", true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        animator.SetBool("isHover", false);
        animator.SetBool("isPressed", false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        animator.SetBool("isPressed", true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        animator.SetBool("isPressed", false);
    }
}
