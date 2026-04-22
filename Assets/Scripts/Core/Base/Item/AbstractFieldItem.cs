using System.Collections;
using UnityEngine;

namespace OpenGS
{

    // eFieldItemType enum moved to Interface/eFieldItemType.cs

    public interface IFieldItem
    {
        //string path();
    }

    [DisallowMultipleComponent]
    public class AbstractFieldItem : MonoBehaviour,IFieldItem
    {

        public GameObject fieldItemEffect;
        public bool takable = false;
        [SerializeField,Range(1f,40f)]
        public float activeTime = 27.0f;

        public AbstractItemSpawnPoint point;

        protected IEnumerator DelayCoroutine(float activeTime=10.0f)
        {
            transform.position = Vector3.one;

            // 3秒間待つ
            yield return new WaitForSecondsRealtime(activeTime);

            Destroy(gameObject);
            
        }
    
        void Start()
        {

            StartCoroutine(DelayCoroutine(activeTime));
        }

        public void SetActiveTime()
        {

        }

        public void EnableActiveTime()
        {

        }


        public void SetTakable(bool b=true)
        {

        }

        /*
        public string Path()
        {



            return gameObject.GetHierarchyPath();
        }

        */
    }
}