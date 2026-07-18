using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// Builds the desktop control bar as one contained panel with the actions
    /// grouped by kind: each category is a labelled row (a gold caption on the
    /// left, its buttons to the right), so a visitor reads the controls as a
    /// short list of themes rather than a wall of buttons. Scene navigation is
    /// deliberately NOT here — it lives in <see cref="SceneNavigator"/>.
    /// </summary>
    public static class ExhibitBar
    {
        public struct Group
        {
            public System.Func<string> label;
            public (System.Func<string> text, UnityEngine.Events.UnityAction act)[] items;
        }

        const float LabelW = 78f, BtnW = 124f, BtnH = 33f, Gap = 6f, RowPitch = 39f;
        const float PadX = 18f, PadTop = 16f, PadBottom = 12f;

        public static (GameObject panel, List<(Text label, System.Func<string> text)> localized)
            Build(Transform canvas, Group[] groups)
        {
            var localized = new List<(Text, System.Func<string>)>();

            float maxRow = 0f;
            foreach (var g in groups)
            {
                float w = g.items.Length * BtnW + (g.items.Length - 1) * Gap;
                if (w > maxRow) maxRow = w;
            }
            float pw = PadX + LabelW + 8f + maxRow + PadX;
            float ph = PadTop + groups.Length * RowPitch + PadBottom;

            var panelRt = BlackHoleUI.MakePanel(canvas, "Control Bar",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f),
                new Vector2(pw, ph));
            var panel = panelRt.gameObject;
            var tl = new Vector2(0f, 1f);

            for (int i = 0; i < groups.Length; i++)
            {
                var g = groups[i];
                float rowY = -(PadTop + i * RowPitch);

                var cap = BlackHoleUI.MakeText(panelRt, "Cat " + i, 14, BlackHoleUI.TitleGold,
                    TextAnchor.MiddleLeft, tl, tl, new Vector2(PadX, rowY),
                    new Vector2(LabelW, BtnH), FontStyle.Bold);
                cap.text = g.label();
                localized.Add((cap, g.label));

                float x = PadX + LabelW + 8f;
                foreach (var (text, act) in g.items)
                {
                    var btn = BlackHoleUI.MakeButton(panelRt, "Btn " + text(), text(),
                        tl, tl, new Vector2(x, rowY), new Vector2(BtnW, BtnH), act);
                    var label = btn.GetComponentInChildren<Text>();
                    if (label != null) { label.fontSize = 14; localized.Add((label, text)); }
                    x += BtnW + Gap;
                }
            }

            return (panel, localized);
        }
    }
}
