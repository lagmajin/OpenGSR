using System.Collections;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
   
    internal interface IAbstractPlayerAgent
    {

    }

    [DisallowMultipleComponent]
    public class AbstractPlayerAgent : MonoBehaviour, OpenGS.Network.INetworkTransform
    {
        [SerializeField] protected Transform playerTransform;
        private Rigidbody2D body;
        
        private string playerID = string.Empty;
        private EPlayerType playerType;

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        public uint NetworkId => StableNetworkId(OwnerPlayerId);
        public Vector3 Position
        {
            get
            {
                var position = ResolveTransform().position;
                return IsFinite(position) ? position : Vector3.zero;
            }
            set
            {
                if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                {
                    return;
                }

                body ??= GetComponent<Rigidbody2D>();
                if (body != null)
                {
                    body.position = value;
                }
                else
                {
                    ResolveTransform().position = value;
                }
            }
        }
        public Quaternion Rotation
        {
            get => ResolveTransform().rotation;
            set
            {
                if (!float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                    !float.IsFinite(value.z) || !float.IsFinite(value.w))
                {
                    return;
                }

                if (OpenGS.Network.QuaternionMath.SqrMagnitude(value) < 0.000001f)
                {
                    return;
                }

                ResolveTransform().rotation = value.normalized;
            }
        }
        public Vector3 Velocity
        {
            get
            {
                body ??= GetComponent<Rigidbody2D>();
                if (body == null) return Vector3.zero;
                var velocity = (Vector3)body.linearVelocity;
                return IsFinite(velocity) ? velocity : Vector3.zero;
            }
        }
        public string OwnerPlayerId => playerID;

        public void SetPlayerID(PlayerID id)
        {
            SetPlayerID(id.ToString());
        }

        public void SetPlayerID(string id)
        {
            var previousId = playerID;
            playerID = id ?? string.Empty;

            if (OpenGS.Network.LagCompensationManager.Instance != null)
            {
                OpenGS.Network.LagCompensationManager.Instance.UnregisterNetworkObject(previousId);
                OpenGS.Network.LagCompensationManager.Instance.RegisterNetworkObject(this);
            }
        }

        public EPlayerType PlayerType()
        {
            return playerType;
        }

        public void SetPlayerType(EPlayerType type = EPlayerType.Unknown)
        {
            playerType = type;

        }

        private Transform ResolveTransform()
        {
            return playerTransform != null ? playerTransform : transform;
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }

        private static uint StableNetworkId(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                if (string.IsNullOrEmpty(value)) return 0u;
                foreach (var character in value)
                {
                    hash ^= char.ToUpperInvariant(character);
                    hash *= 16777619u;
                }
                return hash;
            }
        }

        protected virtual void OnEnable()
        {
            OpenGS.Network.LagCompensationManager.Instance?.RegisterNetworkObject(this);
        }

        protected virtual void OnDisable()
        {
            OpenGS.Network.LagCompensationManager.Instance?.UnregisterNetworkObject(OwnerPlayerId);
        }

        protected virtual void OnDropWeapon()
        {
        }



    }
}
