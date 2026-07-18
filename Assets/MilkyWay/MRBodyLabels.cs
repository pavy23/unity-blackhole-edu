using System.Collections.Generic;
using UnityEngine;
using BlackHoleEffect; // Loc, BlackHoleUI

namespace MilkyWay
{
    /// <summary>
    /// World-space name tags for the MR exhibits: a billboard TextMesh floating
    /// above each tracked body, with an optional leader line down to it. The
    /// labels live in scene space (no parent) so a planet's own scale — which
    /// the realism blend animates over three orders of magnitude — never leaks
    /// into the text size. Text size instead follows <see cref="scaleRef"/>
    /// (the exhibit root), so two-hand scaling the miniature scales its tags.
    /// </summary>
    public class MRBodyLabels : MonoBehaviour
    {
        class Entry
        {
            public Transform target;
            public System.Func<string> text;
            public TextMesh label;
            public LineRenderer leader;
            public float offsetMul;   // times the target's world radius (lossyScale.x)
            public float offsetAdd;   // plus this many metres
            public int locVersion = -1;
        }

        /// <summary>Exhibit root whose lossyScale drives label size (two-hand
        /// scaling). Null = fixed size.</summary>
        public Transform scaleRef;

        /// <summary>World text height factor at scaleRef's spawn scale.</summary>
        public float baseCharSize = 0.012f;

        public Color textColor = new Color(0.92f, 0.95f, 1f, 0.95f);
        public Color lineColor = new Color(0.7f, 0.8f, 1f, 0.4f);

        readonly List<Entry> entries = new();
        Material lineMat;
        float refScale = 1f;
        bool visible = true;

        /// <summary>Call once after the exhibit is built, before Add().</summary>
        public void Init(Transform exhibitRoot)
        {
            scaleRef = exhibitRoot;
            refScale = exhibitRoot != null ? Mathf.Max(exhibitRoot.lossyScale.x, 1e-6f) : 1f;
        }

        public void Add(Transform target, System.Func<string> text,
            float offsetMul = 2.2f, float offsetAdd = 0.025f, bool leaderLine = false)
        {
            if (target == null) return;
            var go = new GameObject("Label — " + target.name);
            go.transform.SetParent(transform, false);

            var label = go.AddComponent<TextMesh>();
            label.font = BlackHoleUI.UIFont;
            label.fontSize = 64;
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            label.color = textColor;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = label.font.material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            LineRenderer leader = null;
            if (leaderLine)
            {
                if (lineMat == null)
                    lineMat = new Material(Shader.Find("Sprites/Default"));
                var lineGo = new GameObject("Leader");
                lineGo.transform.SetParent(go.transform, false);
                leader = lineGo.AddComponent<LineRenderer>();
                leader.positionCount = 2;
                leader.startWidth = leader.endWidth = 1f;
                leader.widthMultiplier = 0.002f;
                leader.material = lineMat;
                leader.startColor = leader.endColor = lineColor;
                leader.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                leader.useWorldSpace = true;
            }

            entries.Add(new Entry
            {
                target = target, text = text, label = label, leader = leader,
                offsetMul = offsetMul, offsetAdd = offsetAdd
            });
        }

        public void SetVisible(bool on)
        {
            visible = on;
        }

        public bool Visible => visible;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            float s = scaleRef != null
                ? Mathf.Max(scaleRef.lossyScale.x, 1e-6f) / refScale : 1f;
            float charSize = baseCharSize * s;

            foreach (var e in entries)
            {
                if (e.label == null) continue;
                bool on = visible && e.target != null
                    && e.target.gameObject.activeInHierarchy;
                if (e.label.gameObject.activeSelf != on) e.label.gameObject.SetActive(on);
                if (!on) continue;

                if (e.locVersion != Loc.Version)
                {
                    e.locVersion = Loc.Version;
                    e.label.text = e.text();
                }

                float radius = e.target.lossyScale.x;
                Vector3 anchor = e.target.position;
                Vector3 pos = anchor + Vector3.up * (radius * e.offsetMul + e.offsetAdd * s);
                e.label.characterSize = charSize;
                e.label.transform.position = pos;
                // Yaw-only billboard: pitching with the head reads as HUD.
                Vector3 fwd = pos - cam.transform.position;
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 1e-6f)
                    e.label.transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);

                if (e.leader != null)
                {
                    e.leader.widthMultiplier = 0.0022f * s;
                    e.leader.SetPosition(0, anchor + Vector3.up * radius * 1.05f);
                    e.leader.SetPosition(1, pos);
                }
            }
        }

        void OnDestroy()
        {
            if (lineMat != null) Destroy(lineMat);
        }
    }
}
