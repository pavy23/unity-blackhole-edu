using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace BlackHoleEffect
{
    /// <summary>
    /// MR gesture toy: open your left palm and a miniature black hole
    /// (Rs = 2 cm) materializes floating above it; make a fist and it winks
    /// out. Uses XR Hands joint data; degrades gracefully without tracking.
    /// </summary>
    public class PalmMiniBlackHole : MonoBehaviour
    {
        public Material holeMaterial;
        public XROrigin xrOrigin;
        [Tooltip("Average fingertip-to-palm distance (m) above which the hand counts as open.")]
        public float openThreshold = 0.085f;

        XRHandSubsystem hands;
        GameObject mini;
        float visibility; // smoothed 0..1

        static readonly XRHandJointID[] Tips =
        {
            XRHandJointID.IndexTip, XRHandJointID.MiddleTip,
            XRHandJointID.RingTip, XRHandJointID.LittleTip
        };

        void Update()
        {
            if (hands == null)
            {
                var list = new List<XRHandSubsystem>();
                SubsystemManager.GetSubsystems(list);
                if (list.Count > 0) hands = list[0];
                if (hands == null) return;
            }

            bool open = false;
            Pose palmPose = default;
            var hand = hands.leftHand;
            if (hand.isTracked && hand.GetJoint(XRHandJointID.Palm).TryGetPose(out palmPose))
            {
                float total = 0f;
                int counted = 0;
                foreach (var id in Tips)
                {
                    if (hand.GetJoint(id).TryGetPose(out var tipPose))
                    {
                        total += Vector3.Distance(tipPose.position, palmPose.position);
                        counted++;
                    }
                }
                open = counted >= 3 && (total / counted) > openThreshold;
            }

            visibility = Mathf.MoveTowards(visibility, open ? 1f : 0f, Time.deltaTime * 4f);

            if (visibility <= 0.01f)
            {
                if (mini != null) mini.SetActive(false);
                return;
            }

            EnsureMini();
            mini.SetActive(true);

            // Session space -> world space via the XR origin.
            Vector3 palmWorld = palmPose.position;
            Vector3 upWorld = palmPose.rotation * Vector3.up;
            if (xrOrigin != null)
            {
                palmWorld = xrOrigin.transform.TransformPoint(palmPose.position);
                upWorld = xrOrigin.transform.TransformDirection(palmPose.rotation * Vector3.up);
            }

            mini.transform.position = palmWorld + upWorld * 0.12f;
            mini.transform.rotation = Quaternion.Euler(0f, Time.time * 12f, -4f);
            mini.transform.localScale = Vector3.one * (0.02f * visibility); // Rs = 2 cm fully open
        }

        void EnsureMini()
        {
            if (mini != null) return;
            mini = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mini.name = "Palm Mini Black Hole";
            mini.hideFlags = HideFlags.HideAndDontSave;
            Destroy(mini.GetComponent<Collider>());
            if (holeMaterial != null)
                mini.GetComponent<MeshRenderer>().sharedMaterial = holeMaterial;
        }
    }
}
