

using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;
//using UnityEngine;

namespace OpenGS
{
    public interface IWaterFall
    {

    }
    [DisallowMultipleComponent]
    public class WaterFall: MonoBehaviour,IWaterFall
    {
        class PlayerData
        {
            public GameObject player;
            public float lastDamageTime;
        }

        public AudioClip hitSound;

        [Required]public float hitInterval = 1.0f;

        private readonly Dictionary<int, PlayerData> players = new Dictionary<int, PlayerData>();

        [Required] public PlayerEffectMasterData effectPrefabMasterData;
        [Required] public GameSoundMasterData masterdata;

        [SerializeField] private float damageAmount = 70f;

        private Coroutine damageCoroutine;
        private IEffectService effectService;

        [Inject]
        public void Construct([InjectOptional] IEffectService effectService)
        {
            this.effectService = effectService;
        }

        private void Awake()
        {
            // ensure collection initialized
            players.Clear();
        }

        private void OnEnable()
        {
            damageCoroutine = StartCoroutine(DamageLoop());
        }

        private void OnDisable()
        {
            if (damageCoroutine != null) StopCoroutine(damageCoroutine);
            damageCoroutine = null;
            players.Clear();
        }

        private IEnumerator DamageLoop()
        {
            var wait = new WaitForSecondsRealtime(0.1f);
            while (true)
            {
                var now = Time.time;
                var ids = new List<int>(players.Keys);
                foreach (var id in ids)
                {
                    if (!players.TryGetValue(id, out var pd)) continue;
                    if (pd == null || pd.player == null)
                    {
                        players.Remove(id);
                        continue;
                    }

                    if (now - pd.lastDamageTime >= hitInterval)
                    {
                        ApplyDamageTo(pd.player);
                        pd.lastDamageTime = now;
                    }
                }

                yield return wait;
            }
        }

        private void ApplyDamageTo(GameObject player)
        {
            if (player == null) return;

            // play effect
            try
            {
                if (effectService != null)
                {
                    effectService.PlayOneShotEffect(effectPrefabMasterData != null ? effectPrefabMasterData.HitEffect : null, player.transform.position, Quaternion.identity);
                }
                else if (effectPrefabMasterData != null && effectPrefabMasterData.HitEffect != null)
                {
                    var fx = Instantiate(effectPrefabMasterData.HitEffect);
                    fx.transform.position = player.transform.position;
                }
            }
            catch { }

            // play sound
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position);
            }

            // apply damage
            if (player.TryGetComponent<IDamageable>(out var dmg))
            {
                var dir = (player.transform.position - transform.position);
                dmg.AddDamage(new Vector2(dir.x, dir.y), damageAmount, eDamageType.WaterFall);
            }
        }

        private void RegisterPlayer(GameObject go)
        {
            if (go == null) return;
            if (!go.TryGetComponent<IMultipleTags>(out var tags)) return;
            if (!tags.HasPlayerTag()) return;

            var id = go.GetInstanceID();
            if (!players.ContainsKey(id))
            {
                // register and apply immediate damage on enter
                players[id] = new PlayerData { player = go, lastDamageTime = Time.time };
                ApplyDamageTo(go);
            }
        }

        private void UnregisterPlayer(GameObject go)
        {
            if (go == null) return;
            var id = go.GetInstanceID();
            players.Remove(id);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            RegisterPlayer(collision.gameObject);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            UnregisterPlayer(collision.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            RegisterPlayer(collision.gameObject);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            UnregisterPlayer(collision.gameObject);
        }

    }
}
