using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    #region 属性
    public Transform target;
    public Vector2 offset;
    public float smoothTime = 0.2f;
    public float lookAheadDistance = 1.5f;
    public float lookAheadSmoothTime = 0.15f;
    public bool useBounds;
    public Vector2 minPosition;
    public Vector2 maxPosition;

    private Vector3 velocity;
    private Vector2 lookAhead;
    private Vector2 lookAheadVelocity;
    private Vector3 lastTargetPosition;
    #endregion


    private void Awake()
    {
        //ResolveTarget();

        if (target == null)
        {
            // Debug.LogError("CameraFollow2D ����: δָ�� Target������ Inspector ����н����������ק�� Target ��λ��");
            return;
        }

        lastTargetPosition = target.position;
        transform.position = GetCameraPosition(target.position);
    }

    private void LateUpdate()
    {
        //ResolveTarget();

        if (target == null)
            return;

        Vector3 targetDelta = target.position - lastTargetPosition;
        Vector2 desiredLookAhead = targetDelta.sqrMagnitude > 0.0001f
            ? new Vector2(Mathf.Sign(targetDelta.x) * lookAheadDistance, Mathf.Sign(targetDelta.y) * lookAheadDistance)
            : Vector2.zero;

        lookAhead = Vector2.SmoothDamp(lookAhead, desiredLookAhead, ref lookAheadVelocity, lookAheadSmoothTime);

        Vector3 desiredPosition = GetCameraPosition(target.position + (Vector3)lookAhead);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        lastTargetPosition = target.position;
    }
    #region 注释
    /*
    private void ResolveTarget()
    {
        if (target != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            return;
        }

        // ע�⣺�˴��������ƶ���ӦΪʵ����ҿ��ƽű����������������Ŀ����
        �����ƶ� playerMove = FindObjectOfType<�����ƶ�>();
        if (playerMove != null)
            target = playerMove.transform;
    }
    */
    #endregion

    private Vector3 GetCameraPosition(Vector3 targetPosition)
    {
        float x = targetPosition.x + offset.x;
        float y = targetPosition.y + offset.y;

        if (useBounds)
        {
            x = Mathf.Clamp(x, minPosition.x, maxPosition.x);
            y = Mathf.Clamp(y, minPosition.y, maxPosition.y);
        }

        return new Vector3(x, y, transform.position.z);
    }
}
