//  AssemblyCompileTimeWindow.cs
//  http://kan-kikuchi.hatenablog.com/entry/Assembly_Compile_Time
//
//  Created by kan.kikuchi on 2018.11.26.

using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using Debug = UnityEngine.Debug;

/// <summary>
/// アセンブリごとのコンパイル時間を計測するためのウィンドウ
/// </summary>
public class AssemblyCompileTimeWindow : EditorWindow {

  //時間を計るためのクラス
  private static Stopwatch _stopwatch = new Stopwatch();

  //スクロール位置
  private Vector2 _scrollPosition = Vector2.zero;

  //設定情報と保存時のKey
  private AssemblyCompileTimeWindowSettingData _settingData = null;
  private const string SETTING_DATA_SAVE_KEY = "AssemblyCompileTimeWindowSettingDataKey";

  //=================================================================================
  //ウィンドウ表示
  //=================================================================================

  //メニューからウィンドウを表示
  [MenuItem("Window/AssemblyCompileTimeWindow")]
  public static void Open() {
    GetWindow<AssemblyCompileTimeWindow>(typeof(AssemblyCompileTimeWindow));
  }

  //=================================================================================
  //イベントの管理
  //=================================================================================

  //開いた時などに実行される
  private void OnEnable() {
    //各アセンブリのコンパイル開始、終了時のイベントにメソッド登録
    CompilationPipeline.assemblyCompilationStarted  += OnAssemblyCompilationStarted;
    CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

    //設定情報のロード
    _settingData = JsonUtility.FromJson<AssemblyCompileTimeWindowSettingData>(EditorUserSettings.GetConfigValue(SETTING_DATA_SAVE_KEY));
    if (_settingData == null) {
      _settingData = new AssemblyCompileTimeWindowSettingData();
    }
  }

  //閉じた時などに実行される                                                   
  private void OnDisable() {
    //各アセンブリのコンパイル開始、終了時のイベントからメソッド削除
    CompilationPipeline.assemblyCompilationStarted  -= OnAssemblyCompilationStarted;
    CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;

    //設定情報の保存
    EditorUserSettings.SetConfigValue(SETTING_DATA_SAVE_KEY, JsonUtility.ToJson(_settingData));
  }

  //=================================================================================
  //各コンパイル開始と終了の検知
  //=================================================================================

  //各コンパイル開始時に呼ばれる
  private void OnAssemblyCompilationStarted(object obj) {
    //計測時間を初期化し、計測開始
    _stopwatch.Reset();
    _stopwatch.Start();
  }

  //各コンパイル終了時に呼ばれる
  private void OnAssemblyCompilationFinished(object obj, CompilerMessage[] messages) {
    //計測を終了時、コンパイル時間を取得
    _stopwatch.Stop();
    float compileTime = (float)_stopwatch.Elapsed.TotalSeconds;

    //直近のコンパイル時間を更新
    string assemblyName = obj.ToString().Replace("Library/ScriptAssemblies/", "");
    _settingData.UpdateCurrentCompileTime(assemblyName, compileTime);

    //警告表示フラグが有効かつ、閾値を超過してる場合は警告を表示
    if (_settingData.ShouldShowWarning && compileTime > _settingData.WarningBorder) {
      Debug.LogWarningFormat("{0}のコンパイル時間が長すぎます！ : {1}秒, (閾値 : {2}秒)", assemblyName, compileTime.ToString("F2"), _settingData.WarningBorder.ToString("F2"));
    }
    //ログ表示のフラグが有効ならコンパイル時間をログで表示
    else if (_settingData.ShouldShowLog) {
      Debug.Log(assemblyName + "のコンパイル時間 : " + compileTime.ToString("F2") + "秒");
    }

  }

  //=================================================================================
  //表示するGUIの設定
  //=================================================================================

  private void OnGUI() {
    //描画範囲が足りなければスクロール出来るように
    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.scrollView);

