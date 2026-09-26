using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class ReSpawnPoints : MonoBehaviour, IReSpawnPoints
    {
        [SerializeField] public List<GameObject> Points;
        [SerializeField] public bool dontUseBeforePoint = true;
        public int i = 2;
        private Object lastIndex;

        public Vector2 GetRandomSpawnPoint(ETeam team = ETeam.NoTeam) => random();
        public int Count(ETeam team = ETeam.NoTeam) => Count();

        public Vector2 random()
        {
            if (Points == null || Points.Count == 0)
            {
                return Vector2.zero;
            }

            var validPoints = new List<GameObject>();
            foreach (var point in Points)
            {
                if (point != null)
                {
                    validPoints.Add(point);
                }
            }

            if (validPoints.Count == 0)
            {
                return Vector2.zero;
            }

            return validPoints[Random.Range(0, validPoints.Count)].transform.position;
        }

        public int Count() => Points != null ? Points.Count : 0;
        public void PrintInfo()
        {
            var total = Points != null ? Points.Count : 0;
            var valid = 0;
            if (Points != null)
            {
                foreach (var point in Points)
                {
                    if (point != null)
                    {
                        valid++;
                    }
                }
            }

            Debug.Log($"[ReSpawnPoints] {name}: {valid}/{total} valid spawn points");
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Points == null) return;

            var style = new GUIStyle();
            style.normal.textColor = Color.yellow;
            Gizmos.color = Color.red;

            foreach (var point in Points)
            {
                if (point == null) continue;
                var pos = point.transform.position;
                UnityEditor.Handles.Label(pos + Vector3.up * 0.2f, point.name, style);
                Gizmos.DrawSphere(pos, 0.1f);
            }
        }
#endif
    }
}
