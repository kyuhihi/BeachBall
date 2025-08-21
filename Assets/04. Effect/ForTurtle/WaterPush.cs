using UnityEngine;

public class WaterPush : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;

    public float pushForce = 2f;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
    }

    void OnParticleCollision(GameObject other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 pushDir = (other.transform.position - transform.position).normalized;

            rb.AddForce(pushDir * pushForce, ForceMode.Impulse);

            // Debug.Log("Object pushed by water particle!");
        }
    }
}