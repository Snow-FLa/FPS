using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
    private Transform casingSpawnPoint;                // 탄피 생성 위치
    [SerializeField]
    private Transform bulletSpawnPoint;                // 총알 생성 위치


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

    [Header("Aim UI")]
    [SerializeField]
    private Image Aim;                               // 줌에 따른 조준점 이미지

    private float lastFireTime = 0;                          // 마지막 발사 시간
    private bool isReload = false;                          // 재장전 중인지 여부
    private bool isAttack = false;         // 공격 여부 체크용
    private bool isModeChange = false;     // 모드 전환 여부 체크용
    private float defaultModeFOV = 60;      // 기본모드에서의 카메라 FOV
    private float aimModeFOV = 40;          // AIM모드에서의 카메라 FOV

    private AudioSource audioSource;                 // 사운드 재생 컴포넌트
    private PlayerAnimatorController animator;   // 애니메이터 컨트롤러
    private CasingMemoryPool casingMemoryPool;               // 탄피 생성, 관리
    private HitMemoryPool hitMemoryPool;                    // 공격효과 생성, 관리
    private Camera mainCamera;                              // 광선 발사

    public WeaponName WeaponName => weaponSetting.weaponName;    // 무기 이름

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponentInParent<PlayerAnimatorController>();
        casingMemoryPool = GetComponent<CasingMemoryPool>();
        hitMemoryPool = GetComponent<HitMemoryPool>();
        mainCamera = Camera.main;

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

        ResetVariables();
    }

    public void StartWeaponAction(int type = 0)
    {
        

        // 모드 전환 중 액션 x
        if (isModeChange == true) return;

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
        // 마우스 우클 : 줌
        else
        {
            // 공격 중 모드 전환x
            if (isAttack == true) return;

            StartCoroutine("OnModeChange");
        }
}

    public void StopWeaponAction(int type = 0)
    {
        if (type == 0)
        {
            isAttack = false;
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
            // animator.Play("Fire", -1, 0);
            string animName = animator.AimModeIs == true ? "aimfire" : "Fire";
            animator.Play(animName, -1, 0);
            // 총구 이펙트
            if (animator.AimModeIs == false)
            {
                StartCoroutine("OnMuzzleFlashEffect");
            }

            // 발사 효과 재생
            StartCoroutine("OnMuzzleFlashEffect");

            // 발사 사운드 재생
            PlaySound(audioClipFire);

            // 탄피 생성
            casingMemoryPool.SpawnCasing(casingSpawnPoint.position, transform.right);

            // 광선 발사
            TwoStepRaycast();
        }

    }

    private void TwoStepRaycast()
    {
        Ray ray;
        RaycastHit hit;
        Vector3 targetPoint = Vector3.zero;

        // 화면의 중앙 좌표 (Aim 기준으로 Raycast 연산)
        ray = mainCamera.ViewportPointToRay(Vector2.one * 0.5f);
        // 공격 사거리(attackDistance) 안에 부딪히는 오브젝트가 있으면 targetPoint는 광선에 부딪힌 위치
        if (Physics.Raycast(ray, out hit, weaponSetting.attackDistance))
        {
            targetPoint = hit.point;
        }
        // 공격 사거리 안에 부딪히는 오브젝트가 없으면 targetPoint는 최대 사거리 위치
        else
        {
            targetPoint = ray.origin + ray.direction * weaponSetting.attackDistance;
        }
        Debug.DrawRay(ray.origin, ray.direction * weaponSetting.attackDistance, Color.red);

        // 첫번째 Raycast연산으로 얻어진 targetPoint를 목표지점으로 설정하고,
        // 총구를 시작지점으로 하여 Raycast 연산
        Vector3 attackDirection = (targetPoint - bulletSpawnPoint.position).normalized;
        if (Physics.Raycast(bulletSpawnPoint.position, attackDirection, out hit, weaponSetting.attackDistance))
        {
            hitMemoryPool.SpawnHit(hit);

            if (hit.transform.CompareTag("HitEnemy"))
            {
                // 적에게 데미지 전달
                hit.transform.GetComponent<EnemyFSM>().TakeDamage(weaponSetting.damage);
            }
        }
        Debug.DrawRay(bulletSpawnPoint.position, attackDirection * weaponSetting.attackDistance, Color.blue);
    }

    private IEnumerator OnModeChange()
    {
        float current = 0;
        float percent = 0;
        float time = 0.35f;

        animator.AimModeIs = !animator.AimModeIs;
        Aim.enabled = !Aim.enabled;

        float start = mainCamera.fieldOfView;
        float end = animator.AimModeIs == true ? aimModeFOV : defaultModeFOV;

        isModeChange = true;

        while (percent < 1)
        {
            current += Time.deltaTime;
            percent = current / time;

            // mode에 따라 카메라의 시야각을 변경
            mainCamera.fieldOfView = Mathf.Lerp(start, end, percent);

            yield return null;
        }

        isModeChange = false;
    }

    private void ResetVariables()
    {
        isReload = false;
        isAttack = false;
        isModeChange = false;
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
            // Move(기본) 상태이거나 aimfirepose(정조준) 상태로 돌아왔을 때 모두 장전 완료 처리
            if (audioSource.isPlaying == false && (animator.CurrentAnimationIs("Move") || animator.CurrentAnimationIs("aimfirepose")))
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