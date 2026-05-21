using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// 既存シーン互換のためのスロットコンポーネント。
    /// 中身は WaitRoomPlayerInfoController をそのまま使う。
    /// </summary>
    [DisallowMultipleComponent]
    public class WaitRoomPlayerSlot : WaitRoomPlayerInfoController
    {
    }
}
