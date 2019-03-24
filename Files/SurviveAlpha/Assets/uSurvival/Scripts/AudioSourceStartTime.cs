using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioSourceStartTime : MonoBehaviour
{
    new public AudioSource audio;
    public float time = 0;

    void Start()
    {
        audio.time = time;
    }
}
