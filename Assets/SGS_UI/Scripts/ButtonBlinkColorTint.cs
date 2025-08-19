using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(Button))]
public class ButtonBlinkColorTint : MonoBehaviour
{
    [Header("Blink")]
    public int blinkCount = 3;
    public float onTime  = 0.07f;   // Highlighted 유지
    public float offTime = 0.07f;   // Pressed 유지
    public bool reactivateAfter = true; // 깜빡임 후 버튼 다시 활성화할지

    [Header("Callback")]
    public UnityEvent onExecute;     // 깜빡임 끝난 뒤 실행할 동작

    Button btn;
    Graphic target;                  // Button.targetGraphic
    Selectable.Transition originalTransition;
    ColorBlock cb;                   // 버튼의 Color Tint 세트
    Color originalColor;             // 현재 렌더러 색 (복구용)
    bool busy;

    void Awake()
    {
        btn = GetComponent<Button>();
        target = btn.targetGraphic;              // 보통 Image나 TMP SubMesh 등 Graphic
        originalTransition = btn.transition;
        cb = btn.colors;
        originalColor = target != null ? target.color : Color.white;
    }

    // Button.OnClick에 이 함수를 연결하세요.
    public void OnClickBlinkThenExecute()
    {
        if (!gameObject.activeInHierarchy || busy) return;
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        busy = true;

        // 상태머신 간섭 방지를 위해 잠시 꺼두고(색은 직접 지정)
        btn.interactable = false;
        btn.transition   = Selectable.Transition.None;

        // 깜빡임: Highlighted ↔ Pressed 반복
        for (int i = 0; i < blinkCount; i++)
        {
            SetColor(cb.highlightedColor);
            yield return new WaitForSecondsRealtime(onTime);

            SetColor(cb.pressedColor);
            yield return new WaitForSecondsRealtime(offTime);
        }

        // 정상 색으로 복귀
        SetColor(cb.normalColor);

        // 원래 트랜지션 복원
        btn.transition = originalTransition;
        if (reactivateAfter) btn.interactable = true;

        // 실제 동작 실행
        onExecute?.Invoke();

        busy = false;
    }

    void SetColor(Color c)
    {
        if (target == null) return;
        // 즉시 적용(페이드 없이). 페이드 원하면 CrossFadeColor 사용 가능.
        target.color = c;
    }
}
