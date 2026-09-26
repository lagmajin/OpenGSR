using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class DamageCanvas : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject comboTextPrefab;
        [SerializeField] private GameObject damageTextPrefab;
        [SerializeField] private GameObject misty;

        private void OnValidate()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }
        }

        void Start()
        {
            AutoSet();
        }

        void Reset()
        {
            canvas = GetComponent<Canvas>();
        }

        void Set()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            if (canvas != null)
            {
                canvas.enabled = true;
            }
        }

        public void AutoSet()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            if (misty == null)
            {
                foreach (var player in FindObjectsByType<AbstractPlayer>(FindObjectsSortMode.None))
                {
                    if (player != null && player.PlayerType() == EPlayerType.MyPlayer)
                    {
                        misty = player.gameObject;
                        break;
                    }
                }
            }

            if (comboTextPrefab == null)
            {
                comboTextPrefab = damageTextPrefab;
            }

            Set();
        }

        public void ShowDamage()
        {
            var target = misty != null ? misty.transform : transform;
            ShowDamageText(target, 1.0f);
        }

        public void ShowDamageText(Transform target, float time = 1.0f)
        {
            SpawnFloatingText(damageTextPrefab != null ? damageTextPrefab : comboTextPrefab, target, time, "Damage");
        }

        public void ShowComboText(Transform target, float time = 1.0f)
        {
            SpawnFloatingText(comboTextPrefab != null ? comboTextPrefab : damageTextPrefab, target, time, "Combo");
        }

        [Button("Show Damage Text")]
        public void ShowDamageText()
        {
            ShowDamageText(misty != null ? misty.transform : transform, 1.0f);
        }

        [Button("Show Combo Text")]
        public void ShowComboText()
        {
            ShowComboText(misty != null ? misty.transform : transform, 1.0f);
        }

        private void SpawnFloatingText(GameObject prefab, Transform target, float time, string label)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[DamageCanvas] {label} prefab is not assigned.");
                return;
            }

            time = float.IsFinite(time) ? Mathf.Max(0.1f, time) : 1f;
            var parent = canvas != null ? canvas.transform : transform;
            var obj = Instantiate(prefab, parent, false);
            if (target != null)
            {
                obj.transform.position = target.position;
            }

            Destroy(obj, time);
        }
    }
}
