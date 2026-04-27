using UnityEngine;

public class ParticlePool : MonoBehaviour
{
    public GameObject particlePrefab;
    public int poolSize = 4;

    private GameObject[] pool;

    void Awake()
    {
        pool = new GameObject[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            pool[i] = Instantiate(particlePrefab);
            pool[i].SetActive(false);
        }
    }

    public GameObject GetParticle()
    {
        for (int i = 0; i < pool.Length; i++)
        {
            if (!pool[i].activeSelf)
                return pool[i];
        }
        return null;
    }
}