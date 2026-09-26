using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    [SerializeField] private bool activateOnStart = true;
    [SerializeField] private float destroyAfterSeconds = 0f;

    private void OnValidate()
    {
        if (!float.IsFinite(destroyAfterSeconds)) destroyAfterSeconds = 0f;
        destroyAfterSeconds = Mathf.Max(0f, destroyAfterSeconds);
    }

    private void Awake()
    {
        destroyAfterSeconds = float.IsFinite(destroyAfterSeconds)
            ? Mathf.Max(0f, destroyAfterSeconds)
            : 0f;
    }

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

    public void SetActiveState(bool active)
    {
        gameObject.SetActive(active);
    }
}
