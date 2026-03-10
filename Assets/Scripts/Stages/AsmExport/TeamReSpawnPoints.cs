using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    public interface ITeamRespawnPoints
    {

        Vector2 RandomBlueTeam();
        Vector2 RandomRedTeam();


    }
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class TeamReSpawnPoints : MonoBehaviour, ITeamRespawnPoints
    {
        public List<GameObject> BlueTeamPoints;
        public List<GameObject> RedTeamPoints;

        private void Start()
        {

        }

        public Vector2 RandomBlueTeam()
        {
            var result = new Vector2();

            var rand = Random.Range(0, BlueTeamPoints.Count);

            return result;
        }

        public Vector2 RandomRedTeam()
        {
            var result = new Vector2();

            var rand = Random.Range(0, RedTeamPoints.Count);

            return result;
        }

        public List<string> ReadTeamRespawn()
        {
            List<string> result = new List<string>();

            return result;
        }
    }


}
