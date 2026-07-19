using System.Collections.Generic;
using UnityEngine;

namespace MilkyWay
{
    /// <summary>
    /// Holds the spawned nebula/cluster specimens for the showcase — each one's
    /// root transform, its library index (for the label), and its framing
    /// radius. The gallery reads this to move between objects.
    /// </summary>
    public class NebulaController : MonoBehaviour
    {
        [System.Serializable]
        public struct Specimen
        {
            public Transform root;
            public int heroIndex;
            public float radius;
        }

        // Serialized so the builder's edit-time population survives into play.
        [SerializeField] List<Specimen> specimens = new();

        public int Count => specimens.Count;
        public NebulaLibrary.Hero Hero(int i) => NebulaLibrary.Heroes[specimens[i].heroIndex];
        public Transform Root(int i) => specimens[i].root;
        public float Radius(int i) => specimens[i].radius;

        public void Add(Transform root, int heroIndex, float radius)
        {
            specimens.Add(new Specimen { root = root, heroIndex = heroIndex, radius = radius });
        }
    }
}
