using UnityEngine;
using System.Collections;
public class LightFlickering : MonoBehaviour
{
    // Light Variables
    [Header("Light Settings")]
    [SerializeField] private GameObject[] lights;

    // Active and Inactive Duration
    [Header("Duration Settings")]
    [SerializeField] private float activeDuration = 5f;
    [SerializeField] private float inactiveDuration = 3.5f;

    // Flicker Variables
    [Header("Flicker Settings")]
    [SerializeField] private float flickerPeriod = 3f;

    // Random Variables
    [Header("Random Settings")]
    [SerializeField] private float minActiveDuration = 2f;
    [SerializeField] private float maxActiveDuration = 8f;
    [SerializeField] private float minInactiveDuration = 1f;
    [SerializeField] private float maxInactiveDuration = 5f;

    // Start
    void Start()
    {
        StartCoroutine(LightRoutine());
    }

    // Light Routine
    IEnumerator LightRoutine()
    {
        while (true)
        {
            // Activate Random Lights
            SetRandomLightsActive();

            // Active Period
            yield return new WaitForSeconds(Random.Range(minActiveDuration, maxActiveDuration));

            // Flicker Period
            yield return StartCoroutine(FlickerLights());

            // Disable All Lights
            SetAllLightsActive(false);

            // Inactive Period
            yield return new WaitForSeconds(Random.Range(minInactiveDuration, maxInactiveDuration));
        }
    }

    // Set Random Lights Active
    void SetRandomLightsActive()
    {
        foreach (GameObject lightObject in lights)
        {
            if (lightObject != null)
            {
                lightObject.SetActive(Random.value > 0.5f);
            }
        }
    }

    // Flicker Lights
    IEnumerator FlickerLights()
    {
        float timer = 0f;

        while (timer < flickerPeriod)
        {
            // Randomly Flicker Each Light
            foreach (GameObject lightObject in lights)
            {
                if (lightObject != null)
                {
                    lightObject.SetActive(Random.value > 0.5f);
                }
            }

            yield return new WaitForSeconds(Random.Range(0.05f, 0.3f));

            timer += Time.deltaTime;
        }

        // Activate Random Lights Again
        SetRandomLightsActive();
    }

    // Set All Lights Active
    void SetAllLightsActive(bool state)
    {
        foreach (GameObject lightObject in lights)
        {
            if (lightObject != null)
            {
                lightObject.SetActive(state);
            }
        }
    }
}