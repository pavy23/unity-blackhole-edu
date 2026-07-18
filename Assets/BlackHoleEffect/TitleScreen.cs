using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// The exhibit's front door: pick a language, pick an experience. Three
    /// cards — solar system / Milky Way / black hole, home outward — each
    /// loading its showcase scene, over a live slowly-orbiting galaxy
    /// backdrop. Every scene offers F10 (desktop) or a menu button (MR) to
    /// come back here, so a walk-up visitor always has a clean entry point.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        Text title, subtitle, hint;
        readonly (Text label, System.Func<string> text)[] cardTexts = new (Text, System.Func<string>)[6];
        Button[] langButtons;
        int locVersion = -1;

        struct Card
        {
            public string scene, image;
            public System.Func<string> title, blurb;
        }

        static readonly Card[] Cards =
        {
            new Card { scene = "SolarSystemShowcase", image = "TitleCards/card_solar",
                title = () => Loc.T("태양계", "Solar System", "太陽系", "太阳系"),
                blurb = () => Loc.T("우리 동네 여덟 행성 —\n궤도와 진짜 크기의 이야기",
                                    "Our eight neighbours —\norbits, and the true scale",
                                    "私たちのご近所、8つの惑星 —\n軌道と本当の縮尺",
                                    "我们的八颗行星——\n轨道与真实比例") },
            new Card { scene = "MilkyWayShowcase", image = "TitleCards/card_galaxy",
                title = () => Loc.T("우리은하", "Milky Way", "天の川銀河", "银河系"),
                blurb = () => Loc.T("수천억 개의 별이 이루는\n나선 소용돌이를 여행",
                                    "A journey through the spiral\nof hundreds of billions of stars",
                                    "数千億の星がつくる\n渦巻きを旅する",
                                    "穿越数千亿颗恒星\n组成的旋涡") },
            new Card { scene = "BlackHoleShowcase", image = "TitleCards/card_blackhole",
                title = () => Loc.T("블랙홀", "Black Hole", "ブラックホール", "黑洞"),
                blurb = () => Loc.T("빛마저 갇히는 곳 —\n중력이 만드는 극한의 세계",
                                    "Where even light is trapped —\ngravity at its most extreme",
                                    "光さえ閉じ込められる —\n重力がつくる極限の世界",
                                    "连光都无法逃脱——\n引力的极限世界") },
        };

        static readonly (Loc.Lang lang, string label)[] Languages =
        {
            (Loc.Lang.Korean, "한국어"),
            (Loc.Lang.English, "English"),
            (Loc.Lang.Japanese, "日本語"),
            (Loc.Lang.Chinese, "中文"),
        };

        void Start() => Build();

        void Update()
        {
            if (locVersion != Loc.Version)
            {
                locVersion = Loc.Version;
                Refresh();
            }

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Load(0);
                if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Load(1);
                if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Load(2);
                if (kb.kKey.wasPressedThisFrame) Loc.Cycle();
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) Load(0);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) Load(1);
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) Load(2);
            if (Input.GetKeyDown(KeyCode.K)) Loc.Cycle();
