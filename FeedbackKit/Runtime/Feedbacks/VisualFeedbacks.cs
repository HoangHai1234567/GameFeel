using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FeedbackKit
{
    /// <summary>
    /// Fade alpha của CanvasGroup (popup, panel, nút). Chạy ngược = fade theo chiều ngược lại.
    /// Không dùng intensity.
    /// </summary>
    [Serializable, FeedbackMenu("UI/CanvasGroup Fade")]
    public class CanvasGroupFadeFeedback : TweenFeedback
    {
        [Tooltip("Để trống = CanvasGroup trên object của player.")]
        public CanvasGroup Target;

        [Range(0f, 1f)] public float From;

        [Range(0f, 1f)] public float To = 1f;

        public Ease Ease = Ease.OutQuad;

        public Ease ReverseEase = Ease.InQuad;

        [Tooltip("Đích là ẩn (alpha ≈ 0): tắt interactable/blocksRaycasts ngay từ đầu. Đích là hiện: bật lại khi kết thúc.")]
        public bool ControlRaycasts = true;

        [NonSerialized] float _rest = 1f;

        public CanvasGroupFadeFeedback()
        {
            Seconds = 0.25f;
        }

        protected override void OnInitialize()
        {
            if (Target == null) Target = Owner.GetComponent<CanvasGroup>();
            if (Target != null) _rest = Target.alpha;
        }

        protected override void OnPlay(FeedbackContext ctx)
        {
            if (Target == null) return;
            float a = ctx.Reverse ? To : From;
            float b = ctx.Reverse ? From : To;
            bool visibleAtEnd = b > 0.01f;

            KillTween();
            Target.alpha = a;
            if (ControlRaycasts && !visibleAtEnd) SetRaycasts(false);

            CanvasGroup group = Target;
            Tween t = DOTween.To(() => group.alpha, x => group.alpha = x, b, ScaledSeconds(ctx))
                .SetEase(ctx.Reverse ? ReverseEase : Ease);
            if (ControlRaycasts && visibleAtEnd) t.OnComplete(() => SetRaycasts(true));
            Track(t, ctx);
        }

        protected override void OnSkip(bool reverse)
        {
            KillTween();
            if (Target == null) return;
            float b = reverse ? From : To;
            Target.alpha = b;
            if (ControlRaycasts) SetRaycasts(b > 0.01f);
        }

        protected override void OnRestore()
        {
            KillTween();
            if (Target == null) return;
            Target.alpha = _rest;
            if (ControlRaycasts) SetRaycasts(_rest > 0.01f);
        }

        void SetRaycasts(bool on)
        {
            if (Target == null) return;
            Target.interactable = on;
            Target.blocksRaycasts = on;
        }
    }

    /// <summary>
    /// Pha màu thoáng qua rồi về màu gốc (Graphic hoặc SpriteRenderer).
    /// Lưu ý: color của Image/Sprite là phép nhân với texture — nếu màu gốc đã là trắng thì flash trắng
    /// không thấy gì. Muốn "loé trắng" hãy dùng một lớp overlay trắng + CanvasGroup Fade.
    /// </summary>
    [Serializable, FeedbackMenu("Renderer/Color Flash")]
    public class ColorFlashFeedback : TweenFeedback
    {
        [Tooltip("Dùng một trong hai: Graphic (Image/Text UI) hoặc SpriteRenderer.")]
        public Graphic Graphic;

        public SpriteRenderer Sprite;

        public Color FlashColor = new Color(1f, 0.35f, 0.35f, 1f);

        [Tooltip("Mức pha màu ở đỉnh (0..1). Nhân với intensity.")]
        [Range(0f, 1f)] public float Strength = 0.8f;

        public Ease Ease = Ease.OutQuad;

        [NonSerialized] Color _rest = Color.white;

        public ColorFlashFeedback()
        {
            Seconds = 0.15f;
        }

        bool HasTarget => Graphic != null || Sprite != null;

        Color GetColor()
        {
            if (Graphic != null) return Graphic.color;
            if (Sprite != null) return Sprite.color;
            return Color.white;
        }

        void SetColor(Color c)
        {
            if (Graphic != null) Graphic.color = c;
            else if (Sprite != null) Sprite.color = c;
        }

        protected override void OnInitialize()
        {
            if (HasTarget) _rest = GetColor();
        }

        protected override void OnPlay(FeedbackContext ctx)
        {
            if (!HasTarget) return;
            bool running = TweenRunning;
            KillTween();
            if (running) SetColor(_rest);
            else _rest = GetColor();

            Color rest = _rest;
            Color peak = Color.Lerp(rest, FlashColor, Mathf.Clamp01(Strength * ctx.Intensity));
            peak.a = rest.a;
            SetColor(peak);

            Tween t = DOVirtual.Float(0f, 1f, ScaledSeconds(ctx), v => SetColor(Color.Lerp(peak, rest, v)))
                .SetEase(Ease);
            Track(t, ctx);
        }

        protected override void OnStop()
        {
            bool running = TweenRunning;
            KillTween();
            if (running && HasTarget) SetColor(_rest);
        }

        protected override void OnRestore()
        {
            KillTween();
            if (HasTarget) SetColor(_rest);
        }
    }
}
