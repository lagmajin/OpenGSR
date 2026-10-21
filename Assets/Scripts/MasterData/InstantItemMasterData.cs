
using UnityEngine.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using OpenGSCore;



namespace OpenGS
{

    [CreateAssetMenu(menuName = "Item/InstantItem")]
    public class InstantItemMasterData : ScriptableObject
    {
        [SerializeField] private EInstantItemType type;

        [SerializeField] private Image img;

        public EInstantItemType ItemType => type;

        /// <summary>
        /// Fills the icon in from the item type.
        /// <para>
        /// The type is the only field that is actually authored, so the icon was
        /// set by hand on every asset and any two assets could disagree with the
        /// type they carry. The lookup already exists for the UI, so this uses it
        /// rather than growing a second mapping here.
        /// </para>
        /// </summary>
        [Button("自動セット")]
        void AutoSet()
        {
            if (img == null)
            {
                Debug.LogWarning($"[InstantItemMasterData] {name} has no Image assigned; nothing to set.", this);
                return;
            }

            var icon = InstantItemVisualResolver.GetIcon(type);
            if (icon == null)
            {
                Debug.LogWarning($"[InstantItemMasterData] {name}: no icon is registered for {type}.", this);
                return;
            }

            img.sprite = icon;
            Debug.Log($"[InstantItemMasterData] {name}: icon set for {type}.", this);
        }
    }
}
