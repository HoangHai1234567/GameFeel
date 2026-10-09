using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FeedbackKit
{
    /// <summary>
    /// Nút nhún: thu nhỏ ngay khi ngón tay chạm xuống, nảy về (OutBack) khi nhả; kéo ra ngoài thì bật về.
    /// Nút không tương tác được thì rung ngang báo "không được".
    ///
    /// Gắn CÙNG GameObject với Button. <see cref="Visual"/> phải là một child (thứ được scale) —
    /// không scale chính object chứa Graphic nhận raycast, nếu không vùng chạm co lại và click bị mất.
    /// </summary>
    [AddComponentMenu("FeedbackKit/Juicy Button")]
    [DisallowMultipleComponent]
    public class JuicyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Child được scale/rung. Để trống = child đầu tiên.")]
        public RectTransform Visual;

        [Tooltip("Để trống = Selectable (Button) trên cùng object.")]
        public Selectable Selectable;

        [Header("Press (pointer down)")]
        [Range(0.8f, 1f)] public float PressScale = 0.92f;
        [Min(0f)] public float PressSeconds = 0.08f;
        public Ease PressEase = Ease.OutQuad;

        [Header("Release (pointer up / exit)")]
        [Min(0f)] public float ReleaseSeconds = 0.2f;
        public Ease ReleaseEase = Ease.OutBack;
        [Tooltip("Overshoot của OutBack. 1.70158 ≈ vượt 10%; 1.0–1.2 nhẹ hơn.")]
        public float ReleaseOvershoot = 1.5f;

        [Header("Denied (nút không tương tác được)")]
        public bool ShakeWhenNotInteractable = true;
        public float DeniedShakeStrength = 10f;
        [Min(0f)] public float DeniedShakeSeconds = 0.3f;

        [Header("Feedback players (tuỳ chọn)")]
        [Tooltip("Phát khi ngón tay chạm xuống (tick nhỏ, haptic Selection).")]
        public FeedbackPlayer DownFeedback;
        [Tooltip("Phát khi click thành công (âm click, haptic Light).")]
        public FeedbackPlayer ClickFeedback;
        [Tooltip("Phát khi chạm vào nút không tương tác được (âm từ chối, haptic Warning).")]
        public FeedbackPlayer DeniedFeedback;

        [Tooltip("Chạy cả khi Time.timeScale = 0 (menu pause).")]
        public bool UseUnscaledTime = true;

        Vector3 _restScale = Vector3.one;
        Vector3 _restPosition;
        bool _pressed;
        Tween _scaleTween;
        Tween _shakeTween;
        Button _button;

        bool Interactable => Selectable == null || Selectable.IsInteractable();

        void Awake()
        {
            if (Visual == null && transform.childCount > 0) Visual = transform.GetChild(0) as RectTransform;
            if (Selectable == null) Selectable = GetComponent<Selectable>();
            if (Visual != null)
            {
                _restScale = Visual.localScale;
                _restPosition = Visual.localPosition;
            }

            _button = Selectable as Button;
            if (_button != null) _button.onClick.AddListener(HandleClick);
        }

        void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(HandleClick);
            KillTweens();
        }

        void OnDisable()
        {
            _pressed = false;
            KillTweens();
            if (Visual != null)
            {
                Visual.localScale = _restScale;
                Visual.localPosition = _restPosition;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!Interactable)
            {
                Denied();
                return;
            }

            _pressed = true;
            AnimateScale(_restScale * PressScale, PressSeconds, PressEase, 0f);
            if (DownFeedback != null) DownFeedback.Play();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Kéo ngón tay ra rồi kéo lại vào khi vẫn đang giữ: nhấn lại.
            if (_pressed || !Interactable) return;
            if (eventData.pointerPress == gameObject && eventData.eligibleForClick)
            {
                _pressed = true;
                AnimateScale(_restScale * PressScale, PressSeconds, PressEase, 0f);
            }
        }

        void Release()
        {
            if (!_pressed) return;
            _pressed = false;
            AnimateScale(_restScale, ReleaseSeconds, ReleaseEase, ReleaseOvershoot);
        }

        void HandleClick()
        {
            if (ClickFeedback != null) ClickFeedback.Play();
        }

        void Denied()
        {
            if (DeniedFeedback != null) DeniedFeedback.Play();
            if (!ShakeWhenNotInteractable || Visual == null) return;
            if (FeedbackGlobal.ReduceMotion) return;

            if (_shakeTween != null && _shakeTween.IsActive()) _shakeTween.Kill();
            Visual.localPosition = _restPosition;
            RectTransform visual = Visual;
            Vector3 rest = _restPosition;
            _shakeTween = visual.DOShakePosition(DeniedShakeSeconds, new Vector3(DeniedShakeStrength, 0f, 0f), 15, 0f, false, true)
                .SetUpdate(UseUnscaledTime)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    if (visual != null) visual.localPosition = rest;
                });
        }

        void AnimateScale(Vector3 target, float seconds, Ease ease, float overshoot)
        {
            if (Visual == null) return;
            if (_scaleTween != null && _scaleTween.IsActive()) _scaleTween.Kill();

            Tween t = Visual.DOScale(target, seconds).SetUpdate(UseUnscaledTime).SetLink(gameObject);
            if (overshoot > 0f) t.SetEase(ease, overshoot);
            else t.SetEase(ease);
            _scaleTween = t;
        }

        void KillTweens()
        {
            if (_scaleTween != null && _scaleTween.IsActive()) _scaleTween.Kill();
            if (_shakeTween != null && _shakeTween.IsActive()) _shakeTween.Kill();
            _scaleTween = null;
            _shakeTween = null;
        }
    }
}
