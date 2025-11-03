using UnityEngine;

public class Michel_UA_Spatializer : MonoBehaviour
{
    public Transform player;
    private AudioSource audioSource;
	
    [Header("Audio Physics Settings")]
    public float maxDistance = 20f;
    public float minDistance = 1f;
    public float closeRange = 3f;
    public float speedOfSound = 343f;
	
    [Header("LowPass Filter")]
    public float LPFFront = 5000f;
    public float LPFBack = 1000f;
	
    [Header("Obstacle Interaction")]
    public LayerMask obstacleMask;
    public float occlusionFactor = 0.5f;
    public float materialAbsorption = 0.3f;

    [Header("Volume Curve")]
    public AnimationCurve volumeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
	
    [Header("Delay Settings")]
    public float delayTime = 0.5f;
    private float delayTimer = 0f;

    private float leftEarVolume = 1f;
    private float rightEarVolume = 1f;
    private float leftEarDelay = 0f;
    private float rightEarDelay = 0f;

    private bool isPaused = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("AudioSource non trovato su " + gameObject.name);
            enabled = false;
            return;
        }

        audioSource.spatialBlend = 0.0f;
        audioSource.rolloffMode = AudioRolloffMode.Custom;
        audioSource.Play();
    }
	
    void Update()
    {
        if (Time.timeScale == 0)
        {
            if (!isPaused)
            {
                audioSource.Pause();
                audioSource.volume = 0f;
                isPaused = true;
            }
        }
        else
        {
            if (isPaused)
            {
                audioSource.UnPause();
                audioSource.volume = CalculateVolume(Vector3.Distance(transform.position, player.position)) * OcclusionCheck();
                isPaused = false;
            }
			
            float distance = Vector3.Distance(transform.position, player.position);
            float volume = CalculateVolume(distance) * OcclusionCheck();
			
            ApplyHRTF(distance);
            ApplyReverb(distance);

            audioSource.volume = Mathf.Clamp(volume, 0f, 1f);
            float panFactor = Mathf.Lerp(0.2f, 0.8f, Mathf.InverseLerp(0f, closeRange, distance));
            audioSource.panStereo = Mathf.Clamp((rightEarVolume - leftEarVolume) * panFactor, -1f, 1f);

            if (delayTimer > 0)
            {
                delayTimer -= Time.deltaTime;
                if (delayTimer <= 0)
                {
                    audioSource.Play();
                }
            }
        }
    }

    float CalculateVolume(float distance)
    {
        if (distance > maxDistance) return 0f;

        if (distance < minDistance) return 1f;

        float normalizedDistance = (distance - minDistance) / (maxDistance - minDistance);

        return volumeCurve.Evaluate(normalizedDistance);
    }

    float OcclusionCheck()
    {
        RaycastHit hit;
        Vector3 direction = (player.position - transform.position).normalized;
        if (Physics.Raycast(transform.position, direction, out hit, maxDistance, obstacleMask))
        {
            return Mathf.Clamp(1f - (occlusionFactor + materialAbsorption), 0f, 1f);
        }
        return 1f;
    }
	
    void ApplyHRTF(float distance)
    {
        Vector3 toSound = (transform.position - player.position).normalized;
        Vector3 forward = player.forward;
        float angle = Vector3.SignedAngle(forward, toSound, Vector3.up);
        float pan = Mathf.Sign(angle) * Mathf.Sin(Mathf.Abs(angle) * Mathf.Deg2Rad);
        float distanceFactor = Mathf.Clamp01(distance / maxDistance);
        float ildFactor = Mathf.LerpUnclamped(1f, 0.2f, distanceFactor);
        float earSeparation = 0.2f;
        float soundDelay = (earSeparation / speedOfSound) * 1000f;
        float absPan = Mathf.Abs(pan);
        float backFactor = Mathf.Clamp01((Mathf.Cos(angle * Mathf.Deg2Rad) + 1) / 2);
        float headShadowing = Mathf.LerpUnclamped(1f, 0.6f, 1 - backFactor);
        float lowPassFactor = Mathf.LerpUnclamped(1f, 0.6f, 1 - backFactor); 

        if (angle > 0)
        {
            rightEarVolume = 1f * headShadowing;
            leftEarVolume = Mathf.Clamp01(1f - (absPan * ildFactor)) * headShadowing;
            rightEarDelay = 0f;
            leftEarDelay = soundDelay * absPan;
        }
        else
        {
            leftEarVolume = 1f * headShadowing;
            rightEarVolume = Mathf.Clamp01(1f - (absPan * ildFactor)) * headShadowing;
            leftEarDelay = 0f;
            rightEarDelay = soundDelay * absPan;
        }

        float closeFactor = Mathf.InverseLerp(minDistance, closeRange, distance);
        leftEarVolume = Mathf.LerpUnclamped(1f, leftEarVolume, closeFactor);
        rightEarVolume = Mathf.LerpUnclamped(1f, rightEarVolume, closeFactor);

        ApplyLowPassFilter(leftEarVolume, lowPassFactor);
        ApplyLowPassFilter(rightEarVolume, lowPassFactor);
    }

    void ApplyLowPassFilter(float volume, float lowPassFactor)
    {

        float cutoffFrequency = Mathf.Lerp(LPFFront, LPFBack, 1 - lowPassFactor); 

        AudioLowPassFilter filter = GetComponent<AudioLowPassFilter>();
        if (filter == null)
        {
            filter = gameObject.AddComponent<AudioLowPassFilter>();
        }
        filter.cutoffFrequency = cutoffFrequency;

    }

    void ApplyReverb(float distance)
    {

Applicazione del riverbero come prima

    }

    void TriggerDelay()
    {
        delayTimer = delayTime;

        audioSource.Stop();
    }
}
