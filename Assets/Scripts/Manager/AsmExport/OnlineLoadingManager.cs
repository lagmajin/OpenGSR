
using System.Collections.Generic;

using OpenGSCore;


namespace OpenGS
{

    public class LoadingInfo
    {
        public string MapName { get; set; }
        public EGameMode GameMode { get; set; }
    }


    public class OnlineLoadingManager
    {
        public static OnlineLoadingManager Instance { get; } = new OnlineLoadingManager();

        private Dictionary<string,LoadingGauge> gaugeList=new();

        public LoadingInfo LoadingInfo { get; set; } = new();

        //private List<LoadingSceneUIManagerProvider> subscriber = new();

        private string loadingMessage = "";
        public OnlineLoadingManager()
        {


        }

        public void AddLoadingPlayer(in string id)
        {
            if (!gaugeList.ContainsKey(id))
            {

            }
            else
            {
                gaugeList.Add(id,new LoadingGauge());
            }

        }

        public void UpdateLoading(in string id,float gauge)
        {
            gaugeList[id].Gauge=gauge;

        }

        public Gauge GetGauge(in string id)
        {
            return null;
        }

        void Clear()
        {

        }

        public void SetLoadingMessage(in string message)
        {
            loadingMessage = message;
        }



    }




}

