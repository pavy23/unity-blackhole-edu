using UnityEngine;

namespace BlackHoleEffect
{
    /// <summary>
    /// Plays the pre-generated Korean narration clips (Resources/Narration/*)
    /// for the intro sequence and the guided tour, ducking the procedural
    /// soundscape while the voice is speaking. Created on demand — nothing
    /// needs to be wired in the scene.
    /// </summary>
    public class NarrationManager : MonoBehaviour
    {
        static NarrationManager instance;

        public static NarrationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // Plain runtime object — dies with play mode, no leaks.
                    var go = new GameObject("Narration");
                    instance = go.AddComponent<NarrationManager>();
                }
                return instance;
            }
        }

        AudioSource source;
        BlackHoleAudio ambience;
        float ambienceBaseVolume;

        public bool IsSpeaking => source != null && source.isPlaying;

        void Awake()
        {
            instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0.95f;
        }

        /// <summary>Plays Resources/Narration/{lang-folder}{key} and returns
        /// the clip length in seconds (0 when the clip is missing, so callers
        /// can fall back to their fixed timings).</summary>
        public float Play(string key)
        {
            var clip = Resources.Load<AudioClip>("Narration/" + Loc.NarrationFolder + key);
            if (clip == null) return 0f;
            source.Stop();
            source.clip = clip;
            source.Play();
            return clip.length;
        }

        public void Stop()
        {
            if (source != null) source.Stop();
        }

        void Update()
        {
            if (ambience == null)
            {
                ambience = FindAnyObjectByType<BlackHoleAudio>();
                if (ambience != null) ambienceBaseVolume = ambience.volume;
                if (ambience == null) return;
            }
            float target = IsSpeaking ? ambienceBaseVolume * 0.35f : ambienceBaseVolume;
            ambience.volume = Mathf.MoveTowards(ambience.volume, target, Time.deltaTime * 0.4f);
        }
    }
}
