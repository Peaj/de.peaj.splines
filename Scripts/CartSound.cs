using UnityEngine;
using System.Collections;

public class CartSound : MonoBehaviour {

    [Header("Rattle")]
    public AudioClip Rattle;
    public float RattleVolume = 0.01f;
    public float RattleDistance = 1f;
    [Header("Wind")]
    public AudioClip Wind;
    public float WindVolume = 0.5f;
    public float WindBasePitch = 0.2f;
    public float WindSpeedPitchFactor = 0.2f;
    public float WindMaxSpeedVolume = 100;
    [Header("Roll")]
    public AudioClip Roll;
    public float RollVolume = 0.5f;
    public float RollBasePitch = 0.2f;
    public float RollSpeedPitchFactor = 0.2f;
    public float RollMaxSpeedVolume = 100;

    private float lastRattle = 0f;

    private AudioSource rattleSource;
    private AudioSource windSource;
    private AudioSource rollSource;

    void Awake()
    {
        this.rattleSource = gameObject.AddComponent<AudioSource>();
        this.rattleSource.playOnAwake = false;
        this.rattleSource.clip = this.Rattle;
        this.rattleSource.volume = this.RattleVolume;
        this.windSource = gameObject.AddComponent<AudioSource>();
        this.windSource.playOnAwake = false;
        this.windSource.clip = this.Wind;
        this.windSource.volume = this.WindVolume;
        this.windSource.loop = true;
        this.windSource.Play();
        this.rollSource = gameObject.AddComponent<AudioSource>();
        this.rollSource.playOnAwake = false;
        this.rollSource.clip = this.Roll;
        this.rollSource.volume = this.RollVolume;
        this.rollSource.loop = true;
        this.rollSource.Play();
    }

    void Update()
    {
        var rb = this.GetComponent<Rigidbody>();
        float velocity = rb.velocity.magnitude;
        float t = this.RattleDistance / velocity;

        this.windSource.pitch = this.WindBasePitch + velocity * this.WindSpeedPitchFactor;
        this.windSource.volume = Mathf.InverseLerp(0, this.WindMaxSpeedVolume, velocity) * this.WindVolume;

        this.rollSource.volume = Mathf.InverseLerp(0, this.RollMaxSpeedVolume, velocity) * this.RollVolume;

        if (Time.time > this.lastRattle + t)
        {
            this.rattleSource.Play();
            this.lastRattle = Time.time;
        }
    }
}
