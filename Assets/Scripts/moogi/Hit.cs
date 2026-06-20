using UnityEngine;

public class Hit : MonoBehaviour
{
    private ParticleSystem particle;
    private MemoryPool memoryPool;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
    }

    public void Setup(MemoryPool pool)
    {
        memoryPool = pool;
    }

    private void Update()
    {
        // 파티클이 재생중이 아니면 삭제
        if (particle.isPlaying == false)
        {
            // 🌟 안전장치: memoryPool이 비어있지 않을 때만 회수하고, 비어있다면 에러 없이 그냥 파괴
            if (memoryPool != null)
            {
                memoryPool.DeactivatePoolItem(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}