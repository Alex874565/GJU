using System.Collections;
using UnityEngine;

public class Battery : MonoBehaviour, IInteractable, IResettable
{
    [Header("References")]
    [SerializeField] private Transform visual;
    [SerializeField] private Light batteryLight;

    [Header("Lift")]
    [SerializeField] private float liftHeight = 0.25f;
    [SerializeField] private float moveSpeed = 5f;

    [Header("Float")]
    [SerializeField] private float floatAmplitude = 0.05f;
    [SerializeField] private float floatFrequency = 2f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private Vector3 rotationAxis = new Vector3(0.6f, 1f, 0.3f);
    [SerializeField] private float wobbleAmount = 8f;
    [SerializeField] private float wobbleSpeed = 2f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip[] pickupSounds;
    
    [Header("Signal Light")]
    [SerializeField] private Vector2 signalDelayRange = new Vector2(2f, 6f);
    [SerializeField] private int minFlashes = 1;
    [SerializeField] private int maxFlashes = 3;
    [SerializeField] private float flashOnTime = 0.06f;
    [SerializeField] private float flashOffTime = 0.08f;
    [SerializeField] private float signalIntensity = 2f;
    
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool pickedUp;

    private Coroutine signalRoutine;

    private Vector3 visualBaseLocalPos;
    private bool isHighlighted;
    private float floatTimer;
    
    private bool isFlickering;
    private float flickerTimer;
    private float targetLightIntensity;
    private float baseLightIntensity;

    private void Awake()
    {
        if (visual == null)
            visual = transform;

        visualBaseLocalPos = visual.localPosition;
        
        if (batteryLight != null)
        {
            batteryLight.enabled = false;
            batteryLight.intensity = 0f;
        }
        
        startPosition = transform.position;
        startRotation = transform.rotation;
    }
    
    private void OnEnable()
    {
        signalRoutine = StartCoroutine(SignalRoutine());
    }

    private void OnDisable()
    {
        if (signalRoutine != null)
            StopCoroutine(signalRoutine);

        if (batteryLight != null)
        {
            batteryLight.enabled = false;
            batteryLight.intensity = 0f;
        }
    }

    private void Update()
    {
        if (pickedUp) return;
        
        Vector3 targetLocalPos = visualBaseLocalPos;

        if (isHighlighted)
        {
            floatTimer += Time.deltaTime * floatFrequency;

            float floatOffset = Mathf.Sin(floatTimer) * floatAmplitude;
            targetLocalPos += Vector3.up * (liftHeight + floatOffset);

            visual.localRotation = Quaternion.Slerp(
                visual.localRotation,
                Quaternion.Euler(
                    Mathf.Sin(Time.time * 0.7f) * 10f,
                    visual.localEulerAngles.y,
                    Mathf.Cos(Time.time * 0.6f) * 10f
                ),
                Time.deltaTime * 2f
            );
        }

        visual.localPosition = Vector3.Lerp(
            visual.localPosition,
            targetLocalPos,
            moveSpeed * Time.deltaTime
        );
    }
    
    private IEnumerator SignalRoutine()
    {
        while (true)
        {
            // long darkness
            yield return new WaitForSeconds(Random.Range(signalDelayRange.x, signalDelayRange.y));

            // flicker burst
            float burstDuration = Random.value < 0.25f 
                ? Random.Range(0.6f, 1.2f)   // rare long panic flicker
                : Random.Range(0.15f, 0.4f);
            float timer = 0f;

            while (timer < burstDuration)
            {
                timer += Time.deltaTime;

                // random chance to be on this frame
                bool on = Random.value > 0.35f;

                batteryLight.enabled = on;

                if (on)
                {
                    // unstable intensity instead of fixed
                    batteryLight.intensity = signalIntensity * Random.Range(0.4f, 1f);
                }

                yield return new WaitForSeconds(Random.Range(0.02f, 0.08f));
            }

            // ensure fully off after burst
            batteryLight.enabled = false;
            batteryLight.intensity = 0f;
        }
    }

    public void ChangeHighlight(bool highlighted)
    {
        if(highlighted)
        {
            InteractPrompt.Instance?.Show("Use");
        }
        else
        {
            InteractPrompt.Instance?.Hide();
        }
        isHighlighted = highlighted;
    }

    public void Interact(PlayerInteract player)
    {
        if (pickedUp) return;

        pickedUp = true;
        isHighlighted = false;
        InteractPrompt.Instance?.Hide();

        player.AddBattery(1);

        if (pickupSounds != null && pickupSounds.Length > 0)
            AudioManager.PlaySFX(pickupSounds, transform.position);

        gameObject.SetActive(false);
    }
    
    public void ResetState()
    {
        pickedUp = false;
        isHighlighted = false;

        transform.SetPositionAndRotation(startPosition, startRotation);

        if (visual != null)
        {
            visual.localPosition = visualBaseLocalPos;
            visual.localRotation = Quaternion.identity;
        }

        if (batteryLight != null)
        {
            batteryLight.enabled = false;
            batteryLight.intensity = 0f;
        }

        gameObject.SetActive(true);
    }
}