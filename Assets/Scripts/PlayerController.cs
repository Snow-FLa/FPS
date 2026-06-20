using UnityEngine;
using UnityEngine.InputSystem; // 신버전 입력 시스템 사용

public class PlayerController : MonoBehaviour
{

    [Header("Audio Clips")]
    [SerializeField]
    private AudioClip audioClipRun;
    [SerializeField]
    private AudioClip audioClipWalk;

    private RotateToMouse               rotateToMouse;  // 마우스 이동으로 카메라 회전
    private MovementCharacterController movement; // 플레이어 이동 제어를 위한 컴포넌트
    private Status                      status; // 플레이어의 상태 (걷기, 달리기 속도 등)
    private PlayerAnimatorController    animator; // 플레이어 애니메이션 제어를 위한 컴포넌트
    private AudioSource                 audioSource; // 플레이어의 발소리를 재생하기 위한 오디오 소스
    private WeaponAR                    weapon; // 무기를 이용한 공격 제어

    private void Awake()
    {
        // 마우스 커서를 보이지 않게 설정하고, 현재 위치에 고정시킨다
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        rotateToMouse = GetComponent<RotateToMouse>();
        movement = GetComponent<MovementCharacterController>();
        status = GetComponent<Status>();
        animator = GetComponent<PlayerAnimatorController>();
        audioSource = GetComponent<AudioSource>();
        weapon = GetComponentInChildren<WeaponAR>();
    }

    private void Update()
    {
        UpdateRotate();
        UpdateMove();
        UpdateJump();
        UpdateWeaponAction();
    }

    private void UpdateRotate()
    {
        // 마우스가 연결되어 있지 않은 경우 에러를 방지합니다.
        if (Mouse.current == null) return;

        // 신버전 입력 시스템: 마우스가 한 프레임 동안 움직인 거리를 Vector2(x, y)로 가져옵니다.
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // 기존 Old Input System과 값의 크기가 다르기 때문에 0.1f를 곱해 속도를 보정해 줍니다.
        float mouseX = mouseDelta.x * 0.1f;
        float mouseY = mouseDelta.y * 0.1f;

        rotateToMouse.UpdateRotate(mouseX, mouseY);
    }

    private void UpdateMove()
    {
        float x = 0f;
        float z = 0f;

        // 키보드가 연결되어 있는지 확인합니다.
        if (Keyboard.current != null)
        {
            // A, D 키 입력 (좌우)
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;

            // W, S 키 입력 (상하)
            if (Keyboard.current.wKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;
        }

        // 이동 중일 때 (걷기 or 뛰기)
        if (x != 0 || z != 0)
        {
            bool isRun = false;

            // 옆이나 뒤로 이동할 때는 달릴 수 없다 (앞으로 갈 때만 달리기 허용)
            if (z > 0 && Keyboard.current != null)
            {
                // 신버전 입력 시스템: 왼쪽 Shift 키가 눌려있는지 확인합니다.
                isRun = Keyboard.current.leftShiftKey.isPressed;
            }

            movement.MoveSpeed = isRun == true ? status.RunSpeed : status.WalkSpeed;
            animator.MoveSpeed = isRun == true ? 1 : 0.5f; // Blend Tree를 위한 애니메이션 파라미터 전달
            audioSource.clip = isRun == true ? audioClipRun : audioClipWalk;


            if (!audioSource.isPlaying)
            {
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        // 제자리에 멈춰있을 때
        else
        {
            movement.MoveSpeed = 0f;
            animator.MoveSpeed = 0f;

            if (audioSource.isPlaying == true)
            {
                audioSource.Stop();
            }
        }

        // 방향 벡터 생성 (대각선 이동 시 속도가 빨라지는 것을 막기 위해 normalized 적용)
        Vector3 moveDirection = new Vector3(x, 0, z).normalized;

        movement.MoveTo(moveDirection);
    }

    private void UpdateJump()
    {
        // 신버전 입력 시스템: Space 키가 눌려있는지 확인합니다.
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            movement.Jump();
        }
    }

    private void UpdateWeaponAction()
    {
        // 마우스가 연결되어 있지 않으면 작동하지 않도록 예외 처리
        if (Mouse.current == null) return;

        // 마우스 왼쪽 버튼을 누른 순간
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            weapon.StartWeaponAction();
        }
        // 마우스 왼쪽 버튼을 뗀 순간
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            weapon.StopWeaponAction();
        }

        //  마우스 우클릭을 눌렀을 때 (정조준 시작)
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            weapon.StartWeaponAction(1);
        }
        //  마우스 우클릭을 뗐을 때 (정조준 해제)
        else if (Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame)
        {
            weapon.StopWeaponAction(1);
        }

        // R 키를 누른 순간 장전 시작
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            weapon.StartReload();
        }
    }
}