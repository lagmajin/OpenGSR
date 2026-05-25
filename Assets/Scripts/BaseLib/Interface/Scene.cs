using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    [SerializeField] private bool activateOnStart = true;
    [SerializeField] private float destroyAfterSeconds = 0f;

    private void Start()
    {
        if (!activateOnStart)
        {
            gameObject.SetActive(false);
        }

        if (destroyAfterSeconds > 0f)
        {
            Destroy(gameObject, destroyAfterSeconds);
        }
    }

    private void Update()
    {
    }

    public void SetActiveState(bool active)
    {
        gameObject.SetActive(active);
    }
}
