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
        public GameObject randomItemPrefab;

        private string path;

        public bool startImmidietry = true;

        public float firstTimeDelay = 27;
        public float generateInterval = 20;

        private float countdownInterval = 0.5f;

        private float countdown = 0.0f;

        protected eFieldItemType? beforeGeneratedItem=null;

        protected eFieldItemType? nextItem = null;

        IEnumerator OneSecCallback()
        {
            countdown = firstTimeDelay;

            
            while (true)
            {
                //yield return new WaitForSecondsRealtime(0.5f);
                //countdown -= 1.0f;
                //if (countdown <= 0&& gameObject.HasChild())
                {


                   // GenerateItem();
                }

            

            }

            
        }

        void Start()
        {
            //path = gameObject.GetHierarchyPath();

            Debug.Log("path" + path);

            if(startImmidietry)
            {

            }

        }

        void OnEnable()
        {

        }

        public void StartWorking()
        {
            if (startImmidietry)
            {
                StartCoroutine(OneSecCallback());
            }
        }

        public virtual void GenerateItem()
        {

            TurnOffGenerate();
        }

        public void TurnOnGenerate()
        {

        }

        public void TurnOffGenerate()
        {

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
