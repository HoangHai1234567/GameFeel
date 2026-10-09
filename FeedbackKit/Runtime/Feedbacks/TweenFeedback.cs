using System;
using DG.Tweening;
using UnityEngine;

namespace FeedbackKit
{
    /// <summary>Lớp gốc cho feedback chạy bằng một tween DOTween.</summary>
    [Serializable]
    public abstract class TweenFeedback : Feedback
    {
        [Min(0f)] public float Seconds = 0.2f;

        [NonSerialized] protected Tween ActiveTween;

        public override float Duration => Seconds;

        protected bool TweenRunning => ActiveTween != null && ActiveTween.IsActive();

        protected float ScaledSeconds(FeedbackContext ctx) => Seconds * ctx.DurationMultiplier;

        /// <summary>Gắn tween vào feedback: chạy theo time mode của player và tự kill khi player bị huỷ.</summary>
        protected void Track(Tween tween, FeedbackContext ctx)
        {
            if (ActiveTween != null && ActiveTween != tween && ActiveTween.IsActive()) ActiveTween.Kill();
            ActiveTween = tween;
            tween.SetUpdate(ctx.Unscaled);
            if (Owner != null) tween.SetLink(Owner.gameObject);
        }

        protected void KillTween()
        {
            if (ActiveTween != null && ActiveTween.IsActive()) ActiveTween.Kill();
            ActiveTween = null;
        }

        protected override void OnStop()
        {
            KillTween();
        }
    }
}