#endif
        }

        static void Load(int i) =>
            UnityEngine.SceneManagement.SceneManager.LoadScene(Cards[i].scene);

        void Build()
        {
            var canvas = BlackHoleUI.EnsureCanvas(GetComponent<Camera>() ?? Camera.main);

            // The 68px title glyphs overflow their box downward; give it room
            // and seat the subtitle clear of that overflow (they used to
            // collide at ~-205 px).
            title = BlackHoleUI.MakeText(canvas.transform, "Title", 68, BlackHoleUI.TitleGold,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -108f), new Vector2(1400f, 100f), FontStyle.Bold);

            subtitle = BlackHoleUI.MakeText(canvas.transform, "Subtitle", 24, BlackHoleUI.TextSecondary,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -226f), new Vector2(1400f, 36f));

            // Three experience cards, home outward: solar system → galaxy → hole.
            const float cardW = 430f, cardH = 330f, gap = 45f;
            float x0 = -(cardW + gap);
            for (int i = 0; i < Cards.Length; i++)
            {
                int idx = i;
                var card = BlackHoleUI.MakePanel(canvas.transform, "Card " + Cards[i].scene,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x0 + i * (cardW + gap), -30f), new Vector2(cardW, cardH));

                // MakePanel leaves its Image non-raycastable (panels are usually
                // just backdrops); the whole card is a click target here, so turn
                // it back on — without this only the number keys select.
                var cardImg = card.GetComponent<Image>();
                cardImg.raycastTarget = true;
                Graphic hoverTarget = cardImg;

                // A screenshot of the scene fills the card. The rounded panel
                // image doubles as a Mask so the photo gets the card's rounded
                // corners; a scrim over it keeps the title/blurb readable on
                // bright regions (the galaxy core, the disk) and doubles as the
                // hover target — hovering thins it, brightening the scene.
                var photoSprite = Resources.Load<Sprite>(Cards[i].image);
                if (photoSprite != null)
                {
                    card.gameObject.AddComponent<Mask>().showMaskGraphic = true;

                    var photo = new GameObject("Photo", typeof(RectTransform), typeof(Image));
                    var pr = (RectTransform)photo.transform;
                    pr.SetParent(card, false);
                    pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0.5f);
                    // Cover the card without distortion: fit by height, let the
                    // wider 16:9 frame overflow and be clipped by the mask.
                    float aspect = photoSprite.rect.width / photoSprite.rect.height;
                    pr.sizeDelta = new Vector2(cardH * aspect, cardH);
                    var pimg = photo.GetComponent<Image>();
                    pimg.sprite = photoSprite;
                    pimg.preserveAspect = true;
                    pimg.raycastTarget = false;
                    pr.SetSiblingIndex(0);

                    // Darken only the top and bottom (where the title/blurb
                    // sit); the middle stays clear so the scene shows through.
                    var scrim = new GameObject("Scrim", typeof(RectTransform), typeof(Image));
                    var sr = (RectTransform)scrim.transform;
                    sr.SetParent(card, false);
                    sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one;
                    sr.offsetMin = sr.offsetMax = Vector2.zero;
                    var simg = scrim.GetComponent<Image>();
                    simg.sprite = EdgeGradient;
                    simg.type = Image.Type.Simple;
                    simg.color = Color.white;
                    simg.raycastTarget = false;
                    sr.SetSiblingIndex(1);
                    hoverTarget = simg;
                }

                var btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = hoverTarget;
                var colors = btn.colors;
                // Tint multiplies the scrim: alpha < 1 thins it on hover so the
                // scene brightens; pressed darkens briefly.
                colors.highlightedColor = new Color(1f, 1f, 1f, 0.45f);
                colors.pressedColor = new Color(1f, 1f, 1f, 0.85f);
                btn.colors = colors;
                btn.onClick.AddListener(() => Load(idx));

                var num = BlackHoleUI.MakeText(card, "Num", 22, BlackHoleUI.TextSecondary,
                    TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(24f, -20f), new Vector2(60f, 30f));
                num.text = (i + 1).ToString();

                // Title hugs the top, blurb hugs the bottom — the scene photo
                // shows through the clear middle instead of being covered.
                var cardTitle = BlackHoleUI.MakeText(card, "Title", 40, BlackHoleUI.TitleGold,
                    TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -44f), new Vector2(cardW - 40f, 52f), FontStyle.Bold);
                cardTexts[i * 2] = (cardTitle, Cards[i].title);

                var blurb = BlackHoleUI.MakeText(card, "Blurb", 20, BlackHoleUI.TextPrimary,
                    TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 20f), new Vector2(cardW - 50f, 84f));
                cardTexts[i * 2 + 1] = (blurb, Cards[i].blurb);
            }

            // Language row: the visit starts by picking a voice.
            langButtons = new Button[Languages.Length];
            const float langW = 190f, langH = 62f, langGap = 16f;
            float lx = -(Languages.Length - 1) * (langW + langGap) * 0.5f;
            for (int i = 0; i < Languages.Length; i++)
            {
                var lang = Languages[i].lang;
                langButtons[i] = BlackHoleUI.MakeButton(canvas.transform, "Lang " + Languages[i].label,
                    Languages[i].label, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(lx + i * (langW + langGap), 150f), new Vector2(langW, langH),
                    () => Loc.SetLanguage(lang));
            }

            hint = BlackHoleUI.MakeText(canvas.transform, "Hint", 17, BlackHoleUI.TextSecondary,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 96f), new Vector2(1200f, 26f));

            Refresh();
        }

        void Refresh()
        {
            if (title == null) return;
            title.text = Loc.T("우주 전시관", "The Cosmos Exhibit", "宇宙展示館", "宇宙展览馆");
            subtitle.text = Loc.T("체험할 우주를 선택하세요",
                                  "Choose the cosmos you want to explore",
                                  "体験する宇宙を選んでください",
                                  "请选择要探索的宇宙");
            hint.text = Loc.T("카드를 클릭하거나 1 · 2 · 3 키로 선택   ·   K 언어",
                              "Click a card or press 1 · 2 · 3   ·   K language",
                              "カードをクリック、または 1 · 2 · 3 キー   ·   K 言語",
                              "点击卡片或按 1 · 2 · 3 键   ·   K 语言");
            foreach (var (label, text) in cardTexts)
                if (label != null) label.text = text();

            // The active language holds a gold edge so the choice reads back.
            for (int i = 0; i < langButtons.Length; i++)
            {
                if (langButtons[i] == null) continue;
                var img = langButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = Languages[i].lang == Loc.Language
                        ? new Color(0.42f, 0.36f, 0.18f, 0.97f)
                        : BlackHoleUI.PanelBg;
                var label = langButtons[i].GetComponentInChildren<Text>();
                if (label != null)
                    label.color = Languages[i].lang == Loc.Language
                        ? BlackHoleUI.TitleGold : BlackHoleUI.TextPrimary;
            }
        }

        static Sprite edgeGradient;
        /// <summary>A vertical scrim: dark at the top and bottom, clear through
        /// the middle. Darkens the bands the title and blurb sit on without
        /// veiling the scene photo in the centre.</summary>
        static Sprite EdgeGradient
        {
            get
            {
                if (edgeGradient != null) return edgeGradient;
                const int h = 128;
                var tex = new Texture2D(4, h, TextureFormat.RGBA32, false)
                    { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < h; y++)
                {
                    float v = y / (h - 1f);            // 0 bottom .. 1 top
                    float topBand = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1f, v));
                    float botBand = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.30f, 0f, v));
                    float a = Mathf.Max(topBand, botBand) * 0.72f;
                    var c = new Color(0.02f, 0.03f, 0.06f, a);
                    for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
                }
                tex.Apply();
                edgeGradient = Sprite.Create(tex, new Rect(0, 0, 4, h), new Vector2(0.5f, 0.5f));
                return edgeGradient;
            }
        }
    }
}
