using UnityEngine;

public class RunnerCamera : MonoBehaviour
{
    public Transform player;
    public float offsetX = 4f;

    private void LateUpdate()
    {
        if (player == null)
            return;

        Vector3 position = transform.position;

        // 가로로만 따라가고 높이는 유지
        position.x = player.position.x + offsetX;

        transform.position = position;
    }
}