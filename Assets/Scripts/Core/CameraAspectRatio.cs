using UnityEngine;


namespace OpenGS
{
    public class CameraAspectRatio : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Camera.main.aspect = (float)Screen.width / (float)Screen.height;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }


}