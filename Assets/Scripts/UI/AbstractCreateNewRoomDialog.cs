using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Base MonoBehaviour for create-new-room dialogs.
    /// Subclass this to implement the dialog, or assign any MonoBehaviour
    /// that implements ICreateNewRoomDialog via the Inspector.
    /// </summary>
    public abstract class AbstractCreateNewRoomDialog : MonoBehaviour, ICreateNewRoomDialog
    {
        public abstract string RoomName();
        public abstract int MaxPlayer();
        public abstract string Password();
        public abstract EGameMode GameMode();
        public abstract bool TeamBalance();
        public abstract void ShowDialog();
    }
}
