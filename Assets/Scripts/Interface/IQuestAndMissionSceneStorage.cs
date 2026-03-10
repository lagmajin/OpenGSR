namespace OpenGS
{
    /// <summary>
    /// クエスト・ミッションシーンのシーンオブジェクトを提供するインターフェース
    /// </summary>
    public interface IQuestAndMissionSceneStorage
    {
        SceneObject Mission1Scene();
        SceneObject Mission2Scene();
        SceneObject Mission3Scene();
        SceneObject Mission4Scene();
        SceneObject Mission5Scene();

        SceneObject Quest1Scene();
        SceneObject Quest2Scene();
        SceneObject Quest3Scene();

        SceneObject MissionResultScene();
    }
}
