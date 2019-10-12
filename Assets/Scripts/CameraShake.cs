using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake
{
    public struct ElementShake
    {
        public float amplitude,
                     frequency;

        public ElementShake(float amplitude, float frequency)
        {
            this.amplitude = amplitude;
            this.frequency = frequency;
        }
    }

    public ElementShake[] rotationShake = new ElementShake[3];
    public ElementShake[] positionShake = new ElementShake[3];
    public ElementShake fieldOfViewShake;

    public float Duration { get; }
    public float BlendInTime { get; }
    public float BlendOutTime { get; }

    public CameraShake()
        : this(1f, 0.1f, 0.2f)
    {
    }

    public CameraShake(float duration, float blendInTime, float blendOutTime)
    {
        Duration = duration;
        BlendInTime = blendInTime;
        BlendOutTime = blendOutTime;

        for (int i = 0; i < rotationShake.Length; i++)
            rotationShake[i] = new ElementShake(0f, 0f);
        for (int i = 0; i < positionShake.Length; i++)
            rotationShake[i] = new ElementShake(0f, 0f);

        fieldOfViewShake = new ElementShake(0f, 0f);
    }
}