using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// 全ての MediateObject の基底クラス。
    /// AbstractScene のインスペクターで型制限を行うために使用。
    /// </summary>
    public abstract class AbstractMediateObject : MonoBehaviour, IAbstractMediateObject
    {
    }
}
