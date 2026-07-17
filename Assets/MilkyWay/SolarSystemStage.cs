using UnityEngine;

namespace MilkyWay
{
    /// <summary>
    /// Bootstraps the solar-system exhibit: spawns the rig at the origin at
    /// exhibit scale on play (the rig's meshes and materials are runtime-
    /// generated, so the scene file stays lean), with a gentle ambient
    /// motion. Tours and experiences find the rig here.
    /// </summary>
    public class SolarSystemStage : MonoBehaviour
    {
        [Tooltip("World scale applied to the rig (rig-local units are kpc-ish).")]
        public float rigScale = 1000f;
        [Tooltip("Ambient orbit/spin speed while nobody is touring.")]
        public float ambientMotionScale = 0.25f;

        public SolarSystemRig Rig { get; private set; }

        void Awake()
        {
            if (!Application.isPlaying) return;
            Rig = SolarSystemRig.Spawn(Vector3.zero);
            Rig.gameObject.name = "Solar System (stage)";
            Rig.transform.localScale = Vector3.one * rigScale;
            Rig.motionScale = ambientMotionScale;
        }
    }
}
