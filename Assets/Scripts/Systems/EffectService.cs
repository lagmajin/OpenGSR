using UnityEngine;
using DG.Tweening;
using Zenject;

namespace OpenGS
{
    public interface IEffectService
    {
        void PlayImpactEffect(Vector2 position, Vector2 normal);
        void ShakeCamera(float intensity, float duration);
    }

    /// <summary>
    /// エフェクトやカメラ演出を管理するサービス。
    /// 旧プロジェクトの BulletImpactEffect 等の機能を統合・洗練。
    /// </summary>
    public class EffectService : IEffectService
    {
        private readonly EffectPrefabMasterData _prefabs;
        private Camera _mainCamera;

        public EffectService(EffectPrefabMasterData prefabs)
        {
            _prefabs = prefabs;
            _mainCamera = Camera.main;
        }

        public void PlayImpactEffect(Vector2 position, Vector2 normal)
        {
            if (_prefabs == null || _prefabs.HitEffect == null) return;

            // TODO: オブジェクトプールを使用するように改善
            var effect = Object.Instantiate(_prefabs.HitEffect, position, Quaternion.LookRotation(Vector3.forward, normal));
            Object.Destroy(effect, 1.0f);
        }

        public void ShakeCamera(float intensity, float duration)
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            _mainCamera.transform.DOShakePosition(duration, intensity, 10, 90, false, true);
        }
    }
}
