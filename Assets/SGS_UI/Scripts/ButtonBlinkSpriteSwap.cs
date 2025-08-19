using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(Button), typeof(Image))]
public class ButtonBlinkSpriteSwap : MonoBehaviour
{
    [Header("Blink Settings")]
    public int blinkCount = 3;
    public float onTime  = 0.07f;   // Highlighted 유지 시간
    public float offTime = 0.07f;   // Pressed 유지 시간

    [Header("Callback")]
    public UnityEvent onExecute;    // 깜빡임 후 실행할 동작(씬 전환 등)

    Button  btn;
    Image   img;

    // 원상 복구용
    Selectable.Transition originalTransition;
    Sprite originalSprite;
    Sprite highlightedSprite;
    Sprite pressedSprite;

    void Awake()
    {
        btn = GetComponent<Button>();
        img = GetComponent<Image>();

        originalTransition = btn.transition;
        originalSprite     = img.sprite;

        var st = btn.spriteState;
        highlightedSprite  = st.highlightedSprite;
        pressedSprite      = st.pressedSprite;
    }

    public void OnClickBlinkThenExecute()
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        // Sprite Swap 상태머신이 간섭하지 않게 잠깐 끔
        btn.interactable = false;
        btn.transition   = Selectable.Transition.None;

        // 깜빡임
        for (int i = 0; i < blinkCount; i++)
        {
            // Highlighted처럼 보이기
            if (highlightedSprite) img.overrideSprite = highlightedSprite;
            else                   img.overrideSprite = originalSprite;
            yield return new WaitForSecondsRealtime(onTime);

            // Pressed처럼 보이기
            if (pressedSprite) img.overrideSprite = pressedSprite;
            else               img.overrideSprite = originalSprite;
            yield return new WaitForSecondsRealtime(offTime);
        }

        // 원상 복구
        img.overrideSprite = null;          // state 머신으로 복귀
        img.sprite         = originalSprite;
        btn.transition     = originalTransition;
        btn.interactable   = true;

        // 실제 동작 실행
        onExecute?.Invoke();
    }
}
