using UnityEngine;

// 열거형 이름도 통일감을 위해 HitType으로 변경했습니다.
public enum HitType { Normal = 0, Obstacle, Enemy, }

public class HitMemoryPool : MonoBehaviour
{
    [SerializeField]
    private GameObject[] hitPrefab;      // 피격 이펙트 배열 (0번: Hit, 1번: HitObs)
    private MemoryPool[] memoryPool;     // 피격 이펙트 메모리풀

    private void Awake()
    {
        // 피격 이펙트가 여러 종류이면 종류별로 memoryPool 생성
        memoryPool = new MemoryPool[hitPrefab.Length];
        for (int i = 0; i < hitPrefab.Length; ++i)
        {
            memoryPool[i] = new MemoryPool(hitPrefab[i]);
        }
    }

    public void SpawnHit(RaycastHit hit)
    {
        // 부딪힌 오브젝트의 Tag 정보에 따라 다르게 처리
        if (hit.transform.CompareTag("Hit"))
        {
            OnSpawnHit(HitType.Normal, hit.point, Quaternion.LookRotation(hit.normal));
        }
        else if (hit.transform.CompareTag("HitObs"))
        {
            OnSpawnHit(HitType.Obstacle, hit.point, Quaternion.LookRotation(hit.normal));
        }
        else if (hit.transform.CompareTag("HitEnemy"))
        {
            OnSpawnHit(HitType.Enemy, hit.point, Quaternion.LookRotation(hit.normal));
        }
    }

    public void OnSpawnHit(HitType type, Vector3 position, Quaternion rotation)
    {
        // 배열의 인덱스를 활용해 알맞은 이펙트를 꺼내옵니다.
        GameObject item = memoryPool[(int)type].ActivatePoolItem();
        item.transform.position = position;
        item.transform.rotation = rotation;

        // 이전 단계에서 클래스명을 'Hit'으로 바꿨으므로 여기서도 Hit 컴포넌트를 가져옵니다.
        item.GetComponent<Hit>().Setup(memoryPool[(int)type]);
    }
}