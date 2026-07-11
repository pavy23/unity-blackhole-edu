using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BlackHoleEffect
{
    /// <summary>
    /// First-person fall into the event horizon (F6): the camera free-falls
    /// toward the hole while the shadow swells to fill the sky — every last
    /// pixel goes black because, physically, every light path from where you
    /// are now ends inside. Then the camera is returned safely. Play mode only.
    /// </summary>
    public class FallInMode : MonoBehaviour
    {
        public Transform hole;
        public DesktopControls controls;
        public CinematicOrbit orbit;
        public float fallDuration = 14f;

        public bool IsFalling { get; private set; }

        Text caption;
        RectTransform captionPanel;

        public void Begin()
        {
            if (!Application.isPlaying || IsFalling || hole == null) return;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            IsFalling = true;
            if (controls != null) { controls.SetImmersive(true); controls.suspendCamera = true; }
            if (orbit != null) orbit.enabled = false;

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float rs = hole.lossyScale.x;
            Vector3 dir = (startPos - hole.position).normalized;
            float r0 = (startPos - hole.position).magnitude / rs;

            for (float t = 0f; t < fallDuration; t += Time.deltaTime)
            {
                float k = Mathf.Pow(t / fallDuration, 2.6f); // free-fall acceleration feel
                float r = Mathf.Lerp(r0, 0.35f, k);
                transform.position = hole.position + dir * (r * rs);
                transform.LookAt(hole.position);

                string rTxt = r.ToString("0.0");
                if (r > 8f) Caption(Loc.T(
                    "자유낙하 시작 —  r = " + rTxt + " Rs\n아직은 평범한 우주입니다.",
                    "Free fall begins —  r = " + rTxt + " Rs\nSpace still looks ordinary out here.",
                    "自由落下開始 —  r = " + rTxt + " Rs\nまだ、ふつうの宇宙です。",
                    "自由落体开始 —  r = " + rTxt + " Rs\n这里还是平常的宇宙。"));
                else if (r > 4f) Caption(Loc.T(
                    "r = " + rTxt + " Rs —  원반이 하늘을 뒤덮기 시작합니다.\n밖의 시간은 점점 빨라 보입니다.",
                    "r = " + rTxt + " Rs —  the disk begins to swallow the sky.\nTime outside appears to run faster and faster.",
                    "r = " + rTxt + " Rs —  円盤が空を覆いはじめます。\n外の時間はどんどん速く見えます。",
                    "r = " + rTxt + " Rs —  吸积盘开始遮蔽天空。\n外面的时间看起来越来越快。"));
                else if (r > 1.8f) Caption(Loc.T(
                    "r = " + rTxt + " Rs —  조석력이 몸을 잡아 늘입니다.\n그림자가 시야의 절반을 삼켰습니다.",
                    "r = " + rTxt + " Rs —  tidal forces stretch your body.\nThe shadow has swallowed half your view.",
                    "r = " + rTxt + " Rs —  潮汐力が体を引き伸ばします。\n影が視界の半分を呑み込みました。",
                    "r = " + rTxt + " Rs —  潮汐力开始拉伸你的身体。\n阴影已吞没一半视野。"));
                else if (r > 1f) Caption(Loc.T(
                    "r = " + rTxt + " Rs —  마지막 빛의 고리가 머리 위로 좁혀듭니다.",
                    "r = " + rTxt + " Rs —  the last ring of light closes in overhead.",
                    "r = " + rTxt + " Rs —  最後の光のリングが頭上で狭まっていきます。",
                    "r = " + rTxt + " Rs —  最后的光环在头顶收拢。"));
                else Caption(Loc.T(
                    "사건의 지평선 통과.\n바깥 우주로는 어떤 신호도 보낼 수 없습니다.",
                    "Event horizon crossed.\nNo signal can ever reach the outside universe again.",
                    "事象の地平面を通過。\n外の宇宙へは、もうどんな信号も送れません。",
                    "已越过事件视界。\n再也无法向外面的宇宙发出任何信号。"));
                yield return null;
            }

            Caption(Loc.T(
                "이 안에서는 모든 미래의 경로가 중심 특이점을 향합니다.\n— 여기까지가 물리학이 말할 수 있는 전부입니다.",
                "In here, every future path leads to the central singularity.\n— This is as far as physics can speak.",
                "この中では、あらゆる未来の経路が中心の特異点へ向かいます。\n— ここから先は、物理学が語れる限界です。",
                "在这里，所有未来的路径都通向中心奇点。\n— 物理学能讲述的，到此为止。"));
            yield return new WaitForSeconds(3.5f);

            transform.position = startPos;
            transform.rotation = startRot;
            HideCaption();
            if (controls != null) { controls.SetImmersive(false); controls.suspendCamera = false; }
            if (orbit != null) orbit.enabled = true; // the drift never stays off
            IsFalling = false;
        }

        void Caption(string text)
        {
            if (caption == null)
            {
                var canvas = BlackHoleUI.EnsureCanvas(GetComponent<Camera>());
                captionPanel = BlackHoleUI.MakePanel(canvas.transform, "FallIn Caption",
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(860f, 100f));
                caption = BlackHoleUI.MakeText(captionPanel, "Text", 21, BlackHoleUI.TextPrimary, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 84f));
                caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            captionPanel.gameObject.SetActive(true);
            caption.text = text;
        }

        void HideCaption()
        {
            if (captionPanel != null) captionPanel.gameObject.SetActive(false);
        }
    }
}
