using UnityEngine;
using UnityEngine.EventSystems;
using OpenGSR.Audio;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class TitleSceneMediateObject : AbstractMediateObject
    {
        // Scene YAML との互換性維持のため、既存の field 名をそのまま残す。
        [SerializeField] private EventSystem system;
        [SerializeField] private SimpleAudioManager soundManager;
        [SerializeField] private GeneralSceneMasterData generalSceneMasterData;
        [SerializeField] private bool auto;
        [SerializeField] private TitleScene titleSceneMainScript;

        public EventSystem System => system;
        public SimpleAudioManager SoundManager => soundManager;
        public GeneralSceneMasterData GeneralSceneMasterData => generalSceneMasterData;
        public bool Auto => auto;
        public TitleScene TitleSceneMainScript => titleSceneMainScript;
    }
}
