//using KanKikuchi.AudioManager;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public abstract class AbstractBulletAgent : MonoBehaviour
    {
        [ShowInInspector]public float Damage=0;

        [SerializeField]private AudioClip[] hitObjectSounds;
        [SerializeField] AudioSource audioSource;
        [SerializeField]protected GameObject hitEffect;
        public abstract void Launch(Vector2 direction, float speed, float damage=0);
        // Use this for initialization
        public void PlaySound(ESoundEffect effect)
        {
            switch (effect)
            {
                case ESoundEffect.HitStageObject:  // ← enum名をフルで書く
                    if (hitObjectSounds.Length > 0)
                    {
                        var clip = hitObjectSounds[Random.Range(0, hitObjectSounds.Length)];
                        global::OpenGS.PlaySound.PlaySE(clip);

                        //audioSource.PlayOneShot(clip);
                    }
                    break;  // ← 他のcase追加しやすいようにbreakを忘れずに
            }
        }

    }
}
