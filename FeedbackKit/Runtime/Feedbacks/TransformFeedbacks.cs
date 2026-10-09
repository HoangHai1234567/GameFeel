using System;
using DG.Tweening;
using UnityEngine;

namespace FeedbackKit
{
    /// <summary>
    /// Nảy scale rồi về. Chạm liên tục không làm scale trôi: mỗi lần play đưa về scale nghỉ trước.
    /// </summary>
    [Serializable, FeedbackMenu("Transform/Scale Punch")]
    public class ScalePunchFeedback : TweenFeedback
    {
        [Tooltip("Để trống = transform của player. Nên trỏ vào child Visual, không phải object chứa logic/hitbox.")]
        public Transform Target;

        [Tooltip("Lượng cộng thêm vào scale ở đỉnh (0.15 = phóng 15%). Nhân với intensity.")]
        public Vector3 Punch = new Vector3(0.15f, 0.15f, 0.15f);

        [Range(1, 20)] public int Vibrato = 6;

        [Range(0f, 1f)] public float Elasticity = 0.6f;

        [NonSerialized] Vector3 _rest;

        public ScalePunchFeedback()
        {
            Seconds = 0.25f;
        }

        public override bool IsMotion => true;

        protected override void OnInitialize()
        {
            if (Target == null) Target = Owner.transform;
            _rest = Target.localScale;
        }

        protected override void OnPlay(FeedbackContext ctx)
        {
            if (Target == null) return;
            bool running = TweenRunning;
            KillTween();
            if (running) Target.localScale = _rest;
            else _rest = Target.localScale;

            Track(Target.DOPunchScale(Punch * ctx.Intensity, ScaledSeconds(ctx), Vibrato, Elasticity), ctx);
        }

        protected override void OnStop()
        {
            bool running = TweenRunning;
            KillTween();
            if (running && Target != null) Target.localScale = _rest;
        }

        protected override void OnRestore()
        {
            KillTween();
            if (Target != null) Target.localScale = _rest;
        }
    }

    /// <summary>
    /// Scale từ From tới To (popup mở: 0.85 → 1, OutBack). Chạy ngược thì đi To → From với ReverseEase.
    /// Không dùng intensity.
    /// </summary>
    [Serializable, FeedbackMenu("Transform/Scale Tween")]
    public class ScaleTweenFeedback : TweenFeedback
    {
        public Transform Target;

        [Tooltip("Bật: From/To là hệ số nhân với scale gốc. Tắt: giá trị scale tuyệt đối.")]
        public bool RelativeToRest = true;

        public Vector3 From = new Vector3(0.85f, 0.85f, 0.85f);

        public Vector3 To = Vector3.one;

        public Ease Ease = Ease.OutBack;

        [Tooltip("Chỉ dùng cho Back/Elastic. 1.70158 ≈ vượt 10%; 1.0–1.2 nhẹ hơn.")]
        public float Overshoot = 1.70158f;

        [Tooltip("Ease khi player chạy ngược (đóng) — thường là ease-in.")]
        public Ease ReverseEase = Ease.InCubic;

        [NonSerialized] Vector3 _rest;

        public ScaleTweenFeedback()
        {
            Seconds = 0.3f;
        }

        protected override void OnInitialize()
        {
            if (Target == null) Target = Owner.transform;
            _rest = Target.localScale;
        }

        Vector3 Resolve(Vector3 v) => RelativeToRest ? Vector3.Scale(_rest, v) : v;

        protected override void OnPlay(FeedbackContext ctx)
        {
            if (Target == null) return;
            Vector3 a = Resolve(ctx.Reverse ? To : From);
            Vector3 b = Resolve(ctx.Reverse ? From : To);

            KillTween();
            Target.localScale = a;
            Tween t = Target.DOScale(b, ScaledSeconds(ctx));
            if (ctx.Reverse) t.SetEase(ReverseEase);
            else t.SetEase(Ease, Overshoot);
            Track(t, ctx);
        }

        protected override void OnSkip(bool reverse)
        {
            KillTween();
            if (Target != null) Target.localScale = Resolve(reverse ? From : To);
        }

