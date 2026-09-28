using System;





namespace OpenGSCore
{
    public enum EGrenadeType
    {
        Empty,
        Normal,
        Power,
        Magnetic,
        Mine,
        Cluster,
        ClusterChild,
        Fire,
        Smoke,
    }



    public class PowerGrenade : AbstractGrenade
    {

        public PowerGrenade()
        {


        }

        public override void Hit()
        {
            // _powergrenade_hit_event_publish
            // 爆発エフェクト再生
            // ダメージ適用
            Console.WriteLine($"PowerGrenade hit!");
        }
    }

    public class ClusterGrenade : AbstractGrenade
    {
        int child = 3;

        public ClusterGrenade()
        {


        }

        public int Child { get => child; set => child = value; }

        public override void Hit()
        {
            // クラスター爆発時に子grenadeを生成
            Console.WriteLine($"ClusterGrenade hit! Spawning {Child} child grenades");
        }
    }

    public class ChildClusterGrenade : AbstractGrenade
    {
        public ChildClusterGrenade()
        {


        }

        public override void Hit()
        {
            // 子grenadeの爆発処理
            Console.WriteLine("ChildClusterGrenade hit!");
        }
    }
}
