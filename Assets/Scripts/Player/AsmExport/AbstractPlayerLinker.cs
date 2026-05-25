using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class AbstractPlayerLinker : MonoBehaviour
    {
        [SerializeField] private string id;
        [SerializeField] private AbstractPlayer player;

        public string PlayerId => id;
        public AbstractPlayer Player => player;

        protected virtual void Awake()
        {
            if (player == null)
            {
                player = GetComponent<AbstractPlayer>();
            }
        }

        public void SetPlayerId(string value)
        {
            id = value ?? string.Empty;
        }

        public void SetPlayer(AbstractPlayer value)
        {
            player = value;
        }
    }
}
