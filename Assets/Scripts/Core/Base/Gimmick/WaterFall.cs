

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

        private void OnValidate()
        {
            if (!float.IsFinite(hitInterval)) hitInterval = 1f;
            if (!float.IsFinite(damageAmount)) damageAmount = 70f;
            hitInterval = Mathf.Max(0.05f, hitInterval);
            damageAmount = Mathf.Max(0f, damageAmount);
        }

        [Inject]
        public void Construct([InjectOptional] IEffectService effectService)
        {
            this.effectService = effectService;
        }

        private void Awake()
        {
            hitInterval = float.IsFinite(hitInterval) ? Mathf.Max(0.05f, hitInterval) : 1f;
            damageAmount = float.IsFinite(damageAmount) ? Mathf.Max(0f, damageAmount) : 70f;
            // ensure collection initialized
            players.Clear();
        }

        private void OnEnable()
        {
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
            }
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
                if (!float.IsFinite(now) || now < 0f)
                {
                    yield return wait;
                    continue;
                }
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
                    Destroy(fx, 5f);
                }
            }
            catch { }

            // play sound
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position);
            }

            // apply damage
            var abstractPlayer = player.GetComponentInParent<AbstractPlayer>();
            if (abstractPlayer != null && PlayerRegistry.Instance != null)
            {
                var source = (Vector2)(abstractPlayer.transform.position - transform.position);
                PlayerRegistry.Instance.ApplyDamage(
                    abstractPlayer.UniqueID(),
                    source,
                    damageAmount,
                    eDamageType.WaterFall,
                    string.Empty,
                    nameof(WaterFall),
                    false);
                return;
            }

            var dmg = player.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                var dir = (player.transform.position - transform.position);
                dmg.AddDamage(new Vector2(dir.x, dir.y), damageAmount, eDamageType.WaterFall);
            }
        }

        private void RegisterPlayer(GameObject go)
        {
            if (go == null) return;
            var tags = go.GetComponentInParent<IMultipleTags>();
            if (tags == null) return;
            if (!tags.HasPlayerTag()) return;

            var abstractPlayer = go.GetComponentInParent<AbstractPlayer>();
            if (abstractPlayer != null)
            {
                go = abstractPlayer.gameObject;
            }
            else
            {
                var playerAgent = go.GetComponentInParent<PlayerAgent>();
                if (playerAgent != null)
                {
                    go = playerAgent.gameObject;
                }
            }

            var id = UnityObjectIdCompat.GetObjectId(go);
            if (!players.ContainsKey(id))
            {
                // register and apply immediate damage on enter
                var now = Time.time;
                players[id] = new PlayerData
                {
                    player = go,
                    lastDamageTime = float.IsFinite(now) && now >= 0f ? now : 0f
                };
                ApplyDamageTo(go);
            }
        }

        private void UnregisterPlayer(GameObject go)
        {
            if (go == null) return;
            var abstractPlayer = go.GetComponentInParent<AbstractPlayer>();
            if (abstractPlayer != null)
            {
                go = abstractPlayer.gameObject;
            }
            else
            {
                var playerAgent = go.GetComponentInParent<PlayerAgent>();
                if (playerAgent != null)
                {
                    go = playerAgent.gameObject;
                }
            }

            var id = UnityObjectIdCompat.GetObjectId(go);
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
