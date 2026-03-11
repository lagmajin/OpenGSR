using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGSCore;


namespace OpenGS
{

    //#PlayerSession
    public class PlayerSessionInfo
    {
        private PlayerID _localId;

        public PlayerID LocalId
        {
            get => _localId ??= PlayerID.New();
            private set => _localId = value;
        }

        public bool Online { get; set; }
        
        // Store GlobalUserId from login success message
        public string GlobalUserId { get; set; }
        
        public void EnsureLocalId()
        {
            _ = LocalId; // getter 呼ぶだけで遅延初期化が走る
        }


        // 必要ならテスト用/復元用に明示的設定メソッドも作れる
        public void SetLocalId(PlayerID id) => LocalId = id;
    }
}
