using UnityEngine;

namespace YourNamespace
{
    [ExecuteAlways]
    public class VFX_FireController : MonoBehaviour
    {
        [Header("Réglages VFX Feu")]
        [SerializeField] private Color fireColor = Color.red;
        [SerializeField, Range(0f, 2f)] private float fireIntensity = 1f;
        [SerializeField] private Vector3 fireWindDirection = Vector3.zero;

        private ParticleSystem[] fireParticleSystems;
        private float[] defaultFireRateValues;        // Valeurs par défaut du spawn rate (Emission)
        private float[] defaultFireStartSizeValues;     // Valeurs par défaut de la taille (Main > startSize)
        private Light fireLight; // Adding reference to Light component for intensity control

        private void Awake()
        {
            FindFireParticles();
            ApplyFireSettings();
        }

        private void OnValidate()
        {
            // En mode éditeur, il se peut qu'Awake() ne soit pas appelé,
            // donc on s'assure que les tableaux sont initialisés.
            if (fireParticleSystems == null || fireParticleSystems.Length == 0 ||
                defaultFireRateValues == null || defaultFireStartSizeValues == null ||
                defaultFireRateValues.Length != fireParticleSystems.Length ||
                defaultFireStartSizeValues.Length != fireParticleSystems.Length)
            {
                FindFireParticles();
            }
            ApplyFireSettings();
        }

        /// <summary>
        /// Recherche tous les ParticleSystem enfants et sauvegarde leurs valeurs par défaut.
        /// Le spawn rate est dans le module Emission et la taille dans le module Main (startSize).
        /// </summary>
        private void FindFireParticles()
        {
            fireParticleSystems = GetComponentsInChildren<ParticleSystem>();
            int count = fireParticleSystems.Length;
            defaultFireRateValues = new float[count];
            defaultFireStartSizeValues = new float[count];

            for (int i = 0; i < count; i++)
            {
                ParticleSystem ps = fireParticleSystems[i];
                if (ps != null)
                {
                    var mainModule = ps.main;
                    var emissionModule = ps.emission;
                    defaultFireRateValues[i] = emissionModule.rateOverTime.constant;
                    defaultFireStartSizeValues[i] = mainModule.startSize.constant;
                }
            }

            fireLight = GetComponentInChildren<Light>(); // Retrieve the Light component if present
        }

        /// <summary>
        /// Applique les réglages sur chaque ParticleSystem enfant.
        /// Les valeurs par défaut sont multipliées par fireIntensity, exactement comme pour le spawn rate.
        /// </summary>
        private void ApplyFireSettings()
        {
            // S'assurer que tous les tableaux sont initialisés correctement.
            if (fireParticleSystems == null || fireParticleSystems.Length == 0 ||
                defaultFireRateValues == null || defaultFireStartSizeValues == null ||
                defaultFireRateValues.Length != fireParticleSystems.Length ||
                defaultFireStartSizeValues.Length != fireParticleSystems.Length)
            {
                FindFireParticles();
            }

            for (int i = 0; i < fireParticleSystems.Length; i++)
            {
                ParticleSystem ps = fireParticleSystems[i];
                if (ps == null)
                    continue;

                var mainModule = ps.main;
                var emissionModule = ps.emission;
                var velocityModule = ps.velocityOverLifetime;

                // Appliquer la couleur
                mainModule.startColor = fireColor;

                // Modifier le spawn rate en multipliant la valeur par défaut par fireIntensity
                float baseRate = defaultFireRateValues[i];
                if (emissionModule.rateOverTime.mode == ParticleSystemCurveMode.Constant)
                {
                    emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(baseRate * fireIntensity);
                }
                else
                {
                    emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(baseRate * fireIntensity, baseRate * fireIntensity);
                }

                // Modifier la taille des particules de la même manière
                float baseSize = defaultFireStartSizeValues[i];
                mainModule.startSize = new ParticleSystem.MinMaxCurve(baseSize * fireIntensity);

                // Appliquer la direction du vent si le module Velocity over Lifetime est activé
                if (velocityModule.enabled)
                {
                    velocityModule.xMultiplier = fireWindDirection.x;
                    velocityModule.yMultiplier = fireWindDirection.y;
                    velocityModule.zMultiplier = fireWindDirection.z;
                }
            }

            // Apply fire light intensity based on fire intensity
            if (fireLight != null)
            {
                fireLight.intensity = fireIntensity;
                fireLight.color = fireColor;
            }
        }

        public void SetFireColor(Color newColor)
        {
            fireColor = newColor;
            ApplyFireSettings();
        }

        public void SetFireIntensity(float newIntensity)
        {
            fireIntensity = Mathf.Clamp(newIntensity, 0f, 4f);
            ApplyFireSettings();
        }

        public void SetFireWindDirection(Vector3 newWindDirection)
        {
            fireWindDirection = newWindDirection;
            ApplyFireSettings();
        }

        public Color GetFireColor() { return fireColor; }
        public float GetFireIntensity() { return fireIntensity; }
        public Vector3 GetFireWindDirection() { return fireWindDirection; }
    }
}
