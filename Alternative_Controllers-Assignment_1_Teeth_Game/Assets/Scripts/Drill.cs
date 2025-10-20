using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drill : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float shakeAmount = 0.05f; // Maximum shake offset
    [SerializeField] private float shakeSpeed = 20f;    // How fast it shakes

    private bool isActive = false;
    private Vector3 shakeOffset;

    void Update()
    {
        if (isActive)
        {
            ApplyShake();
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }

    private void ApplyShake()
    {
        float offsetX = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f) * shakeAmount;
        float offsetY = (Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f) * shakeAmount;
        shakeOffset = new Vector3(offsetX, offsetY, 0f);
    }

    void LateUpdate()
    {
        // Apply the shake on top of the current position set by other scripts
        transform.localPosition += shakeOffset;
    }

    public void Activate()
    {
        isActive = true;
    }

    public void Deactivate()
    {
        isActive = false;
    }
}