    EditorGUILayout.BeginVertical(GUI.skin.box);{

      //ログを表示するかのフラグを設定するトグル
      EditorGUILayout.BeginVertical(GUI.skin.box);{
        _settingData.ShouldShowLog = EditorGUILayout.ToggleLeft("各コンパイル時間をログで表示する", _settingData.ShouldShowLog);
      }
      EditorGUILayout.EndVertical();

      GUILayout.Space(10);

      //警告を表示するかのフラグを設定するトグルと、警告を表示する時間の閾値
      EditorGUILayout.BeginVertical(GUI.skin.box);{
        _settingData.ShouldShowWarning = EditorGUILayout.ToggleLeft("コンパイル時間が長すぎたら警告を出す", _settingData.ShouldShowWarning);

        if (_settingData.ShouldShowWarning) {
          _settingData.WarningBorder = EditorGUILayout.FloatField("警告を出す時間の閾値", _settingData.WarningBorder);
        }
      }
      EditorGUILayout.EndVertical();

      //データがあれば各アセンブリの直近のコンパイル時間を表示
      if (_settingData.CurrentCompileTimeList.Count > 0) {
        GUILayout.Space(10);

        EditorGUILayout.BeginVertical(GUI.skin.box);{
          ShowCurrentCompileTimeGUI();
        }
        EditorGUILayout.EndVertical();
      }

    }
    EditorGUILayout.EndVertical();

    EditorGUILayout.EndScrollView();
  }

  //各アセンブリの直近のコンパイル時間を表示
  private void ShowCurrentCompileTimeGUI() {
    EditorGUILayout.LabelField("直近のコンパイル時間(長い順)");

    GUILayout.Space(10);

    EditorGUILayout.BeginVertical(GUI.skin.box);{
      foreach (NameAndTimePair pair in _settingData.CurrentCompileTimeList) {
        EditorGUILayout.LabelField(pair.Time.ToString("F2") + "秒 : " + pair.Name);
      }
    }
    EditorGUILayout.EndVertical();

    GUILayout.Space(10);

    if (GUILayout.Button("リセット")) {
      _settingData.ResetCurrentCompileTime();
    }
  }

}

/// <summary>
/// AssemblyCompileTimeWindowの設定情報
/// </summary>
public class AssemblyCompileTimeWindowSettingData {

  //各コンパイル時間をログで表示するかのフラグ
  [SerializeField]
  private bool _shouldShowLog = false;
  public  bool  ShouldShowLog { get { return _shouldShowLog; } set { _shouldShowLog = value; } }

  //コンパイル時間が長すぎる時警告をだすかのフラグと、警告を出す閾値
  [SerializeField]
  private bool _shouldShowWarning = false;
  public  bool  ShouldShowWarning { get { return _shouldShowWarning; } set { _shouldShowWarning = value; } }

  [SerializeField]
  private float _warningBorder = 60;
  public  float  WarningBorder { get { return _warningBorder; } set { _warningBorder = value; } }

  //各アセンブリの直近のコンパイル時間
  [SerializeField]
  private List<NameAndTimePair> _currentCompileTimeList = new List<NameAndTimePair>();
  public  List<NameAndTimePair>  CurrentCompileTimeList { get { return _currentCompileTimeList; } }

  //=================================================================================
  //コンパイル時間
  //=================================================================================

  /// <summary>
  /// 直近のコンパイル時間を初期化する
  /// </summary>
  public void ResetCurrentCompileTime() {
    _currentCompileTimeList.Clear();
  }

  /// <summary>
  /// アセンブリの直近のコンパイル時間を更新する
  /// </summary>
  public void UpdateCurrentCompileTime(string assemblyName, float compileTime) {
    //既に登録されてる場合は更新、ソート
    foreach (NameAndTimePair nameAndTimePair in _currentCompileTimeList) {
      if (nameAndTimePair.Name == assemblyName) {
        nameAndTimePair.Time = compileTime;
        SortCompileTimeList();
        return;
      }
    }

    //登録されていない場合は新規登録、ソート
    _currentCompileTimeList.Add(new NameAndTimePair(assemblyName, compileTime));
    SortCompileTimeList();
  }

  //各アセンブリの直近のコンパイル時間をまとめたListを長い順にソートする
  private void SortCompileTimeList() {
    _currentCompileTimeList = _currentCompileTimeList.OrderByDescending(pair => pair.Time).ToList();
  }

}

/// <summary>
/// 名前と時間をペアで管理するクラス(シリアライズするために、Listで使ってDictの代わりに)
/// </summary>
[Serializable]
public class NameAndTimePair {

  [SerializeField]
  private string _name = "";
  public  string  Name { get { return _name; } }

  [SerializeField]
  private float _time = 0;
  public  float  Time { get { return _time; } set { _time = value; } }

  public NameAndTimePair(string name, float time) {
    _name = name;
    _time = time;
  }

}