using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    public sealed class CombatFeedback : MonoBehaviour
    {
        private static CombatFeedback instance;
        private readonly List<FloatingDamageNumber> activeNumbers = new List<FloatingDamageNumber>(16);
        private ObjectPool<FloatingDamageNumber> numberPool;
        private Camera targetCamera;
        private AudioSource audioSource;
        private float shakeRemaining;
        private bool shakeEnabled = true;

        public static CombatFeedback Create(Transform parent, Camera camera)
        {
            GameObject root = new GameObject("CombatFeedback");
            root.transform.SetParent(parent, false);
            CombatFeedback feedback = root.AddComponent<CombatFeedback>();
            feedback.targetCamera = camera;
            feedback.audioSource = root.AddComponent<AudioSource>();
            feedback.numberPool = new ObjectPool<FloatingDamageNumber>(feedback.CreateNumber,
                number => number.gameObject.SetActive(true), number => number.gameObject.SetActive(false),
                number => Destroy(number.gameObject), false, 16, 16);
            feedback.PrewarmNumbers();
            instance = feedback;
            return feedback;
        }

        public static void Play(string id)
        {
            if (instance == null || instance.audioSource == null)
            {
                return;
            }

            AudioClip clip = ProceduralAudio.Get(id);
            if (clip != null)
            {
                instance.audioSource.PlayOneShot(clip);
            }

            if (id == "hurt")
            {
                instance.shakeRemaining = 0.12f;
            }
        }

        public static void ShowDamage(Vector3 position, float damage)
        {
            if (instance == null || instance.numberPool == null || instance.activeNumbers.Count >= 16)
            {
                return;
            }

            FloatingDamageNumber number = instance.numberPool.Get();
            number.transform.position = position;
            number.SetText(Mathf.RoundToInt(damage).ToString());
            number.Begin(2f);
            instance.activeNumbers.Add(number);
        }

        public void SetShakeEnabled(bool enabled)
        {
            shakeEnabled = enabled;
            if (!shakeEnabled)
            {
                shakeRemaining = 0f;
            }
        }

        private FloatingDamageNumber CreateNumber()
        {
            GameObject label = new GameObject("DamageNumber");
            label.transform.SetParent(transform, false);
            TextMesh text = label.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 36;
            text.characterSize = 0.08f;
            text.color = Palette.LightGold;
            FloatingDamageNumber number = label.AddComponent<FloatingDamageNumber>();
            number.Initialize(text);
            label.SetActive(false);
            return number;
        }

        private void PrewarmNumbers()
        {
            FloatingDamageNumber[] values = new FloatingDamageNumber[16];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = numberPool.Get();
            }

            for (int i = 0; i < values.Length; i++)
            {
                numberPool.Release(values[i]);
            }
        }

        private void Update()
        {
            for (int i = activeNumbers.Count - 1; i >= 0; i--)
            {
                if (activeNumbers[i].Tick(Time.deltaTime, targetCamera))
                {
                    numberPool.Release(activeNumbers[i]);
                    activeNumbers.RemoveAt(i);
                }
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                return;
            }

            if (shakeEnabled && shakeRemaining > 0f)
            {
                shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.deltaTime);
                targetCamera.transform.position += Random.insideUnitSphere * 0.035f;
            }

        }

        private void OnDestroy()
        {
            numberPool?.Dispose();
            if (instance == this)
            {
                instance = null;
            }
        }

    }
}
