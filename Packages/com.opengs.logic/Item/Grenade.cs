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

        public PowerGrenade() : this(0f, 0f)
        {

        }

        public PowerGrenade(float x, float y) : base(x, y)
        {
            GranadeType.Type = EGranadeType.Power;
            Damage = 110;
            ExplosionDamage = 160;
            ExplosionRadius = 4.5f;
            FuseTime = 2.5f;
            StoppingPower = 85;
        }

        public override void Hit()
        {
            // 爆発そのものは基底の OnExploded が Exploded イベントとして通知する。
            // クライアントはそこでエフェクトを出すだけで、ダメージはサーバの判定に従う。
            SetState(GrenadeState.Exploded);
            DisposeGrenade();
        }
    }

    public class ClusterGrenade : AbstractGrenade
    {
        int child = 3;

        public ClusterGrenade() : this(0f, 0f)
        {

        }

        public ClusterGrenade(float x, float y) : base(x, y)
        {
            GranadeType.Type = EGranadeType.Cluster;
            FuseTime = 2.0f;
            ExplosionRadius = 3.0f;
        }

        public int Child { get => child; set => child = Math.Max(0, value); }

        /// <summary>
        /// 親の位置から扇状に子グレネードを生成する。
        /// 親はここで破棄されるので、生成された子は親とは独立して扱われる。
        /// </summary>
        public ChildClusterGrenade[] SpawnChildren()
        {
            var spawned = new ChildClusterGrenade[Child];

            // 扇の開き角はangle为中心にした120度。子がない設定なら生成しない。
            const float spreadDegrees = 120f;
            float step = Child > 1 ? spreadDegrees / (Child - 1) : 0f;
            float start = Angle - spreadDegrees / 2f;

            for (int i = 0; i < Child; i++)
            {
                float degrees = start + step * i;
                float radians = degrees * (float)Math.PI / 180f;
                var childGrenade = new ChildClusterGrenade(Posx, Posy)
                {
                    OwnerId = OwnerId,
                    Angle = (int)degrees
                };

                // 散らし方向へ爆発半径だけ飛ばして置く。
                childGrenade.SetPos(
                    Posx + (float)Math.Cos(radians) * ExplosionRadius,
                    Posy + (float)Math.Sin(radians) * ExplosionRadius);

                spawned[i] = childGrenade;
                NotifyChildCreated(childGrenade);
            }

            return spawned;
        }

        public override void Hit()
        {
            SpawnChildren();
            SetState(GrenadeState.Exploded);
            DisposeGrenade();
        }
    }

    public class ChildClusterGrenade : AbstractGrenade
    {
        public ChildClusterGrenade() : this(0f, 0f)
        {

        }

        public ChildClusterGrenade(float x, float y) : base(x, y)
        {
            GranadeType.Type = EGranadeType.Cluster;
            Damage = 60;
            ExplosionDamage = 90;
            ExplosionRadius = 2.0f;
            FuseTime = 1.0f;
        }

        public override void Hit()
        {
            // 子を生むのは親だけなので、この手榴弾は爆発して消えるだけ。
            SetState(GrenadeState.Exploded);
            DisposeGrenade();
        }
    }
}
