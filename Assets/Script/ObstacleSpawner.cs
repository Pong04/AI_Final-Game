using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public Transform player;
    public GameObject obstaclePrefab;

    public float spawnAheadDistance = 25f;
    public float firstObstacleX = 12f;

    public float minGap = 8f;
    public float maxGap = 14f;

    public float obstacleY = -2.5f;
    public float deleteBehindDistance = 15f;

    private float nextSpawnX;
    private readonly List<GameObject> obstacles =
        new List<GameObject>();

    private void Start()
    {
        nextSpawnX = player.position.x + firstObstacleX;
    }

    private void Update()
    {
        // 앞쪽 구간에 장애물을 미리 생성
        while (nextSpawnX <=
               player.position.x + spawnAheadDistance)
        {
            Vector3 position = new Vector3(
                nextSpawnX,
                obstacleY,
                0f
            );

            GameObject obstacle = Instantiate(
                obstaclePrefab,
                position,
                Quaternion.identity
            );

            obstacles.Add(obstacle);

            // 다음 장애물까지의 간격을 랜덤으로 결정
            nextSpawnX += Random.Range(
                Mathf.Max(1f, minGap),
                Mathf.Max(Mathf.Max(1f, minGap), maxGap)
            );
        }

        // 지나갔거나 부딪혀서 사라진 장애물 정리
        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            GameObject obstacle = obstacles[i];

            if (obstacle == null)
            {
                obstacles.RemoveAt(i);
                continue;
            }

            if (!obstacle.activeSelf ||
                obstacle.transform.position.x <
                player.position.x - deleteBehindDistance)
            {
                Destroy(obstacle);
                obstacles.RemoveAt(i);
            }
        }
    }
}