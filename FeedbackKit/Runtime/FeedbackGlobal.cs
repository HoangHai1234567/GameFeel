namespace FeedbackKit
{
    /// <summary>
    /// Công tắc toàn cục. Nối với màn Settings của game (Sound / Haptics / Reduce motion).
    /// </summary>
    public static class FeedbackGlobal
    {
        /// <summary>Tắt toàn bộ feedback (debug, so sánh có/không feedback).</summary>
        public static bool Enabled = true;

        public static bool SoundEnabled = true;

        public static bool HapticsEnabled = true;

        /// <summary>
        /// Accessibility: giảm chuyển động. Feedback chuyển động (IsMotion) nhân biên độ với
        /// <see cref="ReducedMotionMultiplier"/>; ShakeFeedback có thể bị bỏ qua hẳn.
        /// </summary>
        public static bool ReduceMotion = false;

        public static float ReducedMotionMultiplier = 0.35f;
    }
}
