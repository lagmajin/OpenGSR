using UnityEngine;
using System;
using DG.Tweening;
using Zenject;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace OpenGS
{
    public interface IEffectService
    {
        void PlayImpactEffect(Vector2 position, Vector2 normal);
        void PlayOneShotEffect(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = -1f);
        void ShakeCamera(float intensity, float duration);
    }

    /// <summary>
    /// エフェクトやカメラ演出を管理するサービス。
    /// 旧プロジェクトの BulletImpactEffect 等の機能を統合・洗練。
    /// </summary>
    public class EffectService : IEffectService
    {
        private const string PoolRootName = "[EffectService Pool]";
        private const float DefaultHitLifetime = 0.2f;
        private const int DefaultPoolCapacity = 8;
        private const int MaxPoolSize = 32;

        private readonly EffectPrefabMasterData _prefabs;
        private Camera _mainCamera;
        private ObjectPool<GameObject> _hitEffectPool;
        private Transform _poolRoot;

        public EffectService([InjectOptional] EffectPrefabMasterData prefabs)
        {
            _prefabs = prefabs != null ? prefabs : Resources.Load<EffectPrefabMasterData>("MasterData/Effect/EffectPrefab");
            _mainCamera = Camera.main;
        }

        public void PlayImpactEffect(Vector2 position, Vector2 normal)
        {
            if (_prefabs == null || _prefabs.HitEffect == null) return;

            var effect = GetHitEffect();
            if (effect == null)
            {
                return;
            }

            var rotation = CreateImpactRotation(normal);
            effect.transform.SetPositionAndRotation(position, rotation);

            if (effect.TryGetComponent(out BulletImpactEffect impactEffect))
            {
                impactEffect.Play(DefaultHitLifetime, ReleaseHitEffect);
            }
            else
            {
                Object.Destroy(effect, DefaultHitLifetime);
            }
        }

        public void PlayOneShotEffect(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = -1f)
        {
            if (prefab == null)
            {
                return;
            }

            if (_prefabs != null && _prefabs.HitEffect == prefab)
            {
                PlayImpactEffect(position, Vector2.up);
                return;
            }

            var effect = Object.Instantiate(prefab, position, rotation);
            if (lifetime > 0f)
            {
                Object.Destroy(effect, lifetime);
            }
        }

        public void ShakeCamera(float intensity, float duration)
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            _mainCamera.transform.DOShakePosition(duration, intensity, 10, 90, false, true);
        }

        private Quaternion CreateImpactRotation(Vector2 normal)
        {
            var direction = normal.sqrMagnitude > 0f ? (Vector3)normal.normalized : Vector3.up;
            return Quaternion.FromToRotation(Vector3.up, direction);
        }

        private GameObject GetHitEffect()
        {
            EnsureHitEffectPool();
            return _hitEffectPool != null ? _hitEffectPool.Get() : null;
        }

        private void ReleaseHitEffect(GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            if (_hitEffectPool != null)
            {
                _hitEffectPool.Release(effect);
                return;
            }

            effect.SetActive(false);
            Object.Destroy(effect);
        }

        private void EnsureHitEffectPool()
        {
            if (_hitEffectPool != null || _prefabs == null || _prefabs.HitEffect == null)
            {
                return;
            }

            _hitEffectPool = new ObjectPool<GameObject>(
                CreateHitEffect,
                OnGetHitEffect,
                OnReleaseHitEffect,
                OnDestroyHitEffect,
                collectionCheck: false,
                DefaultPoolCapacity,
                MaxPoolSize);
        }

        private GameObject CreateHitEffect()
        {
            EnsurePoolRoot();
            var effect = Object.Instantiate(_prefabs.HitEffect, _poolRoot);
            effect.name = _prefabs.HitEffect.name;
            effect.SetActive(false);
            return effect;
        }

        private void OnGetHitEffect(GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            effect.SetActive(true);
        }

        private void OnReleaseHitEffect(GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            effect.transform.SetParent(_poolRoot, false);
            effect.SetActive(false);
        }

        private void OnDestroyHitEffect(GameObject effect)
        {
            if (effect != null)
            {
                Object.Destroy(effect);
            }
        }

        private void EnsurePoolRoot()
        {
            if (_poolRoot != null)
            {
                return;
            }

            var root = new GameObject(PoolRootName);
            root.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(root);
            _poolRoot = root.transform;
        }
    }
}
