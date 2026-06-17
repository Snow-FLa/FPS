using System.Collections.Generic; // 🌟 필수: List를 사용하려면 반드시 필요합니다.
using UnityEngine;

public class MemoryPool
{
    private class PoolItem
    {
        public GameObject gameObject;   // 풀링할 프리팹
        public bool isActive;           // 현재 사용 중인지 여부
    }

    private int increaseCount = 5; // 풀링할 객체가 부족할 때 추가로 생성할 개수
    private int maxCount;          // 현재 풀에 생성되어 있는 모든 객체의 개수
    private int activeCount;       // 현재 활성화된(화면에 보이는) 객체의 개수

    private GameObject poolObject;
    private List<PoolItem> poolItemList;

    public int MaxCount => maxCount;
    public int ActiveCount => activeCount;

    // 🌟 수정됨: 생성자 매개변수 충돌 및 오타 해결
    public MemoryPool(GameObject prefab)
    {
        this.maxCount = 0;
        this.activeCount = 0;
        this.poolObject = prefab;  // 매개변수로 받은 prefab을 넣어줍니다.

        poolItemList = new List<PoolItem>();

        InstantiateObjects();
    }

    public void InstantiateObjects()
    {
        maxCount += increaseCount;

        for (int i = 0; i < increaseCount; ++i)
        {
            PoolItem poolItem = new PoolItem();

            poolItem.isActive = false;
            poolItem.gameObject = GameObject.Instantiate(poolObject);
            poolItem.gameObject.SetActive(false);

            poolItemList.Add(poolItem);
        }
    }

    public void DestroyObject()
    {
        if (poolItemList == null) return;

        int count = poolItemList.Count;
        for (int i = 0; i < count; ++i)
        {
            GameObject.Destroy(poolItemList[i].gameObject);
        }

        poolItemList.Clear();
    }

    public GameObject ActivatePoolItem()
    {
        if (poolItemList == null) return null;

        // 현재 생성해서 관리하는 모든 오브젝트 개수와 현재 활성화 상태인 오브젝트 개수 비교
        // 모든 오브젝트가 활성화 상태이면 새로운 오브젝트 필요
        if (maxCount == activeCount)
        {
            InstantiateObjects();
        }

        int count = poolItemList.Count;
        for (int i = 0; i < count; ++i)
        {
            PoolItem poolItem = poolItemList[i];

            if (poolItem.isActive == false)
            {
                activeCount++;

                poolItem.isActive = true;
                poolItem.gameObject.SetActive(true);

                return poolItem.gameObject;
            }
        }

        return null;
    }

    /// <summary>
    /// 게임에 사용중인 모든 오브젝트를 비활성화 상태로 설정
    /// </summary>
    public void DeactivateAllPoolItems()
    {
        if (poolItemList == null) return;

        int count = poolItemList.Count;
        for (int i = 0; i < count; ++i)
        {
            PoolItem poolItem = poolItemList[i];

            if (poolItem.gameObject != null && poolItem.isActive == true)
            {
                poolItem.isActive = false;
                poolItem.gameObject.SetActive(false);
            }
        }

        activeCount = 0;
    }

    /// <summary>
    /// 🎁 보너스 추가: 개별 오브젝트를 비활성화(회수) 하는 메서드
    /// (총알이 벽에 부딪히거나, 이펙트가 끝났을 때 사용하세요)
    /// </summary>
    public void DeactivatePoolItem(GameObject removeObject)
    {
        if (poolItemList == null || removeObject == null) return;

        int count = poolItemList.Count;
        for (int i = 0; i < count; ++i)
        {
            PoolItem poolItem = poolItemList[i];

            if (poolItem.gameObject == removeObject)
            {
                activeCount--;
                poolItem.isActive = false;
                poolItem.gameObject.SetActive(false);
                return;
            }
        }
    }
}