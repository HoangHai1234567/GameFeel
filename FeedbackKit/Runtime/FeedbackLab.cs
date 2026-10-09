using System;
using UnityEngine;

namespace FeedbackKit
{
    /// <summary>
    /// Bảng thử feedback ngay trên máy (IMGUI): liệt kê mọi FeedbackPlayer trong scene với nút
    /// Play / Reverse / Stop / Skip / Restore, kèm công tắc toàn cục và thanh intensity.
    /// Chỉ dùng cho bản dev — xoá hoặc tắt trong bản phát hành.
    /// </summary>
    [AddComponentMenu("FeedbackKit/Feedback Lab")]
    public class FeedbackLab : MonoBehaviour
    {
        public bool Visible = true;

        [Range(0f, 3f)] public float Intensity = 1f;

        [Tooltip("-1 = tự động (Ladder tự leo); ≥ 0 = gọi PlayStep(step).")]
        public int Step = -1;

        [Tooltip("Hệ số phóng to GUI cho điện thoại (0 = tự tính theo chiều cao màn hình).")]
        public float GuiScale;

        FeedbackPlayer[] _players = new FeedbackPlayer[0];
        Vector2 _scroll;

        void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            _players = FindObjectsByType<FeedbackPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Array.Sort(_players, (a, b) => string.CompareOrdinal(DisplayName(a), DisplayName(b)));
        }

        void OnGUI()
        {
            float scale = GuiScale > 0f ? GuiScale : Mathf.Max(1f, Screen.height / 900f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            if (GUI.Button(new Rect(8f, 8f, 150f, 36f), Visible ? "Hide Lab" : "Feedback Lab")) Visible = !Visible;

            if (Visible)
            {
                GUILayout.BeginArea(new Rect(8f, 50f, Mathf.Min(width - 16f, 560f), height - 58f), GUI.skin.box);

                GUILayout.BeginHorizontal();
                FeedbackGlobal.Enabled = GUILayout.Toggle(FeedbackGlobal.Enabled, " Feedback");
                FeedbackGlobal.SoundEnabled = GUILayout.Toggle(FeedbackGlobal.SoundEnabled, " Sound");
                FeedbackGlobal.HapticsEnabled = GUILayout.Toggle(FeedbackGlobal.HapticsEnabled, " Haptics");
                FeedbackGlobal.ReduceMotion = GUILayout.Toggle(FeedbackGlobal.ReduceMotion, " Reduce motion");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Intensity " + Intensity.ToString("0.00"), GUILayout.Width(110f));
                Intensity = GUILayout.HorizontalSlider(Intensity, 0f, 3f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Step < 0 ? "Step: auto" : "Step: " + Step, GUILayout.Width(110f));
                if (GUILayout.Button("-")) Step = Mathf.Max(-1, Step - 1);
                if (GUILayout.Button("+")) Step++;
                if (GUILayout.Button("Refresh")) Refresh();
                GUILayout.EndHorizontal();

                _scroll = GUILayout.BeginScrollView(_scroll);
                foreach (FeedbackPlayer p in _players)
                {
                    if (p == null) continue;
                    GUILayout.BeginHorizontal();
                    string state = p.isActiveAndEnabled ? (p.IsPlaying ? " ▶" : "") : " (inactive)";
                    GUILayout.Label(DisplayName(p) + state, GUILayout.Width(220f));
                    if (GUILayout.Button("Play"))
                    {
                        if (Step >= 0) p.PlayStep(Step, Intensity);
                        else p.Play(Intensity);
                    }
                    if (GUILayout.Button("Rev")) p.PlayReverse(Intensity);
                    if (GUILayout.Button("Stop")) p.Stop();
                    if (GUILayout.Button("Skip")) p.SkipToEnd();
                    if (GUILayout.Button("Reset")) p.RestoreInitialValues();
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();

                GUILayout.EndArea();
            }

            GUI.matrix = previous;
        }

        static string DisplayName(FeedbackPlayer p)
        {
            return string.IsNullOrEmpty(p.Title) ? p.name : p.name + " / " + p.Title;
        }
    }
}
