using System;
using Newtonsoft.Json;



namespace OpenGSCore
{
    class Timestamp
    {
        private DateTime _time;

        // コンストラクタで現在の時刻を保持
        public Timestamp() => _time = DateTime.Now;

        // 任意の DateTime を指定して保持
        public Timestamp(DateTime dateTime) => _time = dateTime;

        // 作成時のタイムをそのまま保持
        public DateTime Time => _time;

        // デフォルトフォーマットで返す（yyyy-MM-dd HH:mm:ss）
        public override string ToString() => _time.ToString("yyyy-MM-dd HH:mm:ss");

        // よく使うフォーマットをメンバ関数として定義
        public string ToFileNameFormat() => _time.ToString("yyyy-MM-dd_HH-mm-ss");
        public string ToShortFormat() => _time.ToString("MM-dd HH:mm");
        public string ToFullDateFormat() => _time.ToString("yyyy-MM-dd HH:mm:ss");
        public string ToUtcFormat() => _time.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // 指定したフォーマットで返す
        public string Format(string format) => _time.ToString(format);

        // 静的メソッドで現在時刻を保持した Timestamp を生成
        public static Timestamp Now() => new Timestamp(DateTime.Now);
        public static Timestamp UtcNow() => new Timestamp(DateTime.UtcNow);
    }
}
