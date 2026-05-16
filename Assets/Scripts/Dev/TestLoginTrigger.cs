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
                var loginScene = FindFirstObjectByType<LoginAndSignUpScene>();
                if (loginScene != null)
                {
                    Debug.Log("TestLoginTrigger: F5 pressed -> triggering login scene TryLogin()");
                    loginScene.TryLogin();
                }
                else
                {
                    Debug.LogWarning("TestLoginTrigger: LoginAndSignUpScene not found");
                }
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                Debug.Log("TestLoginTrigger: F6 pressed -> no AuthenticationManager available in current codebase");
            }
        }
    }
}
