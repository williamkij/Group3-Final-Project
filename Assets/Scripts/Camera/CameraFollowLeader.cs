using UnityEngine;

public class CameraFollowLeader : MonoBehaviour
{
    [Header("Targets")]
    public Transform playerA;
    public Transform playerB;
    [Header("Follow")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    public float smoothTime = 0.15f;
    [Header("Lock Axis")]
    public bool lockY = true;
    public float fixedY = 0f;
    [Header("Clamp (Optional)")]
    public bool useClamp = false;
    public float minX = -999f;
    public float maxX = 999f;
    public float minY = -999f;
    public float maxY = 999f;
    private Vector3 velocity;

    void LateUpdate()
    {
        if (!playerA || !playerB) return;
        Transform leader = (playerA.position.x >= playerB.position.x) ? playerA : playerB;
        Vector3 targetPos = leader.position + offset;

        if (lockY) targetPos.y = fixedY;
        if (useClamp)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
        }

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
    }
}
