using UnityEngine;

namespace BlackHoleEffect
{
    /// <summary>
    /// Brief accretion flare on the disk whenever matter is consumed —
    /// triggered by FallingMatter (MR throwables) or other demos.
    /// </summary>
    [RequireComponent(typeof(BlackHoleController))]
    public class MatterFlare : MonoBehaviour
    {
        BlackHoleController controller;
        float flare;
        float baseBrightness;
        bool touching;

        public void Trigger() => flare = 1f;

        void Awake() => controller = GetComponent<BlackHoleController>();

        void Update()
        {
            flare = Mathf.Max(0f, flare - flare * 0.6f * Time.deltaTime);
            if (controller == null) return;

            if (flare > 0.01f)
            {
                if (!touching) { baseBrightness = controller.diskBrightness; touching = true; }
                controller.diskBrightness = baseBrightness * (1f + 1.5f * flare);
                controller.Apply();
            }
            else if (touching)
            {
                controller.diskBrightness = baseBrightness;
                controller.Apply();
                touching = false;
            }
        }
    }
}
