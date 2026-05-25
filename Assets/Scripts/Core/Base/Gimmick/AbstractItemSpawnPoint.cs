using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;


namespace OpenGS
{


    public interface IItemSpawnPoint
    {

    }
    [DisallowMultipleComponent]
    public class AbstractItemSpawnPoint:MonoBehaviour,IItemSpawnPoint
    {
        //public SceneObject obj;

        [SerializeField]protected float heightOffset = 0.4f;


        public GameObject powerUpItemPrefab;
        public GameObject defenceUpItemPrefab;
        public GameObject speedUpItemPrefab;
        public GameObject stealthItemPrefab;
        public GameObject grenadePackItemPrefab;
        public GameObject healItemPrefab;
        public GameObject randomItemPrefab;

        private string path;

        public bool startImmidietry = true;

        public float firstTimeDelay = 27;
        public float generateInterval = 20;

        private float countdown = 0.0f;
        private Coroutine generateCoroutine;
        private bool isGenerating = false;

        protected eFieldItemType? beforeGeneratedItem=null;

        protected eFieldItemType? nextItem = null;

        IEnumerator OneSecCallback()
        {
            countdown = firstTimeDelay;

            while (true)
            {
                if (!isGenerating)
                {
                    yield break;
                }

                yield return new WaitForSecondsRealtime(1.0f);
                countdown -= 1.0f;
                if (countdown <= 0.0f)
                {
                    GenerateItem();
                    countdown = generateInterval;
                }

            }
        }

        void Start()
        {
            //path = gameObject.GetHierarchyPath();

            Debug.Log("path" + path);

            if(startImmidietry)
            {
                StartWorking();
            }

        }

        void OnEnable()
        {

        }

        public void StartWorking()
        {
            if (isGenerating)
            {
                return;
            }

            isGenerating = true;
            if (startImmidietry)
            {
                generateCoroutine = StartCoroutine(OneSecCallback());
            }
        }

        public virtual void GenerateItem()
        {
            countdown = generateInterval;
            TurnOffGenerate();
        }

        public void TurnOnGenerate()
        {
            if (!isGenerating)
            {
                StartWorking();
            }
        }

        public void TurnOffGenerate()
        {
            isGenerating = false;
            if (generateCoroutine != null)
            {
                StopCoroutine(generateCoroutine);
                generateCoroutine = null;
            }
        }

        public eFieldItemType? BeforeGeneratedItem()
        {

            return beforeGeneratedItem;
        }
        
        [Button("アイテム削除")]
        public void DeleteItem()
        {
            var item = gameObject.transform.GetChild(0);

            if (item)
            {
                Destroy(item.gameObject);
            }



        }
        

    }
}
