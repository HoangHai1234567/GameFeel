using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace FeedbackKit.EditorTools
{
    /// <summary>
    /// Giữ lại số đã chỉnh trong Play mode: <see cref="Capture"/> chép mọi GIÁ TRỊ của một FeedbackPlayer (số, bool, enum,
    /// chuỗi, vector, màu, curve, mảng số) — bỏ qua tham chiếu object (Target, Clips…) và Unity Event; <see cref="Apply"/>
    /// dán theo THỨ TỰ bước vào player khác (thường là prefab sau khi thoát Play), giữ nguyên tham chiếu sẵn có.
    /// Số bước hoặc loại bước lệch nhau ⇒ từ chối, không dán gì. Clipboard nằm trong SessionState nên sống qua lần thoát Play.
    /// </summary>
    public static class FeedbackValuesClipboard
    {
        const string SessionKey = "FeedbackKit.ValuesClipboard";
        const string FeedbacksPath = nameof(FeedbackPlayer.Feedbacks);

        static readonly string[] SkippedRoots =
        {
            "m_Script",
            nameof(FeedbackPlayer.OnPlayStarted),
            nameof(FeedbackPlayer.OnCompleted),
            nameof(FeedbackPlayer.OnStopped)
        };

        [Serializable]
        public sealed class Entry
        {
            public string Path;
            public SerializedPropertyType Type;
            public string Value;
        }

        [Serializable]
        public sealed class Snapshot
        {
            public string Title;
            public string SourceName;
            public string CapturedAt;
            public List<string> FeedbackTypes = new List<string>();
            public List<Entry> Entries = new List<Entry>();
        }

        [Serializable]
        sealed class CurveBox
        {
            public AnimationCurve Curve;
        }

        public struct ApplyResult
        {
            public bool Ok;
            public int Applied;
            public int Skipped;
            public string Message;
        }

        public static bool HasStored => !string.IsNullOrEmpty(SessionState.GetString(SessionKey, ""));

        public static Snapshot Stored
        {
            get
            {
                string json = SessionState.GetString(SessionKey, "");
                return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<Snapshot>(json);
            }
        }

        public static void Store(Snapshot snapshot) =>
            SessionState.SetString(SessionKey, snapshot != null ? JsonUtility.ToJson(snapshot) : "");

        public static void Clear() => SessionState.EraseString(SessionKey);

        public static Snapshot Capture(FeedbackPlayer player)
        {
            var so = new SerializedObject(player);
            var snapshot = new Snapshot
            {
                Title = player.Title,
                SourceName = player.name,
                CapturedAt = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
            };

            SerializedProperty list = so.FindProperty(FeedbacksPath);
            for (int i = 0; i < list.arraySize; i++)
            {
                snapshot.FeedbackTypes.Add(list.GetArrayElementAtIndex(i).managedReferenceFullTypename);
            }

            SerializedProperty it = so.GetIterator();
            bool enterChildren = true;
            while (it.NextVisible(enterChildren))
            {
                enterChildren = true;
                if (IsSkippedRoot(it.propertyPath))
                {
                    enterChildren = false;
                    continue;
                }
                if (IsReferenceArraySize(so, it)) continue;
                if (TryRead(it, out string value))
                {
                    snapshot.Entries.Add(new Entry { Path = it.propertyPath, Type = it.propertyType, Value = value });
                    enterChildren = false; // Vector3/Color… đã chép nguyên khối, khỏi chép lại từng x/y/z
                }
            }
            return snapshot;
        }

        /// <summary>Kiểm khớp cấu trúc (số bước + loại từng bước) rồi mới dán; lệch ⇒ không đụng gì.</summary>
        public static ApplyResult Apply(Snapshot snapshot, FeedbackPlayer player)
        {
            if (snapshot == null) return new ApplyResult { Message = "Clipboard trống." };

            var so = new SerializedObject(player);
            SerializedProperty list = so.FindProperty(FeedbacksPath);
            if (list.arraySize != snapshot.FeedbackTypes.Count)
            {
                return new ApplyResult
                {
                    Message = $"Số bước khác nhau: clipboard {snapshot.FeedbackTypes.Count}, đích {list.arraySize}. Không dán."
                };
            }
            for (int i = 0; i < list.arraySize; i++)
            {
                string type = list.GetArrayElementAtIndex(i).managedReferenceFullTypename;
                if (type != snapshot.FeedbackTypes[i])
                {
                    return new ApplyResult
                    {
                        Message = $"Bước {i} khác loại: clipboard {ShortType(snapshot.FeedbackTypes[i])}, đích {ShortType(type)}. Không dán."
                    };
                }
            }

            int applied = 0;
            int skipped = 0;
            foreach (Entry entry in snapshot.Entries)
            {
                SerializedProperty prop = so.FindProperty(entry.Path);
                if (prop == null || prop.propertyType != entry.Type || !TryWrite(prop, entry.Value)) skipped++;
                else applied++;
                // Mảng số đổi độ dài: phải áp ngay để các phần tử phía sau tồn tại khi FindProperty.
                if (entry.Path.EndsWith(".Array.size", StringComparison.Ordinal)) so.ApplyModifiedProperties();
            }
            so.ApplyModifiedProperties();
            return new ApplyResult
            {
                Ok = true,
                Applied = applied,
                Skipped = skipped,
                Message = $"Đã dán {applied} giá trị" + (skipped > 0 ? $", bỏ qua {skipped}" : "") + "."
            };
        }

        static bool IsSkippedRoot(string path)
        {
            foreach (string root in SkippedRoots)
            {
                if (path == root) return true;
            }
            return false;
        }

        // Độ dài mảng tham chiếu (Clips…) không chép: phần tử là tham chiếu nên chép độ dài chỉ đẻ ra ô trống.
        static bool IsReferenceArraySize(SerializedObject so, SerializedProperty prop)
        {
            if (!prop.propertyPath.EndsWith(".Array.size", StringComparison.Ordinal)) return false;
            string arrayPath = prop.propertyPath.Substring(0, prop.propertyPath.Length - ".Array.size".Length);
            SerializedProperty array = so.FindProperty(arrayPath);
            return array != null && array.isArray && array.arrayElementType.StartsWith("PPtr<", StringComparison.Ordinal);
        }

        static bool TryRead(SerializedProperty p, out string value)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.ArraySize:
                    value = p.longValue.ToString(inv); return true;
                case SerializedPropertyType.Boolean: value = p.boolValue ? "1" : "0"; return true;
                case SerializedPropertyType.Float: value = p.doubleValue.ToString("R", inv); return true;
                case SerializedPropertyType.String: value = p.stringValue; return true;
                case SerializedPropertyType.Enum: value = p.intValue.ToString(inv); return true;
                case SerializedPropertyType.Color: value = JsonUtility.ToJson(p.colorValue); return true;
                case SerializedPropertyType.Vector2: value = JsonUtility.ToJson(p.vector2Value); return true;
                case SerializedPropertyType.Vector3: value = JsonUtility.ToJson(p.vector3Value); return true;
                case SerializedPropertyType.Vector4: value = JsonUtility.ToJson(p.vector4Value); return true;
                case SerializedPropertyType.Vector2Int: value = JsonUtility.ToJson(p.vector2IntValue); return true;
                case SerializedPropertyType.Vector3Int: value = JsonUtility.ToJson(p.vector3IntValue); return true;
                case SerializedPropertyType.Quaternion: value = JsonUtility.ToJson(p.quaternionValue); return true;
                case SerializedPropertyType.Rect: value = JsonUtility.ToJson(p.rectValue); return true;
                case SerializedPropertyType.AnimationCurve:
                    value = JsonUtility.ToJson(new CurveBox { Curve = p.animationCurveValue }); return true;
                default:
                    value = null; return false; // tham chiếu object, managed reference, gradient…
            }
        }

        static bool TryWrite(SerializedProperty p, string value)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.ArraySize:
                    if (!long.TryParse(value, NumberStyles.Integer, inv, out long l)) return false;
                    p.longValue = l; return true;
                case SerializedPropertyType.Boolean: p.boolValue = value == "1"; return true;
                case SerializedPropertyType.Float:
                    if (!double.TryParse(value, NumberStyles.Float, inv, out double d)) return false;
                    p.doubleValue = d; return true;
                case SerializedPropertyType.String: p.stringValue = value; return true;
                case SerializedPropertyType.Enum:
                    if (!int.TryParse(value, NumberStyles.Integer, inv, out int e)) return false;
                    p.intValue = e; return true;
                case SerializedPropertyType.Color: p.colorValue = JsonUtility.FromJson<Color>(value); return true;
                case SerializedPropertyType.Vector2: p.vector2Value = JsonUtility.FromJson<Vector2>(value); return true;
                case SerializedPropertyType.Vector3: p.vector3Value = JsonUtility.FromJson<Vector3>(value); return true;
                case SerializedPropertyType.Vector4: p.vector4Value = JsonUtility.FromJson<Vector4>(value); return true;
                case SerializedPropertyType.Vector2Int: p.vector2IntValue = JsonUtility.FromJson<Vector2Int>(value); return true;
                case SerializedPropertyType.Vector3Int: p.vector3IntValue = JsonUtility.FromJson<Vector3Int>(value); return true;
                case SerializedPropertyType.Quaternion: p.quaternionValue = JsonUtility.FromJson<Quaternion>(value); return true;
                case SerializedPropertyType.Rect: p.rectValue = JsonUtility.FromJson<Rect>(value); return true;
                case SerializedPropertyType.AnimationCurve:
                    p.animationCurveValue = JsonUtility.FromJson<CurveBox>(value).Curve; return true;
                default:
                    return false;
            }
        }

        static string ShortType(string fullTypename)
        {
            int dot = fullTypename.LastIndexOf('.');
            return dot >= 0 ? fullTypename.Substring(dot + 1) : fullTypename;
        }
    }
}
