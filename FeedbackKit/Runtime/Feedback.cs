using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FeedbackKit
{
    public enum DirectionCondition
    {
        Always,
        OnlyForward,
        OnlyReverse
    }

    /// <summary>Đường dẫn trong menu "+ Add Feedback" của Inspector, ví dụ "Transform/Scale Punch".</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class FeedbackMenuAttribute : Attribute
    {
        public readonly string Path;

        public FeedbackMenuAttribute(string path)
        {
            Path = path;
        }
    }

    /// <summary>Dữ liệu của một lần Play, truyền xuống từng feedback.</summary>
    public struct FeedbackContext
    {
        public FeedbackPlayer Player;
        public Vector3 Position;
        public bool HasPosition;
        public float Intensity;

        /// <summary>Chỉ số bậc (combo / cascade) cho SoundFeedback Ladder. -1 = không dùng.</summary>
        public int Step;

        public bool Reverse;
        public bool Unscaled;
        public float DurationMultiplier;
    }

    [Serializable]
    public class FeedbackTiming
    {
        [Min(0f)] public float InitialDelay;

        [Tooltip("Thời gian tối thiểu giữa hai lần chạy feedback này.")]
        [Min(0f)] public float Cooldown;

        [Min(0)] public int Repeats;

        [Min(0f)] public float RepeatDelay;

        [Tooltip("Chỉ chạy khi player chạy xuôi / chạy ngược.")]
        public DirectionCondition PlayWhen = DirectionCondition.Always;

        [Tooltip("Bỏ qua intensity của player (luôn = 1).")]
        public bool ConstantIntensity;

        [Tooltip("Chỉ chạy khi intensity nằm trong [Min, Max]. Dùng để bật feedback theo bậc combo.")]
        public bool UseIntensityRange;

        public float IntensityMin;

        public float IntensityMax = 1f;

        [Tooltip("Nhân intensity với một giá trị ngẫu nhiên trong khoảng, để mỗi lần hơi khác nhau.")]
        public bool RandomizeIntensity;

        public Vector2 RandomIntensity = new Vector2(0.85f, 1f);

        [Tooltip("HoldingPause không chờ feedback này (dùng cho feedback nền dài).")]
        public bool ExcludeFromHoldingPause;
    }

    /// <summary>
    /// Một hiệu ứng nguyên tử (một âm, một punch, một lần rung…). Feedback nằm trong danh sách
    /// của <see cref="FeedbackPlayer"/>; player quyết định thứ tự và nhịp chạy.
    /// </summary>
    [Serializable]
    public abstract class Feedback
    {
        public bool Active = true;

        public string Label = "";

        [Range(0f, 100f)] public float Chance = 100f;

        public FeedbackTiming Timing = new FeedbackTiming();

        [NonSerialized] protected FeedbackPlayer Owner;
        [NonSerialized] Coroutine _routine;
        [NonSerialized] float _lastPlayTime = float.NegativeInfinity;
        [NonSerialized] float _endTime = float.NegativeInfinity;

        /// <summary>Thời lượng một lần chạy (giây), chưa nhân DurationMultiplier.</summary>
        public abstract float Duration { get; }

        /// <summary>Feedback tạo chuyển động: biên độ bị giảm khi bật ReduceMotion.</summary>
        public virtual bool IsMotion => false;

        public bool IsPlaying => Owner != null && Owner.Now < _endTime;

        /// <summary>Tổng thời gian kể cả delay và lặp lại.</summary>
        public float TotalDuration(float multiplier)
        {
            float d = Duration;
            return (Timing.InitialDelay + d + Timing.Repeats * (d + Timing.RepeatDelay)) * multiplier;
        }

        public bool MatchesDirection(bool reverse)
        {
            if (Timing.PlayWhen == DirectionCondition.OnlyForward) return !reverse;
            if (Timing.PlayWhen == DirectionCondition.OnlyReverse) return reverse;
            return true;
        }

        public virtual string GetDisplayName()
        {
            var attr = (FeedbackMenuAttribute)Attribute.GetCustomAttribute(GetType(), typeof(FeedbackMenuAttribute));
            if (attr != null)
            {
                int slash = attr.Path.LastIndexOf('/');
                return slash >= 0 ? attr.Path.Substring(slash + 1) : attr.Path;
            }

            string name = GetType().Name;
            return name.EndsWith("Feedback") ? name.Substring(0, name.Length - "Feedback".Length) : name;
        }

        internal void Initialize(FeedbackPlayer owner)
        {
            Owner = owner;
            OnInitialize();
        }

        internal bool CanPlay(FeedbackContext ctx)
        {
            if (!Active) return false;
            if (!MatchesDirection(ctx.Reverse)) return false;
            if (Timing.UseIntensityRange && (ctx.Intensity < Timing.IntensityMin || ctx.Intensity > Timing.IntensityMax)) return false;
            if (Timing.Cooldown > 0f && Owner.Now - _lastPlayTime < Timing.Cooldown) return false;
            if (Chance < 100f && Random.value * 100f >= Chance) return false;
            return true;
        }

        internal void Play(FeedbackContext ctx)
        {
            _lastPlayTime = Owner.Now;
            _endTime = Owner.Now + TotalDuration(ctx.DurationMultiplier);
            ctx.Intensity = ComputeIntensity(ctx.Intensity);

            if (Timing.InitialDelay <= 0f && Timing.Repeats == 0)
            {
                OnPlay(ctx);
                return;
            }

            CancelRoutine();
            _routine = Owner.StartCoroutine(PlayRoutine(ctx));
        }

        internal void Stop()
        {
            CancelRoutine();
            _endTime = float.NegativeInfinity;
            OnStop();
        }

        internal void Skip(bool reverse)
        {
            CancelRoutine();
            _endTime = float.NegativeInfinity;
            OnSkip(reverse);
        }

        internal void Restore()
        {
            CancelRoutine();
            _endTime = float.NegativeInfinity;
            OnRestore();
        }

        IEnumerator PlayRoutine(FeedbackContext ctx)
        {
            if (Timing.InitialDelay > 0f) yield return Owner.Wait(Timing.InitialDelay * ctx.DurationMultiplier);

            for (int i = 0; i <= Timing.Repeats; i++)
            {
                OnPlay(ctx);
                if (i < Timing.Repeats) yield return Owner.Wait((Duration + Timing.RepeatDelay) * ctx.DurationMultiplier);
            }

            _routine = null;
        }

        float ComputeIntensity(float intensity)
        {
            float i = Timing.ConstantIntensity ? 1f : intensity;
            if (Timing.RandomizeIntensity) i *= Random.Range(Timing.RandomIntensity.x, Timing.RandomIntensity.y);
            if (IsMotion && FeedbackGlobal.ReduceMotion) i *= FeedbackGlobal.ReducedMotionMultiplier;
            return i;
        }

        void CancelRoutine()
        {
            if (_routine != null && Owner != null) Owner.StopCoroutine(_routine);
            _routine = null;
        }

        /// <summary>Gọi một lần khi player khởi tạo. Lưu giá trị gốc ở đây.</summary>
        protected virtual void OnInitialize() { }

        protected abstract void OnPlay(FeedbackContext ctx);

        /// <summary>Dừng giữa chừng (Stop, OnDisable).</summary>
        protected virtual void OnStop() { }

        /// <summary>Đưa về trạng thái cuối của chiều đang chạy (SkipToEnd). Mặc định = OnStop.</summary>
        protected virtual void OnSkip(bool reverse)
        {
            OnStop();
        }

        /// <summary>Khôi phục giá trị gốc lưu ở OnInitialize. Mặc định = OnStop.</summary>
        protected virtual void OnRestore()
        {
            OnStop();
        }
    }
}
