using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace OpenGS
{
    public interface IPlayerEffect
    {
        void CreateHitEffect();
    }

    [DisallowMultipleComponent]
    public class PlayerHitEffectController : MonoBehaviour, IPlayerEffect
    {
        [SerializeField] [Required] public AbstractPlayer player;
        [SerializeField] [Required] [SceneObjectsOnly] public Transform transforom;
        [SerializeField] [Required] private GameObject hitEffect;
        private IEffectService effectService;

        [Inject]
        private void Construct([InjectOptional] IEffectService effectService)
        {
            this.effectService = effectService;
        }

        [Button("Effect Test")]
        public void CreateHitEffect()
        {
            if (!hitEffect || player == null)
            {
                return;
            }

            if (effectService != null)
            {
                effectService.PlayOneShotEffect(hitEffect, player.transform.position, Quaternion.identity);
                return;
            }

            var effect = Instantiate(hitEffect);
            effect.transform.position = player.transform.position;
        }
    }
}
