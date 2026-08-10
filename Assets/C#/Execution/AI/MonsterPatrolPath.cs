using System.Collections.Generic;
using UnityEngine;

public class MonsterPatrolPath : MonoBehaviour
{
    [SerializeField] private List<Transform> points = new List<Transform>();
    [SerializeField] private bool loop = true;

    public bool Loop => loop;
    public int Count => points != null ? points.Count : 0;

    /// <summary>编辑器工具使用：追加路径点。</summary>
    public void AddPoint(Transform point)
    {
        if (points == null)
        {
            points = new List<Transform>();
        }

        if (point != null)
        {
            points.Add(point);
        }
    }

    public Transform GetPoint(int index)
    {
        if (points == null || points.Count == 0)
        {
            return null;
        }

        if (loop)
        {
            int wrapped = ((index % points.Count) + points.Count) % points.Count;
            return points[wrapped];
        }

        if (index < 0 || index >= points.Count)
        {
            return null;
        }

        return points[index];
    }

    public int GetNearestPointIndex(Vector3 position)
    {
        if (points == null || points.Count == 0)
        {
            return -1;
        }

        int nearest = -1;
        float nearestSqr = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            Transform point = points[i];
            if (point == null)
            {
                continue;
            }

            float sqr = (point.position - position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearest = i;
                nearestSqr = sqr;
            }
        }

        return nearest;
    }

    private void OnDrawGizmos()
    {
        if (points == null || points.Count == 0)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        for (int i = 0; i < points.Count; i++)
        {
            Transform current = points[i];
            if (current == null)
            {
                continue;
            }

            Gizmos.DrawSphere(current.position, 0.12f);

            Transform next = GetPoint(i + 1);
            if (next != null && (loop || i + 1 < points.Count))
            {
                Gizmos.DrawLine(current.position, next.position);
            }
        }
    }
}
