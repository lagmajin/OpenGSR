using System.Collections;
using UnityEngine;

namespace OpenGS
{

    // eFieldItemType enum moved to Interface/eFieldItemType.cs

    public interface IWorldItem
    {
        //string path();
    }

    [System.Obsolete("Use IWorldItem instead.")]
    public interface IFieldItem : IWorldItem
    {
    }

    [DisallowMultipleComponent]
    public class WorldItem : MonoBehaviour, IWorldItem
    {

        public GameObject fieldItemEffect;
        public bool takable = false;
        [SerializeField,Range(1f,40f)]
        public float activeTime = 27.0f;

        public AbstractItemSpawnPoint point;

        private Coroutine lifetimeCoroutine;

        private void Awake()
        {
            activeTime = float.IsFinite(activeTime) ? Mathf.Clamp(activeTime, 1f, 40f) : 27f;
        }

        protected IEnumerator DelayCoroutine(float activeTime=10.0f)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, activeTime));
            lifetimeCoroutine = null;
            Destroy(gameObject);
        }
    
        void Start()
        {
            EnableActiveTime();
        }

        protected virtual void OnEnable()
        {
            if (lifetimeCoroutine == null)
            {
                EnableActiveTime();
            }
        }

        protected virtual void OnDisable()
        {
            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
                lifetimeCoroutine = null;
            }
        }

        public void SetActiveTime()
        {
            EnableActiveTime();
        }

        public void EnableActiveTime()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
            }

            lifetimeCoroutine = StartCoroutine(DelayCoroutine(activeTime));
        }


        public void SetTakable(bool b=true)
        {
            takable = b;
        }

        /*
        public string Path()
        {



            return gameObject.GetHierarchyPath();
        }

        */
    }

    [System.Obsolete("Use WorldItem instead.")]
    public abstract class AbstractFieldItem : WorldItem
    {
    }
}
