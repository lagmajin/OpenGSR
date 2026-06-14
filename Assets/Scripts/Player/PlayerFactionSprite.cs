using System;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public sealed class PlayerFactionSprite : MonoBehaviour
    {
        [SerializeField] private AbstractPlayer targetPlayer;
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private Sprite soloSprite;
        [SerializeField] private Sprite redSprite;
        [SerializeField] private Sprite blueSprite;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.6f, 0f);

        private void Awake()
        {
            ResolveReferences();
            ApplyCurrentState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ApplyCurrentState();
        }

        private void LateUpdate()
        {
            if (targetPlayer == null || iconRenderer == null)
            {
                ResolveReferences();
            }

            if (targetPlayer == null || iconRenderer == null)
            {
                return;
            }

            transform.position = targetPlayer.transform.position + worldOffset;

            var cam = Camera.main;
            if (cam != null)
            {
                transform.forward = cam.transform.forward;
            }

            ApplyCurrentState();
        }

        public void SetTarget(AbstractPlayer player)
        {
            targetPlayer = player;
            ApplyCurrentState();
        }

        public void SetSprites(Sprite solo, Sprite red, Sprite blue)
        {
            soloSprite = solo;
            redSprite = red;
            blueSprite = blue;
            ApplyCurrentState();
        }

        public void SetIconRenderer(SpriteRenderer renderer)
        {
            iconRenderer = renderer;
            ApplyCurrentState();
        }

        private void ResolveReferences()
        {
            if (targetPlayer == null)
            {
                targetPlayer = GetComponentInParent<AbstractPlayer>();
            }

            if (iconRenderer == null)
            {
                iconRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        private void ApplyCurrentState()
        {
            if (targetPlayer == null || iconRenderer == null)
            {
                return;
            }

            var sprite = ResolveSprite();
            if (sprite != null)
            {
                iconRenderer.sprite = sprite;
                iconRenderer.enabled = true;
            }
            else
            {
                iconRenderer.enabled = false;
            }
        }

        private Sprite ResolveSprite()
        {
            var team = targetPlayer.Team();
            if (team == ETeam.Red)
            {
                return redSprite != null ? redSprite : soloSprite;
            }

            if (team == ETeam.Blue)
            {
                return blueSprite != null ? blueSprite : soloSprite;
            }

            return soloSprite;
        }
    }
}
