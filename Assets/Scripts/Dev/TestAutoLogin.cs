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
            Debug.Log("TestAutoLogin: Attempting AuthenticationManager.TryLogin('test','test')");
            try
            {
                AuthenticationManager.Instance.TryLogin("test", "test", "");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("TestAutoLogin: failed to call TryLogin: " + ex.Message);
            }
        }
    }
}
