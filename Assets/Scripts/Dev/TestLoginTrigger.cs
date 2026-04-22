using UnityEngine;

namespace OpenGS
{
    // Development helper: press F5 to trigger login success, F6 for failure
    [DisallowMultipleComponent]
    public class TestLoginTrigger : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                Debug.Log("TestLoginTrigger: F5 pressed -> trigger success");
                try
                {
                    AuthenticationManager.Instance.DebugTriggerLoginResult(true);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("TestLoginTrigger: Failed to trigger AuthenticationManager: " + ex.Message);
                }
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                Debug.Log("TestLoginTrigger: F6 pressed -> trigger failure");
                try
                {
                    AuthenticationManager.Instance.DebugTriggerLoginResult(false);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("TestLoginTrigger: Failed to trigger AuthenticationManager: " + ex.Message);
                }
            }
        }
    }
}
