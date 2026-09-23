using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AvatarSimpleLipSync : MonoBehaviour
{
    [Header("Audio & Head References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform playerTarget;

    [Header("Mouth Animation Settings")]
    [Tooltip("Target jaw bone or mouth blendshape mesh")]
    [SerializeField] private SkinnedMeshRenderer faceMesh;
    [SerializeField] private int mouthOpenBlendShapeIndex = 0;
    [SerializeField] private Transform jawBone;
    [SerializeField] private float jawOpenAngle = 20f;
    [SerializeField] private float sensitivity = 100f;

    private float[] audioSamples = new float[64];
    private Quaternion initialJawRotation;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (jawBone != null) initialJawRotation = jawBone.localRotation;
    }

    private void Update()
    {
        // Simple subtle look-at player
        if (headTransform != null && playerTarget != null)
        {
            Vector3 direction = (playerTarget.position - headTransform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                headTransform.rotation = Quaternion.Slerp(headTransform.rotation, targetRot, Time.deltaTime * 2f);
            }
        }

        // Audio volume reactive mouth motion
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.GetOutputData(audioSamples, 0);
            float sum = 0f;
            for (int i = 0; i < audioSamples.Length; i++)
            {
                sum += Mathf.Abs(audioSamples[i]);
            }
            float rms = Mathf.Clamp01((sum / audioSamples.Length) * sensitivity);

            // Animate blendshape if available
            if (faceMesh != null && mouthOpenBlendShapeIndex >= 0 && mouthOpenBlendShapeIndex < faceMesh.sharedMesh.blendShapeCount)
            {
                faceMesh.SetBlendShapeWeight(mouthOpenBlendShapeIndex, rms * 100f);
            }

            // Or animate jaw bone rotation
            if (jawBone != null)
            {
                jawBone.localRotation = initialJawRotation * Quaternion.Euler(rms * jawOpenAngle, 0, 0);
            }
        }
        else
        {
            if (faceMesh != null && mouthOpenBlendShapeIndex >= 0 && mouthOpenBlendShapeIndex < faceMesh.sharedMesh.blendShapeCount)
            {
                faceMesh.SetBlendShapeWeight(mouthOpenBlendShapeIndex, Mathf.Lerp(faceMesh.GetBlendShapeWeight(mouthOpenBlendShapeIndex), 0f, Time.deltaTime * 10f));
            }
            if (jawBone != null)
            {
                jawBone.localRotation = Quaternion.Slerp(jawBone.localRotation, initialJawRotation, Time.deltaTime * 10f);
            }
        }
    }
}

