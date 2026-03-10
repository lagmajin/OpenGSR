using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// クエスト・ミッションシーンへの参照を保持する MonoBehaviour。
    /// Inspector からシーンオブジェクトをアサインして使用する。
    /// </summary>
    [DisallowMultipleComponent]
    public class QuestAndMissionSceneStorage : MonoBehaviour, IQuestAndMissionSceneStorage
    {
        [SerializeField] private SceneObject test;

        [Header("ミッションシーン")]
        [SerializeField] private SceneObject mission1;
        [SerializeField] private SceneObject mission2;
        [SerializeField] private SceneObject mission3;
        [SerializeField] private SceneObject mission4;
        [SerializeField] private SceneObject mission5;
        [SerializeField] private SceneObject missionResultScene;

        [Header("クエストシーン")]
        [SerializeField] private SceneObject quest1;
        [SerializeField] private SceneObject quest2;
        [SerializeField] private SceneObject quest3;

        public SceneObject Mission1Scene() => mission1;
        public SceneObject Mission2Scene() => mission2;
        public SceneObject Mission3Scene() => mission3;
        public SceneObject Mission4Scene() => mission4;
        public SceneObject Mission5Scene() => mission5;

        public SceneObject Quest1Scene() => quest1;
        public SceneObject Quest2Scene() => quest2;
        public SceneObject Quest3Scene() => quest3;

        public SceneObject MissionResultScene() => missionResultScene;
    }
}
