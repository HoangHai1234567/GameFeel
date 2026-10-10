using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FeedbackKit.EditorTools
{
    /// <summary>
    /// Inspector cho FeedbackPlayer: thêm feedback theo menu, sắp xếp, nhân bản, xoá,
    /// và nút thử (Play / Reverse / Stop / Skip / Restore) khi đang ở Play mode.
    /// </summary>
    [CustomEditor(typeof(FeedbackPlayer))]
    public class FeedbackPlayerEditor : UnityEditor.Editor
    {
        static List<KeyValuePair<string, Type>> _menuTypes;
        static float _testIntensity = 1f;
        static int _testStep = -1;

        SerializedProperty _list;
        bool _showEvents;

        static readonly string[] ExcludedFromHeader =
        {
            "m_Script",
            nameof(FeedbackPlayer.Feedbacks),
            nameof(FeedbackPlayer.OnPlayStarted),
            nameof(FeedbackPlayer.OnCompleted),
            nameof(FeedbackPlayer.OnStopped)
        };

        void OnEnable()
        {
            _list = serializedObject.FindProperty(nameof(FeedbackPlayer.Feedbacks));
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, ExcludedFromHeader);

            _showEvents = EditorGUILayout.Foldout(_showEvents, "Events", true);
            if (_showEvents)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(FeedbackPlayer.OnPlayStarted)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(FeedbackPlayer.OnCompleted)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(FeedbackPlayer.OnStopped)));
            }

            EditorGUILayout.Space(6f);
            DrawFeedbackList();

            EditorGUILayout.Space(2f);
            if (GUILayout.Button("+ Add Feedback", GUILayout.Height(24f))) ShowAddMenu();

            var player = (FeedbackPlayer)target;
            EditorGUILayout.LabelField("Tổng thời lượng ước tính", player.ComputeTotalDuration().ToString("0.00") + " s", EditorStyles.miniLabel);

            serializedObject.ApplyModifiedProperties();

            DrawRuntimeControls(player);
            DrawValuesClipboard(player);
        }

        // Play mode: chép số đã chỉnh. Ngoài Play: dán vào player đang chọn (thường là prefab), giữ tham chiếu.
        void DrawValuesClipboard(FeedbackPlayer player)
        {
            EditorGUILayout.Space(4f);
            if (Application.isPlaying)
            {
                if (GUILayout.Button(new GUIContent("Copy giá trị (để dán vào prefab sau khi thoát Play)",
                        "Chép số, ease, âm lượng, bật/tắt… của mọi bước. Không chép Target, Clips, Unity Event.")))
                {
                    FeedbackValuesClipboard.Snapshot snap = FeedbackValuesClipboard.Capture(player);
                    FeedbackValuesClipboard.Store(snap);
                    Debug.Log($"[FeedbackKit] Đã copy {snap.Entries.Count} giá trị từ '{Describe(snap)}'. Thoát Play, chọn player trong prefab rồi bấm Paste.");
                }
                return;
            }

            FeedbackValuesClipboard.Snapshot stored = FeedbackValuesClipboard.Stored;
            if (stored == null) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Clipboard: '{Describe(stored)}' — {stored.FeedbackTypes.Count} bước, copy lúc {stored.CapturedAt}",
                    EditorStyles.wordWrappedMiniLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Paste giá trị (giữ tham chiếu)")) Paste(player, stored);
                    if (GUILayout.Button("Xoá", GUILayout.Width(48f))) FeedbackValuesClipboard.Clear();
                }
            }
        }

        void Paste(FeedbackPlayer player, FeedbackValuesClipboard.Snapshot stored)
        {
            if (!string.IsNullOrEmpty(stored.Title) && stored.Title != player.Title &&
                !EditorUtility.DisplayDialog("FeedbackKit",
                    $"Clipboard copy từ '{stored.Title}', player đang chọn là '{player.Title}'. Vẫn dán?", "Dán", "Huỷ"))
            {
                return;
            }

            Undo.RecordObject(player, "Paste FeedbackKit values");
            FeedbackValuesClipboard.ApplyResult result = FeedbackValuesClipboard.Apply(stored, player);
            serializedObject.Update();
            if (result.Ok)
            {
                EditorUtility.SetDirty(player);
                Debug.Log("[FeedbackKit] " + result.Message + " Nhớ Save prefab.");
            }
            else
            {
                EditorUtility.DisplayDialog("FeedbackKit", result.Message, "OK");
            }
        }

        static string Describe(FeedbackValuesClipboard.Snapshot snap) =>
            string.IsNullOrEmpty(snap.Title) ? snap.SourceName : snap.Title;

        void DrawFeedbackList()
        {
            EditorGUILayout.LabelField("Feedbacks (" + _list.arraySize + ")", EditorStyles.boldLabel);

            int removeAt = -1;
            int moveFrom = -1;
            int moveTo = -1;
            int duplicateAt = -1;

            for (int i = 0; i < _list.arraySize; i++)
            {
                SerializedProperty element = _list.GetArrayElementAtIndex(i);
                var feedback = element.managedReferenceValue as Feedback;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (feedback == null)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(i + ". (thiếu type — class đã bị đổi tên/xoá?)", EditorStyles.miniBoldLabel);
                            if (GUILayout.Button("X", GUILayout.Width(24f))) removeAt = i;
                        }
                        continue;
                    }

                    SerializedProperty active = element.FindPropertyRelative(nameof(Feedback.Active));
                    SerializedProperty label = element.FindPropertyRelative(nameof(Feedback.Label));

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        active.boolValue = EditorGUILayout.Toggle(active.boolValue, GUILayout.Width(16f));

                        string title = i + ". " + feedback.GetDisplayName();
                        if (!string.IsNullOrEmpty(label.stringValue)) title += "  —  " + label.stringValue;
                        if (feedback is PauseFeedback pause) title += "  (" + pause.Seconds.ToString("0.##") + " s)";
                        if (Application.isPlaying && feedback.IsPlaying) title += "  ▶";

                        element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, title, true);

                        using (new EditorGUI.DisabledScope(i == 0))
                        {
                            if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(24f)))
                            {
                                moveFrom = i;
                                moveTo = i - 1;
                            }
                        }
                        using (new EditorGUI.DisabledScope(i == _list.arraySize - 1))
                        {
                            if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(24f)))
                            {
                                moveFrom = i;
                                moveTo = i + 1;
                            }
                        }
                        if (GUILayout.Button("Dup", EditorStyles.miniButtonMid, GUILayout.Width(36f))) duplicateAt = i;
                        if (GUILayout.Button("X", EditorStyles.miniButtonRight, GUILayout.Width(24f))) removeAt = i;
                    }

                    if (element.isExpanded) DrawFeedbackFields(element, feedback);
                }
            }

            if (removeAt >= 0) _list.DeleteArrayElementAtIndex(removeAt);
            else if (moveFrom >= 0) _list.MoveArrayElement(moveFrom, moveTo);
            else if (duplicateAt >= 0) Duplicate(duplicateAt);
        }

        static void DrawFeedbackFields(SerializedProperty element, Feedback feedback)
        {
            EditorGUI.indentLevel++;
            SerializedProperty it = element.Copy();
            SerializedProperty end = element.GetEndProperty();
            bool enterChildren = true;
            while (it.NextVisible(enterChildren) && !SerializedProperty.EqualContents(it, end))
            {
                enterChildren = false;
                if (it.name == nameof(Feedback.Active)) continue;
                if (feedback is PauseFeedback && (it.name == nameof(Feedback.Timing) || it.name == nameof(Feedback.Chance))) continue;
                EditorGUILayout.PropertyField(it, true);
            }
            EditorGUI.indentLevel--;
        }

        void Duplicate(int index)
        {
            object source = _list.GetArrayElementAtIndex(index).managedReferenceValue;
            if (source == null) return;

            object copy = Activator.CreateInstance(source.GetType());
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(source), copy);

            _list.InsertArrayElementAtIndex(index + 1);
            SerializedProperty element = _list.GetArrayElementAtIndex(index + 1);
            element.managedReferenceValue = copy;
            element.isExpanded = true;
        }

        void ShowAddMenu()
        {
            var menu = new GenericMenu();
            foreach (KeyValuePair<string, Type> entry in GetMenuTypes())
            {
                Type type = entry.Value;
                menu.AddItem(new GUIContent(entry.Key), false, () =>
                {
                    serializedObject.Update();
                    int index = _list.arraySize;
                    _list.InsertArrayElementAtIndex(index);
                    SerializedProperty element = _list.GetArrayElementAtIndex(index);
                    element.managedReferenceValue = Activator.CreateInstance(type);
                    element.isExpanded = true;
                    serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }

        static List<KeyValuePair<string, Type>> GetMenuTypes()
        {
            if (_menuTypes != null) return _menuTypes;

            _menuTypes = TypeCache.GetTypesDerivedFrom<Feedback>()
                .Where(t => !t.IsAbstract && !t.IsGenericType && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t =>
                {
                    var attr = (FeedbackMenuAttribute)Attribute.GetCustomAttribute(t, typeof(FeedbackMenuAttribute));
                    return new KeyValuePair<string, Type>(attr != null ? attr.Path : "Other/" + t.Name, t);
                })
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .ToList();
            return _menuTypes;
        }

        static void DrawRuntimeControls(FeedbackPlayer player)
        {
            EditorGUILayout.Space(6f);
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Vào Play mode để thử feedback ngay trong Inspector.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField("Test" + (player.IsPlaying ? "  ▶ playing" : ""), EditorStyles.boldLabel);
            _testIntensity = EditorGUILayout.Slider("Intensity", _testIntensity, 0f, 3f);
            _testStep = EditorGUILayout.IntField(new GUIContent("Step (-1 = auto)"), _testStep);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play"))
                {
                    if (_testStep >= 0) player.PlayStep(_testStep, _testIntensity);
                    else player.Play(_testIntensity);
                }
                if (GUILayout.Button("Reverse")) player.PlayReverse(_testIntensity);
                if (GUILayout.Button("Stop")) player.Stop();
                if (GUILayout.Button("Skip")) player.SkipToEnd();
                if (GUILayout.Button("Restore")) player.RestoreInitialValues();
            }
        }
    }
}
