using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class WorldSpaceMenuTransition : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera mainCam;

    [Header("Stages (exactly 3)")]
    [Tooltip("UI 루트(CanvasGroup) 3개: [0]=UI1, [1]=UI2, [2]=UI3")]
    [SerializeField] private CanvasGroup[] stageGroups = new CanvasGroup[3];

    [Tooltip("엔드포인트 3개: [0]=UI2 앞, [1]=UI3 앞, [2]=최종 포인트(UI3 통과 후)")]
    [SerializeField] private Transform[] endPoints = new Transform[3];

    [Header("Motion")]
    [SerializeField] private float moveDuration = 1.2f;
    [SerializeField] private AnimationCurve ease = null; // 없으면 선형
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Optional Fade (Overlay Canvas)")]
    [SerializeField] private Image fadeImage;      // 검정 Image(알파 0으로 시작)
    [SerializeField] private float midMaxFade = 0.25f; // 중간 단계에서 최대 알파
    [SerializeField] private float midFadeStartAt = 0.85f; // 이동 진행률 기준

    [Header("Final Scene")]
    [SerializeField] private string nextSceneName = "Battle Scene";
    [SerializeField] private bool asyncLoad = true;      // true 권장
    [SerializeField] private float finalFadeInTime = 0.35f;  // 마지막엔 완전 암전으로
    [SerializeField] private float finalHoldBlack = 0.1f;    // 완전 암전 유지 시간

    int currentStage = 0; // 0 → UI1, 1 → UI2, 2 → UI3
    bool busy;

    void Awake()
    {
        // 초기 상태: UI1만 인터랙션 ON
        for (int i = 0; i < stageGroups.Length; i++)
        {
            bool active = (i == 0);
            if (stageGroups[i] != null)
            {
                stageGroups[i].alpha = 1f;
                stageGroups[i].interactable   = active;
                stageGroups[i].blocksRaycasts = active;
            }
        }

        if (fadeImage != null)
            fadeImage.color = new Color(0, 0, 0, 0);
    }

    /// <summary>
    /// 모든 "다음" 버튼에 이 함수를 연결하면 됨.
    /// - 현재가 UI1(0) → UI2(1) 앞으로 이동
    /// - 현재가 UI2(1) → UI3(2) 앞으로 이동
    /// - 현재가 UI3(2) → 엔드포인트3(2)로 이동 후 다음 씬
    /// 
    /// </summary>
    public void OnClickNext()
    {
        if (busy) return;

        if (currentStage < 2)
        {
            // 다음 UI로 이동
            StartCoroutine(MoveToStage(currentStage + 1));
        }
        else
        {
            // 최종: 엔드포인트3 이동 후 씬 전환
            StartCoroutine(MoveToFinalAndLoad());
        }
    }

    IEnumerator MoveToStage(int targetStage)
{
    busy = true;

    // 현재 UI 입력 잠금
    if (stageGroups[currentStage] != null)
    {
        stageGroups[currentStage].interactable = false;
        stageGroups[currentStage].blocksRaycasts = false;
    }

    // 🔧 엔드포인트 인덱스 보정: 0→(없음), 1→0, 2→1
    int endpointIndex = Mathf.Clamp(targetStage - 1, 0, endPoints.Length - 1);

    // 카메라 전진: UI2앞/ UI3앞 으로 이동
    yield return MoveCamera(endPoints[endpointIndex], /*finalStep*/ false);
    // stageGroups: [0]=UI1, [1]=UI2, [2]=UI3
    // endPoints  : [0]=UI2 앞, [1]=UI3 앞, [2]=최종(통과 후)

    // 다음 UI 입력 활성화
        currentStage = targetStage;
    if (stageGroups[currentStage] != null)
    {
        stageGroups[currentStage].interactable = true;
        stageGroups[currentStage].blocksRaycasts = true;
    }

    busy = false;
}

    IEnumerator MoveToFinalAndLoad()
    {
        busy = true;

        // 마지막 UI 입력 잠금
        if (stageGroups[currentStage] != null)
        {
            stageGroups[currentStage].interactable = false;
            stageGroups[currentStage].blocksRaycasts = false;
        }

        // 최종 전진(완전 암전)
        yield return MoveCamera(endPoints[2], /*finalStep*/ true);

        // 완전 암전 잠깐 유지
        if (fadeImage != null && finalHoldBlack > 0f)
        {
            float t = 0f;
            while (t < finalHoldBlack)
            {
                t += Delta();
                yield return null;
            }
        }

        // 씬 로딩
        if (asyncLoad)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
            op.allowSceneActivation = true; // 이미 완전 암전 상태
            // 보통 바로 넘어가지만, 안전하게 한 프레임 대기
            yield return null;
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }

        busy = false;
    }

    IEnumerator MoveCamera(Transform target, bool finalStep)
    {
        if (target == null || mainCam == null) yield break;

        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;

        Vector3 endPos = target.position;
        Quaternion endRot = target.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Delta() / moveDuration;
            float k = (ease != null) ? ease.Evaluate(Mathf.Clamp01(t)) : Mathf.Clamp01(t);

            mainCam.transform.position = Vector3.Lerp(startPos, endPos, k);
            mainCam.transform.rotation = Quaternion.Slerp(startRot, endRot, k);

            // 페이드 처리
            if (fadeImage != null)
            {
                if (!finalStep)
                {
                    if (k >= midFadeStartAt)
                    {
                        float f = Mathf.InverseLerp(midFadeStartAt, 1f, k);
                        SetFade(Mathf.Lerp(0f, midMaxFade, f));
                    }
                }
                else
                {
                    // 최종 단계: 완전 암전으로 부드럽게
                    float a = Mathf.Clamp01(k / Mathf.Max(0.0001f, finalFadeInTime / moveDuration));
                    SetFade(a);
                }
            }

            yield return null;
        }
    }

    float Delta() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    void SetFade(float a)
    {
        if (fadeImage == null) return;
        var c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}
