using UnityEngine;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "MasterData/Sound/MatchSoundMasterData")]
    public class MatchSoundMasterData : ScriptableObject
    {
        public AudioClip MatchSoundAudioClip(EMatchSound sound)
        {
            var soundMaster = SoundMasterData.Instance();
            if (soundMaster == null)
            {
                Debug.LogWarning($"[MatchSoundMasterData] SoundMasterData is not available for {sound}.");
                return null;
            }

            var clip = soundMaster.GetMatchSound(sound);
            if (clip == null)
            {
                Debug.LogWarning($"[MatchSoundMasterData] Match sound not found: {sound}");
            }

            return clip;
        }
    }
}
