using System.Collections.Generic;
using UnityEditor;
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
        //public GameObject blutTeamPoints;
        //public GameObject redTeamPoints;

        public Vector2 random()
        {
            var count = Points.Count;

            if (count == 0)
            {
                return new Vector2();
            }



            var rand = Random.Range(0, count);


            return Points[0].transform.position;
        }

        public int Count()
        {
            return Points.Count;
        }

        public void PrintInfo()
        {

        }
        private void OnDrawGizmos()
        {
            if (Points == null) return;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.yellow;

            Gizmos.color = Color.red;

            foreach (var point in Points)
            {
                if (point != null)
                {
                    var pos = point.transform.position;
                    Handles.Label(pos + Vector3.up * 0.2f, point.name, style);
                    Gizmos.DrawSphere(pos, 0.1f);  // ← 印としての球
                }
            }
        }
    }
}



