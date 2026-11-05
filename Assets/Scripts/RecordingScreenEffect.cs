using UnityEngine;
using UnityEngine.UI;

namespace Restart
{
    [RequireComponent(typeof(RawImage))]
    [DefaultExecutionOrder(200)]
    public sealed class RecordingScreenEffect : MonoBehaviour
    {
        [SerializeField] private ProjectionSession session;
        [SerializeField] private Material effectMaterial;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.15f;
        private static readonly int StrengthId = Shader.PropertyToID("_Strength");
        private RawImage image;
        private Material instance;
        private PlayerMotor2D player;
        private float strength;

        private void Awake()
        {
            image = GetComponent<RawImage>();
            image.raycastTarget = false;
            image.enabled = false;
            if (effectMaterial == null) return;
            instance = new Material(effectMaterial);
            instance.SetFloat(StrengthId, 0);
            image.material = instance;
            player = FindFirstObjectByType<PlayerMotor2D>();
            if (player != null) player.Restarting += Clear;
        }
        private void LateUpdate()
        {
            if (instance == null) return;
            float target = session != null && session.IsRecording ? 1f : 0f;
            strength = Mathf.MoveTowards(strength, target, Time.unscaledDeltaTime / fadeDuration);
            instance.SetFloat(StrengthId, strength);
            image.enabled = strength > 0f;
        }
        private void Clear()
        {
            strength = 0;
            if (instance != null) instance.SetFloat(StrengthId, 0);
            if (image != null) image.enabled = false;
        }
        private void OnDisable() => Clear();
        private void OnDestroy()
        {
            if (player != null) player.Restarting -= Clear;
            if (instance != null) Destroy(instance);
        }
    }
}
