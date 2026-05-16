using System.Collections;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class TestAutoLogin : MonoBehaviour
    {
        // Auto attempt login shortly after Play to test server flow
        IEnumerator Start()
        {
            yield return new WaitForSeconds(0.5f);
            var loginScene = FindFirstObjectByType<LoginAndSignUpScene>();
            if (loginScene != null)
            {
                Debug.Log("TestAutoLogin: triggering LoginAndSignUpScene.TryLogin()");
                loginScene.TryLogin();
            }
            else
            {
                Debug.LogWarning("TestAutoLogin: LoginAndSignUpScene not found");
            }
        }
    }
}
