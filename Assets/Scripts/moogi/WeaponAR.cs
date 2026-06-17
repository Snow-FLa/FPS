using System.Collections;
using UnityEngine;

[System.Serializable]
public class AmmoEvent : UnityEngine.Events.UnityEvent<int, int> { }

public class WeaponAR : MonoBehaviour
{
    [HideInInspector]
    public AmmoEvent onAmmoEvent = new AmmoEvent();                 // 탄 수 변경 이벤트

    [Header("Fire Effect")]
    [SerializeField]
    private GameObject muzzleFlashEffect;              // 발사 효과

    [Header("Spawn Points")]
    [SerializeField]
    private Transform casingSpawnPoint;                // 총알 생성 위치

    [Header("Audio Clips")]
    [SerializeField]
    private AudioClip audioClipTakeOutWeapon;      // 무기 장착 사운드
    [SerializeField]
    private AudioClip audioClipFire;               // 발사 사운드
    [SerializeField]
    private AudioClip audioClipReloadTactical;             // 전술 장전 사운드
    [SerializeField]
    private AudioClip audioClipReloadEmpty;             // 빈 탄창 장전 사운드

    [Header("Weapon Settings")]
    [SerializeField]
    private WeaponSetting weaponSetting;             // 무기 설정

    private float lastFireTime = 0;                          // 마지막 발사 시간
    private bool isReload = false;                          // 재장전 중인지 여부

    private AudioSource audioSource;                 // 사운드 재생 컴포넌트
    private PlayerAnimatorController animator;   // 애니메이터 컨트롤러
    private CasingMemoryPool casingMemoryPool;               // 탄피 생성, 관리

    public WeaponName WeaponName => weaponSetting.weaponName;    // 무기 이름

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponentInParent<PlayerAnimatorController>();
        casingMemoryPool = GetComponent<CasingMemoryPool>();

        // 처음 탄 수 최대
        weaponSetting.currentAmmo = weaponSetting.maxAmmo;
    }

    private void OnEnable()
    {
        // 무기 장착 사운드 재생
        PlaySound(audioClipTakeOutWeapon);
        // 발사 효과 비활성화
        muzzleFlashEffect.SetActive(false);

        onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);
    }

    public void StartWeaponAction(int type = 0)
    {
        // 재장전시 액션 x
        if(isReload == true)
        {
            return;
        }

        // 마우스 좌클 : 공격
        if (type == 0)
        {
            //연속 공격
            if (weaponSetting.isAutomaticAttack == true)
            {
                StartCoroutine("OnAttackLoop");
            }
            //단발 공격
            else
            {
                OnAttack();
            }
        }
    }

    public void StopWeaponAction(int type = 0)
    {
        if (type == 0)
        {
            StopCoroutine("OnAttackLoop");
        }
    }

    public void StartReload()
    {
        // 재장전 중이면 재장전 불가
        if (isReload == true)
        {
            return;
        }

        // 무기 액션 중 장전하면 액션 종료 후 재장전
        StopWeaponAction();

        StartCoroutine("OnReload");
    }

    private IEnumerator OnAttackLoop()
    {
        while (true)
        {
            OnAttack();

            yield return null;
        }
    }

    public void OnAttack()
    {
        // 공격 속도에 따른 발사 시간 체크
        if (Time.time - lastFireTime > weaponSetting.attackRate)
        {
            if (animator.MoveSpeed > 0.5f)
            {
                return;
            }

            lastFireTime = Time.time;

            //탄 수 없으면 공격불가
            if( weaponSetting.currentAmmo <= 0)
            {
                return;
            }
            //공격시 currentAmmo 감소
            weaponSetting.currentAmmo--;
            onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);

            // 공격 애니메이션 재생
            animator.Play("Fire", -1, 0);

            // 발사 효과 재생
            StartCoroutine("OnMuzzleFlashEffect");

            // 발사 사운드 재생
            PlaySound(audioClipFire);

            // 탄피 생성
            casingMemoryPool.SpawnCasing(casingSpawnPoint.position, transform.right);
        }

    }

    private IEnumerator OnMuzzleFlashEffect()
    {
        muzzleFlashEffect.SetActive(true);
        yield return new WaitForSeconds(weaponSetting.attackRate * 0.2f);
        muzzleFlashEffect.SetActive(false);
    }

    private IEnumerator OnReload()
    {
        isReload = true;

        // 1. 재생할 애니메이션 이름, 사운드, 약실에 남길 총알 수를 담을 변수 준비
        string reloadAnimName = "";
        AudioClip selectedReloadSound = null; // 🌟 재생할 사운드를 담아둘 변수 추가
        int chamberRound = 0;

        // 2. 잔탄 수에 따른 조건 분기
        if (weaponSetting.currentAmmo > 0)
        {
            // 잔탄이 1발 이상 있을 때 (전술 장전)
            reloadAnimName = "Reload_Tactical";
            selectedReloadSound = audioClipReloadTactical; // 🌟 전술 장전 사운드 할당
            chamberRound = 1;
        }
        else
        {
            // 잔탄이 0발일 때 (빈 탄창 장전)
            reloadAnimName = "Reload_Empty";
            selectedReloadSound = audioClipReloadEmpty;    // 🌟 빈 탄창 장전 사운드 할당
            chamberRound = 0;
        }

        // 3. 결정된 애니메이션과 사운드 재생
        animator.Play(reloadAnimName, -1, 0);
        PlaySound(selectedReloadSound); // 🌟 분기된 사운드 재생

        while (true)
        {
            // 사운드 재생이 끝나고, 캐릭터가 다시 Move 상태로 돌아왔다면 장전 완료
            if (audioSource.isPlaying == false && animator.CurrentAnimationIs("Move"))
            {
                weaponSetting.currentAmmo = weaponSetting.maxAmmo + chamberRound;
                onAmmoEvent.Invoke(weaponSetting.currentAmmo, weaponSetting.maxAmmo);

                isReload = false;
                yield break;
            }

            // 유니티 멈춤(프리징) 방지를 위한 필수 1프레임 대기
            yield return null;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        audioSource.Stop();             // 기존에 재생중인 사운드를 정지하고,
        audioSource.clip = clip;        // 새로운 사운드 clip으로 교체 후
        audioSource.Play();             // 사운드 재생
    }
}