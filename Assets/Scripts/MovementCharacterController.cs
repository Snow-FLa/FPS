using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementCharacterController : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed;       // 이동속도
    private Vector3 moveForce;     // 이동 힘 (x, z와 y축을 별도로 계산해 실제 이동에 적용)

    [SerializeField]
    private float gravity;
    [SerializeField]
    private float jumpForce;

    public float MoveSpeed
    {
        set => moveSpeed = Mathf.Max(0, value);
        get => moveSpeed;
    }

    private CharacterController characterController;  // 플레이어 이동 제어를 위한 컴포넌트

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 🌟 수정된 부분 1: 바닥에 닿아있고, 떨어지는 중이었다면 y축 힘을 초기화합니다.
        if (characterController.isGrounded && moveForce.y < 0)
        {
            // 완전히 0으로 하면 바닥에서 미세하게 뜰 수 있으므로, 바닥에 밀착하도록 작은 마이너스 값을 줍니다.
            moveForce.y = -2f;
        }
        else
        {
            // 땅에 닿아있지 않다면 중력 가속도를 적용하여 y축 이동 힘을 감소시킵니다.
            moveForce.y -= gravity * Time.deltaTime;
        }

        // 1초당 moveForce 속력으로 이동
        characterController.Move(moveForce * Time.deltaTime);
    }

    public void MoveTo(Vector3 direction)
    {
        // 이동 방향 = 캐릭터의 회전 값 * 방향 값
        direction = transform.rotation * new Vector3(direction.x, 0, direction.z);

        // 이동 힘 = 이동방향 * 속도
        moveForce = new Vector3(direction.x * moveSpeed, moveForce.y, direction.z * moveSpeed);
    }

    public void Jump()
    {
        if (characterController.isGrounded)
        {
            // 🌟 수정된 부분 2: 점프 힘을 += 로 더하지 않고, 그냥 = 로 대입(덮어쓰기)합니다.
            moveForce.y = jumpForce;
        }
    }
}