using System;
using UnityEngine;

namespace Manatea.RootlingForest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class TopDownAudioSpatializer : MonoBehaviour
    {
        private static AudioListener _activeAudioListener;
        public static AudioListener activeAudioListener
        {
            get
            {
                if (!_activeAudioListener
                    || !_activeAudioListener.isActiveAndEnabled)
                {
                    var audioListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    _activeAudioListener = Array.Find(audioListeners, audioListener => audioListener.enabled); // No need to check isActiveAndEnabled, FindObjectsOfType already filters out inactive objects.
                }

                return _activeAudioListener;
            }
        }


        private AudioSource m_AudioSource;

        private float m_Attenuation = 1;
        private float m_StereoPan = 0.5f;

        private bool m_SkipAudioManipulation;


        private void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();
        }

        private void LateUpdate()
        {
            if (!m_AudioSource.isPlaying)
            {
                m_SkipAudioManipulation = true;
                return;
            }
            m_SkipAudioManipulation = false;

            Camera camera = Camera.main;
            Vector3 screenPoint = camera.WorldToViewportPoint(transform.position);
            screenPoint = screenPoint * 2 - (Vector3)Vector2.one;

            m_Attenuation = MMath.InverseLerpClamped(1.5f, 0.5f, ((Vector2)screenPoint).magnitude);
            m_StereoPan = MMath.Clamp01(MMath.Pow(MMath.Abs(screenPoint.x / 1.25f), 1.0f) * MMath.Sign(screenPoint.x) * 0.5f + 0.5f);
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (m_SkipAudioManipulation)
                return;

            Debug.Assert(channels == 2, "Only Stereo sounds are supported right now");

            float leftVolume = m_Attenuation * MMath.InverseLerpClamped(0, 0.5f, m_StereoPan);
            float rightVolume = m_Attenuation * MMath.InverseLerpClamped(1, 0.5f, m_StereoPan);

            for (int i = 0; i < data.Length; i++)
            {
                if (i % 2 == 0)
                    data[i] *= rightVolume;
                else
                    data[i] *= leftVolume;
            }
        }
    }
}
