using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlitchController : MonoBehaviour
{
    [SerializeField]
    private AnimationCurve glitchProgress = null;
    [SerializeField]
    private PlayerController controller = null;
    [Header("V Ram")]
    [SerializeField]
    private AnimationCurve vramIntensity = null;
    [SerializeField]
    private AnimationCurve vramMultiplier = null;
    [SerializeField]
    private float vramSpeed = 2f;
    [SerializeField]
    private ShaderEffect_CorruptedVram vRamController = null;
    [Header("Color Bleed")]
    [SerializeField]
    private AnimationCurve bleedIntensity = null;
    [SerializeField]
    private AnimationCurve bleedMultiplier = null;
    [SerializeField]
    private float bleedSpeed = 2f;
    [SerializeField]
    private ShaderEffect_BleedingColors bleedController = null;
    [Header("CRT")]
    [SerializeField]
    private ShaderEffect_CRT crtController = null;
    [Header("Scanner")]
    [SerializeField]
    private AnimationCurve scannerIntensity = null;
    [SerializeField]
    private float scannerMultiplier = 10f;
    [SerializeField]
    private float scannerSpeed = 2f;
    [SerializeField]
    private ShaderEffect_Scanner scannerController = null;
    [Header("SFX")]
    [SerializeField]
    private AudioSource SFX = null;

    private void Update()
    {
        if (controller.Hp.Scalar <= 0.3f)
        {
            float m = glitchProgress.Evaluate(1f - controller.Hp.Scalar);
            float ramInt = vramIntensity.Evaluate(Time.time * vramSpeed * m) * vramMultiplier.Evaluate(Time.time * m);
            SFX.volume = ramInt;
            vRamController.enabled = crtController.enabled = ramInt > 0.5f;
            vRamController.shift = ramInt * m;
            //SFX.pitch = ramInt / vramMultiplier;
            bleedController.intensity = bleedIntensity.Evaluate(Time.time * bleedSpeed * m) * bleedMultiplier.Evaluate(Time.time * m) * m;
            float scannerInt = scannerIntensity.Evaluate(Time.time * scannerSpeed * m) * scannerMultiplier;
            scannerController.enabled = scannerInt > 0.5f;
            scannerController.area = scannerInt * m;
        }
        else
            vRamController.enabled = crtController.enabled = scannerController.enabled = false;
    }
}