        protected override void OnRestore()
        {
            KillTween();
            if (Target != null) Target.localScale = _rest;
        }
    }

    /// <summary>
    /// Co giãn giữ thể tích theo một trục (ô rơi chạm đáy, vật được nhấc lên…).
    /// Curve mặc định: giãn → bẹp → giãn nhẹ → về.
    /// </summary>
    [Serializable, FeedbackMenu("Transform/Squash & Stretch")]
    public class SquashStretchFeedback : TweenFeedback
    {
        public enum Axis
        {
            X,
            Y,
            Z
        }

        public Transform Target;

        [Tooltip("Trục bị kéo giãn; các trục còn lại co lại để giữ diện tích / thể tích.")]
        public Axis StretchAxis = Axis.Y;

        [Tooltip("Biên độ ở đỉnh curve (0.2 = giãn 20%). Âm = bẹp trước. Nhân với intensity.")]
        public float Amount = 0.2f;

        [Tooltip("Bật: giữ diện tích (2D/UI). Tắt: giữ thể tích (3D).")]
        public bool PreserveArea2D = true;

        [Tooltip("Hình dạng theo thời gian (0→1); giá trị 1 = Amount.")]
        public AnimationCurve Curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.55f, -0.45f),
            new Keyframe(0.8f, 0.15f),
            new Keyframe(1f, 0f));

        [NonSerialized] Vector3 _rest;

        public SquashStretchFeedback()
        {
            Seconds = 0.3f;
        }

        public override bool IsMotion => true;

        protected override void OnInitialize()
        {
            if (Target == null) Target = Owner.transform;
            _rest = Target.localScale;
        }

        protected override void OnPlay(FeedbackContext ctx)
        {
            if (Target == null) return;
            bool running = TweenRunning;
            KillTween();
            if (running) Target.localScale = _rest;
            else _rest = Target.localScale;

            float amount = Amount * ctx.Intensity;
            Tween t = DOVirtual.Float(0f, 1f, ScaledSeconds(ctx), v => Apply(Curve.Evaluate(v) * amount))
                .SetEase(Ease.Linear)
                .OnComplete(() => Apply(0f));
            Track(t, ctx);
        }

        void Apply(float v)
        {
            if (Target == null) return;
            float main = Mathf.Max(0.05f, 1f + v);
            float side = PreserveArea2D ? 1f / main : 1f / Mathf.Sqrt(main);
            var s = new Vector3(side, side, side);
            switch (StretchAxis)
            {
                case Axis.X: s.x = main; break;
                case Axis.Y: s.y = main; break;
                default: s.z = main; break;
            }
            if (PreserveArea2D && StretchAxis != Axis.Z) s.z = 1f;
            Target.localScale = Vector3.Scale(_rest, s);
        }

        protected override void OnStop()
        {
            bool running = TweenRunning;
            KillTween();
            if (running && Target != null) Target.localScale = _rest;
        }

        protected override void OnRestore()
        {
            KillTween();
            if (Target != null) Target.localScale = _rest;
        }
    }

    /// <summary>Đẩy vị trí (local) rồi nảy về. UI: đơn vị pixel; world: đơn vị world.</summary>
    [Serializable, FeedbackMenu("Transform/Position Punch")]
    public class PositionPunchFeedback : TweenFeedback
    {
        public Transform Target;

        [Tooltip("Hướng và biên độ (local). UI ví dụ (0, 12, 0); world ví dụ (0, 0.15, 0). Nhân với intensity.")]
        public Vector3 Punch = new Vector3(0f, 12f, 0f);

        [Range(1, 20)] public int Vibrato = 6;

        [Range(0f, 1f)] public float Elasticity = 0.5f;

        [NonSerialized] Vector3 _rest;

        public PositionPunchFeedback()
        {
            Seconds = 0.25f;
        }

        public override bool IsMotion => true;

        protected override void OnInitialize()
        {
            if (Target == null) Target = Owner.transform;
            _rest = Target.localPosition;
        }

        protected override void OnPlay(FeedbackContext ctx)
        {
            if (Target == null) return;
            bool running = TweenRunning;
            KillTween();
            if (running) Target.localPosition = _rest;
            else _rest = Target.localPosition;

            Track(Target.DOPunchPosition(Punch * ctx.Intensity, ScaledSeconds(ctx), Vibrato, Elasticity), ctx);
        }

        protected override void OnStop()
        {
            bool running = TweenRunning;
            KillTween();
            if (running && Target != null) Target.localPosition = _rest;
        }

        protected override void OnRestore()
        {
            KillTween();
            if (Target != null) Target.localPosition = _rest;
        }
    }

    /// <summary>Lắc xoay rồi về (icon, mascot, thẻ).</summary>
    [Serializable, FeedbackMenu("Transform/Rotation Punch")]
    public class RotationPunchFeedback : TweenFeedback
    {
        public Transform Target;

        [Tooltip("Góc (độ) ở đỉnh. Nhân với intensity.")]
        public Vector3 Punch = new Vector3(0f, 0f, 12f);

        [Range(1, 20)] public int Vibrato = 6;

        [Range(0f, 1f)] public float Elasticity = 0.5f;

        [NonSerialized] Quaternion _rest;

        public RotationPunchFeedback()
        {
            Seconds = 0.3f;
        }

        public override bool IsMotion => true;

        protected override void OnInitialize()
        {
            if (Target == null) Target = Owner.transform;
            _rest = Target.localRotation;
        }

        protected override void OnPlay(FeedbackContext ctx)
        {
            if (Target == null) return;
            bool running = TweenRunning;
            KillTween();
            if (running) Target.localRotation = _rest;
            else _rest = Target.localRotation;

            Track(Target.DOPunchRotation(Punch * ctx.Intensity, ScaledSeconds(ctx), Vibrato, Elasticity), ctx);
        }

        protected override void OnStop()
        {
            bool running = TweenRunning;
            KillTween();
            if (running && Target != null) Target.localRotation = _rest;
        }

        protected override void OnRestore()
        {
            KillTween();
            if (Target != null) Target.localRotation = _rest;
        }
    }

    /// <summary>
    /// Rung vị trí (local). Báo thao tác bị từ chối, hoặc rung container UI khi nhận thưởng.
    /// Bị bỏ qua khi bật Reduce motion (tuỳ chọn).
    /// </summary>
    [Serializable, FeedbackMenu("Transform/Shake")]
    public class ShakeFeedback : TweenFeedback
    {
        public Transform Target;

        [Tooltip("Biên độ (local). Từ chối trên UI: (10, 0, 0). Rung container UI: (8, 8, 0). Nhân với intensity.")]
        public Vector3 Strength = new Vector3(10f, 0f, 0f);

        [Range(1, 50)] public int Vibrato = 15;

        [Tooltip("0 = lắc qua lại đều; > 90 trông lộn xộn.")]
        [Range(0f, 180f)] public float Randomness = 10f;

        public bool FadeOut = true;

        [Tooltip("Bỏ qua hoàn toàn khi người chơi bật Reduce motion.")]
        public bool SkipWhenReduceMotion = true;

        [NonSerialized] Vector3 _rest;

        public ShakeFeedback()
        {
            Seconds = 0.3f;
        }

        public override bool IsMotion => true;

        protected override void OnInitialize()
        {
            if (Target == null) Target = Owner.transform;
            _rest = Target.localPosition;
        }

        protected override void OnPlay(FeedbackContext ctx)
        {
            if (Target == null) return;
            if (SkipWhenReduceMotion && FeedbackGlobal.ReduceMotion) return;

            bool running = TweenRunning;
            KillTween();
            if (running) Target.localPosition = _rest;
            else _rest = Target.localPosition;

            Tween t = Target.DOShakePosition(ScaledSeconds(ctx), Strength * ctx.Intensity, Vibrato, Randomness, false, FadeOut)
                .OnComplete(() =>
                {
                    if (Target != null) Target.localPosition = _rest;
                });
            Track(t, ctx);
        }

        protected override void OnStop()
        {
            bool running = TweenRunning;
            KillTween();
            if (running && Target != null) Target.localPosition = _rest;
        }

        protected override void OnRestore()
        {
            KillTween();
            if (Target != null) Target.localPosition = _rest;
        }
    }
}
