using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UniRx;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS.EditorTools
{
    /// <summary>
    /// UnityEditor 専用のデバッグダッシュボード。
    /// Console だけでは追いにくいログ、イベント、プレイヤー状態、選択オブジェクトをまとめて確認できる。
    /// </summary>
    public sealed class DebugDashboardWindow : EditorWindow
    {
        private enum DashboardTab
        {
            Overview,
            Network,
            Players,
            Logs,
            Events,
            Watch
        }

        [Serializable]
        private sealed class WatchItem
        {
            public UnityEngine.Object Target;
            public string Label;
            public bool Expanded = true;
        }

        [Serializable]
        private sealed class LogEntry
        {
            public string TimeLabel;
            public LogType Type;
            public string Message;
            public string StackTrace;
        }

        [Serializable]
        private sealed class EventEntry
        {
            public string TimeLabel;
            public string Channel;
            public string Name;
            public string Summary;
        }

        [Serializable]
        private sealed class NetworkEntry
        {
            public string TimeLabel;
            public string Source;
            public string Direction;
            public string MessageType;
            public string Summary;
        }

        private readonly struct QueuedLog
        {
            public readonly string TimeLabel;
            public readonly string Message;
            public readonly string StackTrace;
            public readonly LogType Type;

            public QueuedLog(string timeLabel, string message, string stackTrace, LogType type)
            {
                TimeLabel = timeLabel;
                Message = message;
                StackTrace = stackTrace;
                Type = type;
            }
        }

        private readonly struct QueuedEvent
        {
            public readonly string TimeLabel;
            public readonly string Channel;
            public readonly string Name;
            public readonly string Summary;

            public QueuedEvent(string timeLabel, string channel, string name, string summary)
            {
                TimeLabel = timeLabel;
                Channel = channel;
                Name = name;
                Summary = summary;
            }
        }

        private readonly struct QueuedNetworkEntry
        {
            public readonly string TimeLabel;
            public readonly string Source;
            public readonly string Direction;
            public readonly string MessageType;
            public readonly string Summary;

            public QueuedNetworkEntry(string timeLabel, string source, string direction, string messageType, string summary)
            {
                TimeLabel = timeLabel;
                Source = source;
                Direction = direction;
                MessageType = messageType;
                Summary = summary;
            }
        }

        [SerializeField] private DashboardTab currentTab = DashboardTab.Overview;
        [SerializeField] private List<WatchItem> watchItems = new List<WatchItem>();
        [SerializeField] private List<LogEntry> logEntries = new List<LogEntry>();
        [SerializeField] private List<EventEntry> eventEntries = new List<EventEntry>();
        [SerializeField] private List<NetworkEntry> networkEntries = new List<NetworkEntry>();
        [SerializeField] private bool autoAddSelectionAsWatch = true;
        [SerializeField] private bool showSelectionPreview = true;
        [SerializeField] private bool repaintWhilePlaying = true;
        [SerializeField] private float repaintInterval = 0.2f;
        [SerializeField] private bool captureLogs = true;
        [SerializeField] private bool captureEvents = true;
        [SerializeField] private bool captureNetwork = true;
        [SerializeField] private bool autoScrollLogs = true;
        [SerializeField] private bool autoScrollEvents = true;
        [SerializeField] private bool autoScrollNetwork = true;
        [SerializeField] private bool showInfoLogs = true;
        [SerializeField] private bool showWarnings = true;
        [SerializeField] private bool showErrors = true;
        [SerializeField] private bool showExceptions = true;
        [SerializeField] private string logSearch = string.Empty;
        [SerializeField] private string eventSearch = string.Empty;
        [SerializeField] private string networkSearch = string.Empty;
        [SerializeField] private string watchSearch = string.Empty;
        [SerializeField] private int maxLogEntries = 500;
        [SerializeField] private int maxEventEntries = 250;
        [SerializeField] private int maxNetworkEntries = 250;

        private readonly ConcurrentQueue<QueuedLog> pendingLogs = new ConcurrentQueue<QueuedLog>();
        private readonly ConcurrentQueue<QueuedEvent> pendingEvents = new ConcurrentQueue<QueuedEvent>();
        private readonly ConcurrentQueue<QueuedNetworkEntry> pendingNetworkEntries = new ConcurrentQueue<QueuedNetworkEntry>();

        private Vector2 mainScroll;
        private Vector2 logsScroll;
        private Vector2 eventsScroll;
        private Vector2 networkScroll;
        private double nextRepaintTime;
        private UnityEngine.Object pendingAddTarget;
        private PlayerRegistry trackedRegistry;
        private readonly List<IDisposable> brokerSubscriptions = new List<IDisposable>();
        private readonly List<IDisposable> networkSubscriptions = new List<IDisposable>();
        private GeneralServerNetworkManager trackedGeneralNetworkManager;
        private MatchRUDPServerNetworkManager trackedMatchNetworkManager;
        private ClientNetworkManager trackedClientNetworkManager;
        private WaitRoomNetworkManager trackedWaitRoomNetworkManager;
        private bool generalNetworkSubscribed;
        private bool matchNetworkSubscribed;
        private bool logHooked;

        [MenuItem("OpenGSR/Tools/Debug Dashboard")]
        private static void Open()
        {
            var window = GetWindow<DebugDashboardWindow>();
            window.titleContent = new GUIContent("Debug");
            window.minSize = new Vector2(860f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            HookLogCapture();
            SyncRuntimeHooks(force: true);
            SyncNetworkHooks(force: true);
            CacheSelectionPreview();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            UnhookLogCapture();
            DisposeEventSubscriptions();
            DisposeNetworkSubscriptions();
            UnhookTrackedRegistry();
            UnhookTrackedNetworkManagers();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.EnteredPlayMode:
                case PlayModeStateChange.ExitingEditMode:
                    SyncRuntimeHooks(force: true);
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    DisposeEventSubscriptions();
                    DisposeNetworkSubscriptions();
                    UnhookTrackedRegistry();
                    UnhookTrackedNetworkManagers();
                    break;
            }

            Repaint();
        }

        private void OnSelectionChanged()
        {
            if (autoAddSelectionAsWatch)
            {
                pendingAddTarget = Selection.activeObject;
            }

            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (pendingAddTarget != null)
            {
                AddWatch(pendingAddTarget);
                pendingAddTarget = null;
            }

            DrainQueues();
            SyncRuntimeHooks(force: false);
            SyncNetworkHooks(force: false);

            if (!Application.isPlaying || !repaintWhilePlaying)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < nextRepaintTime)
            {
                return;
            }

            nextRepaintTime = EditorApplication.timeSinceStartup + Math.Max(0.05, repaintInterval);
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(4f);

            mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

            switch (currentTab)
            {
                case DashboardTab.Overview:
                    DrawOverviewTab();
                    break;
                case DashboardTab.Network:
                    DrawNetworkTab();
                    break;
                case DashboardTab.Players:
                    DrawPlayersTab();
                    break;
                case DashboardTab.Logs:
                    DrawLogsTab();
                    break;
                case DashboardTab.Events:
                    DrawEventsTab();
                    break;
                case DashboardTab.Watch:
                    DrawWatchTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Add Selection", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                {
                    AddWatch(Selection.activeObject);
                }

                if (GUILayout.Button("Clear Watch", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                {
                    watchItems.Clear();
                }

                if (GUILayout.Button("Clear Logs", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    logEntries.Clear();
                }

                if (GUILayout.Button("Clear Events", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                {
                    eventEntries.Clear();
                }

                GUILayout.FlexibleSpace();

                autoAddSelectionAsWatch = GUILayout.Toggle(autoAddSelectionAsWatch, "Auto Add", EditorStyles.toolbarButton);
                showSelectionPreview = GUILayout.Toggle(showSelectionPreview, "Selection", EditorStyles.toolbarButton);
                captureLogs = GUILayout.Toggle(captureLogs, "Logs", EditorStyles.toolbarButton);
                captureEvents = GUILayout.Toggle(captureEvents, "Events", EditorStyles.toolbarButton);
                captureNetwork = GUILayout.Toggle(captureNetwork, "Network", EditorStyles.toolbarButton);
                autoScrollLogs = GUILayout.Toggle(autoScrollLogs, "Scroll Logs", EditorStyles.toolbarButton);
                autoScrollEvents = GUILayout.Toggle(autoScrollEvents, "Scroll Events", EditorStyles.toolbarButton);
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                DrawTabButton(DashboardTab.Overview, "Overview", 80f);
                DrawTabButton(DashboardTab.Network, "Network", 75f);
                DrawTabButton(DashboardTab.Players, "Players", 70f);
                DrawTabButton(DashboardTab.Logs, "Logs", 60f);
                DrawTabButton(DashboardTab.Events, "Events", 70f);
                DrawTabButton(DashboardTab.Watch, "Watch", 70f);

                GUILayout.FlexibleSpace();

                repaintWhilePlaying = GUILayout.Toggle(repaintWhilePlaying, "Live Repaint", EditorStyles.toolbarButton);
            }
        }

        private void DrawTabButton(DashboardTab tab, string label, float width)
        {
            var isActive = currentTab == tab;
            var style = isActive ? EditorStyles.toolbarButton : EditorStyles.toolbarButton;
            if (GUILayout.Button(label, style, GUILayout.Width(width)))
            {
                currentTab = tab;
            }
        }

        private void DrawOverviewTab()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Play Mode", Application.isPlaying ? "Playing" : "Edit");
                EditorGUILayout.LabelField("Paused", EditorApplication.isPaused ? "Yes" : "No");
                EditorGUILayout.LabelField("Active Scene", SceneManager.GetActiveScene().name);
                EditorGUILayout.LabelField("Selected", Selection.activeObject != null ? Selection.activeObject.name : "-");
                EditorGUILayout.LabelField("Watch Count", watchItems.Count.ToString());
                EditorGUILayout.LabelField("Log Count", logEntries.Count.ToString());
                EditorGUILayout.LabelField("Event Count", eventEntries.Count.ToString());
                EditorGUILayout.LabelField("Network Count", networkEntries.Count.ToString());
                EditorGUILayout.LabelField("Selected Character", GamePlayerManager.Instance != null ? GamePlayerManager.Instance.SelectedPlayerCharacter().ToString() : "-");
            }

            if (showSelectionPreview)
            {
                EditorGUILayout.Space(4f);
                DrawSelectionPanel();
            }

            EditorGUILayout.Space(4f);
            DrawPlayerRegistrySummary();
        }

        private void DrawNetworkTab()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Network", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Capture", captureNetwork ? "On" : "Off");
                EditorGUILayout.LabelField("Total Entries", networkEntries.Count.ToString());

                using (new EditorGUILayout.HorizontalScope())
                {
                    captureNetwork = GUILayout.Toggle(captureNetwork, "Capture", EditorStyles.miniButton);
                    autoScrollNetwork = GUILayout.Toggle(autoScrollNetwork, "Auto Scroll", EditorStyles.miniButton);

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Refresh", GUILayout.Width(75f)))
                    {
                        SyncNetworkHooks(force: true);
                    }

                    if (GUILayout.Button("Clear", GUILayout.Width(60f)))
                    {
                        networkEntries.Clear();
                    }

                    if (GUILayout.Button("Copy Visible", GUILayout.Width(95f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = BuildVisibleNetworkText();
                    }
                }

                DrawNetworkSummaryPanel();

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Search", GUILayout.Width(44f));
                    networkSearch = EditorGUILayout.TextField(networkSearch);
                }

                var visibleEntries = GetVisibleNetworkEntries().ToArray();
                EditorGUILayout.LabelField("Visible", visibleEntries.Length.ToString());

                if (autoScrollNetwork)
                {
                    networkScroll.y = float.MaxValue;
                }

                using (var scroll = new EditorGUILayout.ScrollViewScope(networkScroll, GUILayout.MinHeight(240f)))
                {
                    networkScroll = scroll.scrollPosition;

                    for (var i = 0; i < visibleEntries.Length; i++)
                    {
                        DrawNetworkEntry(visibleEntries[i]);
                    }
                }
            }
        }

        private void DrawNetworkSummaryPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Connections", EditorStyles.boldLabel);
                DrawNetworkSourceSummary("General Server", trackedGeneralNetworkManager != null ? "Resolved" : "Missing", DescribeGeneralNetworkManager());
                DrawNetworkSourceSummary("Match RUDP", trackedMatchNetworkManager != null ? "Resolved" : "Missing", DescribeMatchNetworkManager());
                DrawNetworkSourceSummary("Client TCP/UDP", trackedClientNetworkManager != null ? "Scene Object" : "Missing", DescribeClientNetworkManager());
                DrawNetworkSourceSummary("WaitRoom", trackedWaitRoomNetworkManager != null ? "Scene Object" : "Missing", DescribeWaitRoomNetworkManager());
            }
        }

        private void DrawNetworkSourceSummary(string label, string status, string details)
        {
            EditorGUILayout.LabelField(label, status);
            if (!string.IsNullOrWhiteSpace(details))
            {
                EditorGUILayout.LabelField(" ", details);
            }
        }

        private void DrawPlayerRegistrySummary()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Player Registry", EditorStyles.boldLabel);

                if (PlayerRegistry.Instance == null)
                {
                    EditorGUILayout.HelpBox("PlayerRegistry.Instance is null.", MessageType.Info);
                    return;
                }

                var players = PlayerRegistry.Instance.GetAllPlayers().Where(player => player != null).ToArray();
                EditorGUILayout.LabelField("Registered Players", players.Length.ToString());

                if (players.Length == 0)
                {
                    EditorGUILayout.LabelField("No players are registered.");
                    return;
                }

                for (var i = 0; i < Math.Min(players.Length, 6); i++)
                {
                    var player = players[i];
                    EditorGUILayout.LabelField(
                        player.gameObject.name,
                        $"{ShortGuid(player.UniqueID())} | {player.PlayerType()} | {player.Team()} | HP {FormatGauge(player.GetHP(), player.GetMaxHP())} | Armor {FormatGauge(player.GetArmor(), player.GetMaxArmor())} | Booster {FormatGauge(player.GetBooster(), player.GetMaxBooster())}"
                    );
                }

                if (players.Length > 6)
                {
                    EditorGUILayout.LabelField("More", $"+{players.Length - 6} players");
                }
            }
        }

        private void DrawPlayersTab()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Players", EditorStyles.boldLabel);

                if (PlayerRegistry.Instance == null)
                {
                    EditorGUILayout.HelpBox("PlayerRegistry.Instance is null.", MessageType.Info);
                    return;
                }

                var players = PlayerRegistry.Instance.GetAllPlayers().Where(player => player != null).ToArray();
                if (players.Length == 0)
                {
                    EditorGUILayout.LabelField("No registered players.");
                    return;
                }

                foreach (var player in players)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(player.gameObject.name, EditorStyles.boldLabel);
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("Ping", GUILayout.Width(48f)))
                            {
                                EditorGUIUtility.PingObject(player.gameObject);
                            }

                            if (GUILayout.Button("Select", GUILayout.Width(56f)))
                            {
                                Selection.activeObject = player.gameObject;
                            }
                        }

                        EditorGUILayout.LabelField("GUID", player.UniqueID().ToString());
                        EditorGUILayout.LabelField("Character", player.Character().ToString());
                        EditorGUILayout.LabelField("Type", player.PlayerType().ToString());
                        EditorGUILayout.LabelField("Team", player.Team().ToString());
                        EditorGUILayout.LabelField("Dead", player.IsDead() ? "Yes" : "No");
                        EditorGUILayout.LabelField("Position", FormatVector3(player.transform.position));
                        EditorGUILayout.LabelField("HP", FormatGauge(player.GetHP(), player.GetMaxHP()));
                        EditorGUILayout.LabelField("Armor", FormatGauge(player.GetArmor(), player.GetMaxArmor()));
                        EditorGUILayout.LabelField("Booster", FormatGauge(player.GetBooster(), player.GetMaxBooster()));
                    }
                }
            }
        }

        private void DrawLogsTab()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Logs", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    showInfoLogs = GUILayout.Toggle(showInfoLogs, "Info", EditorStyles.miniButton);
                    showWarnings = GUILayout.Toggle(showWarnings, "Warning", EditorStyles.miniButton);
                    showErrors = GUILayout.Toggle(showErrors, "Error", EditorStyles.miniButton);
                    showExceptions = GUILayout.Toggle(showExceptions, "Exception", EditorStyles.miniButton);

                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Search", GUILayout.Width(44f));
                    logSearch = EditorGUILayout.TextField(logSearch);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    maxLogEntries = Mathf.Max(50, EditorGUILayout.IntField("Max Entries", maxLogEntries));
                    if (GUILayout.Button("Copy Visible", GUILayout.Width(95f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = BuildVisibleLogText();
                    }
                }

                var visibleLogs = GetVisibleLogs().ToArray();
                EditorGUILayout.LabelField("Visible", visibleLogs.Length.ToString());

                if (autoScrollLogs)
                {
                    logsScroll.y = float.MaxValue;
                }

                using (var scroll = new EditorGUILayout.ScrollViewScope(logsScroll, GUILayout.MinHeight(220f)))
                {
                    logsScroll = scroll.scrollPosition;

                    for (var i = 0; i < visibleLogs.Length; i++)
                    {
                        DrawLogEntry(visibleLogs[i]);
                    }
                }
            }
        }

        private void DrawEventsTab()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Search", GUILayout.Width(44f));
                    eventSearch = EditorGUILayout.TextField(eventSearch);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    maxEventEntries = Mathf.Max(50, EditorGUILayout.IntField("Max Entries", maxEventEntries));
                    if (GUILayout.Button("Copy Visible", GUILayout.Width(95f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = BuildVisibleEventText();
                    }
                }

                var visibleEvents = GetVisibleEvents().ToArray();
                EditorGUILayout.LabelField("Visible", visibleEvents.Length.ToString());

                if (autoScrollEvents)
                {
                    eventsScroll.y = float.MaxValue;
                }

                using (var scroll = new EditorGUILayout.ScrollViewScope(eventsScroll, GUILayout.MinHeight(220f)))
                {
                    eventsScroll = scroll.scrollPosition;

                    for (var i = 0; i < visibleEvents.Length; i++)
                    {
                        DrawEventEntry(visibleEvents[i]);
                    }
                }
            }
        }

        private void DrawWatchTab()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Watch", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Search", GUILayout.Width(44f));
                    watchSearch = EditorGUILayout.TextField(watchSearch);

                    if (GUILayout.Button("Add Selection", GUILayout.Width(100f)))
                    {
                        AddWatch(Selection.activeObject);
                    }
                }

                if (watchItems.Count == 0)
                {
                    EditorGUILayout.LabelField("No pinned objects yet.");
                    return;
                }

                var visibleItems = watchItems
                    .Where(item => item != null && MatchesFilter(item.Label, watchSearch))
                    .ToArray();

                EditorGUILayout.LabelField("Visible", visibleItems.Length.ToString());

                for (var index = 0; index < watchItems.Count; index++)
                {
                    var item = watchItems[index];
                    if (item == null)
                    {
                        continue;
                    }

                    if (!MatchesFilter(item.Label, watchSearch))
                    {
                        continue;
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            item.Expanded = EditorGUILayout.Foldout(item.Expanded, item.Label ?? "(unnamed)", true);
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("Ping", GUILayout.Width(48f)))
                            {
                                EditorGUIUtility.PingObject(item.Target);
                            }

                            if (GUILayout.Button("Select", GUILayout.Width(56f)))
                            {
                                Selection.activeObject = item.Target;
                            }

                            if (GUILayout.Button("X", GUILayout.Width(24f)))
                            {
                                watchItems.RemoveAt(index);
                                GUIUtility.ExitGUI();
                            }
                        }

                        if (!item.Expanded)
                        {
                            continue;
                        }

                        if (item.Target == null)
                        {
                            EditorGUILayout.HelpBox("Target is missing.", MessageType.Warning);
                            continue;
                        }

                        DrawObjectSummary(item.Target);
                    }
                }
            }
        }

        private void DrawSelectionPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);

                var active = Selection.activeObject;
                if (active == null)
                {
                    EditorGUILayout.LabelField("Nothing selected.");
                    return;
                }

                DrawObjectSummary(active);
            }
        }

        private void DrawObjectSummary(UnityEngine.Object target)
        {
            EditorGUILayout.ObjectField("Target", target, typeof(UnityEngine.Object), true);
            EditorGUILayout.LabelField("Type", target.GetType().Name);

            var go = GetGameObject(target);
            if (go != null)
            {
                EditorGUILayout.LabelField("GameObject", go.name);
                EditorGUILayout.LabelField("Active", go.activeInHierarchy ? "Yes" : "No");
                EditorGUILayout.LabelField("Layer", LayerMask.LayerToName(go.layer));
                EditorGUILayout.LabelField("Hierarchy", GetHierarchyPath(go.transform));
                DrawTransformDetails(go.transform);
                DrawComponentNames(go);
            }
        }

        private void DrawTransformDetails(Transform transform)
        {
            EditorGUILayout.LabelField("Position", FormatVector3(transform.position));
            EditorGUILayout.LabelField("Local Position", FormatVector3(transform.localPosition));
            EditorGUILayout.LabelField("Rotation", FormatVector3(transform.eulerAngles));
            EditorGUILayout.LabelField("Local Scale", FormatVector3(transform.localScale));

            if (transform is RectTransform rectTransform)
            {
                EditorGUILayout.LabelField("Anchored Pos", FormatVector3(rectTransform.anchoredPosition3D));
                EditorGUILayout.LabelField("Size Delta", FormatVector2(rectTransform.sizeDelta));
            }
        }

        private void DrawComponentNames(GameObject go)
        {
            var components = go.GetComponents<Component>()
                .Where(component => component != null)
                .Select(component => component.GetType().Name)
                .ToArray();

            EditorGUILayout.LabelField("Components", components.Length > 0 ? string.Join(", ", components) : "-");
        }

        private void DrawLogEntry(LogEntry entry)
        {
            if (!ShouldShowLogType(entry.Type))
            {
                return;
            }

            if (!MatchesFilter(entry.Message, logSearch) && !MatchesFilter(entry.StackTrace, logSearch))
            {
                return;
            }

            var color = GetLogColor(entry.Type);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var header = $"[{entry.TimeLabel}] {entry.Type} {entry.Message}";
                var style = new GUIStyle(EditorStyles.label)
                {
                    richText = true,
                    wordWrap = true
                };

                EditorGUILayout.SelectableLabel(Colorize(header, color), style, GUILayout.MinHeight(18f));

                if (!string.IsNullOrWhiteSpace(entry.StackTrace))
                {
                    EditorGUILayout.SelectableLabel(entry.StackTrace, EditorStyles.textArea, GUILayout.MinHeight(36f));
                }
            }
        }

        private void DrawEventEntry(EventEntry entry)
        {
            if (!MatchesFilter(entry.Name, eventSearch) &&
                !MatchesFilter(entry.Channel, eventSearch) &&
                !MatchesFilter(entry.Summary, eventSearch))
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"[{entry.TimeLabel}] {entry.Channel}.{entry.Name}", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(entry.Summary, EditorStyles.textArea, GUILayout.MinHeight(22f));
            }
        }

        private void DrawNetworkEntry(NetworkEntry entry)
        {
            if (!MatchesFilter(entry.Source, networkSearch) &&
                !MatchesFilter(entry.Direction, networkSearch) &&
                !MatchesFilter(entry.MessageType, networkSearch) &&
                !MatchesFilter(entry.Summary, networkSearch))
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"[{entry.TimeLabel}] {entry.Source} {entry.Direction} {entry.MessageType}", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(entry.Summary, EditorStyles.textArea, GUILayout.MinHeight(22f));
            }
        }

        private void SyncRuntimeHooks(bool force)
        {
            if (captureLogs)
            {
                HookLogCapture();
            }
            else
            {
                UnhookLogCapture();
            }

            if (!Application.isPlaying || !captureEvents)
            {
                if (force)
                {
                    DisposeEventSubscriptions();
                    UnhookTrackedRegistry();
                }

                return;
            }

            EnsureEventSubscriptions();
            SyncTrackedRegistry();
        }

        private void SyncNetworkHooks(bool force)
        {
            if (!captureNetwork || !Application.isPlaying)
            {
                if (force || !captureNetwork)
                {
                    DisposeNetworkSubscriptions();
                    UnhookTrackedNetworkManagers();
                }

                return;
            }

            if (force)
            {
                DisposeNetworkSubscriptions();
                UnhookTrackedNetworkManagers();
            }

            ResolveNetworkManagers();
            SubscribeToNetworkManagers();
        }

        private void ResolveNetworkManagers()
        {
            trackedGeneralNetworkManager ??= TryResolveGeneralNetworkManager();
            trackedMatchNetworkManager ??= TryResolveMatchNetworkManager();
            trackedClientNetworkManager ??= FindFirstObjectByType<ClientNetworkManager>();
            trackedWaitRoomNetworkManager ??= FindFirstObjectByType<WaitRoomNetworkManager>();
        }

        private void SubscribeToNetworkManagers()
        {
            if (trackedGeneralNetworkManager != null && !generalNetworkSubscribed)
            {
                networkSubscriptions.Add(trackedGeneralNetworkManager.ConnectedStream.Subscribe(_ => EnqueueNetworkState("GeneralServer", "Connected", "Connected", "connected")));
                networkSubscriptions.Add(trackedGeneralNetworkManager.DisconnectedStream.Subscribe(_ => EnqueueNetworkState("GeneralServer", "Disconnected", "Disconnected", "disconnected")));
                networkSubscriptions.Add(trackedGeneralNetworkManager.DataReceivedStream.Subscribe(json => EnqueueNetworkJson("GeneralServer", "Received", json)));
                generalNetworkSubscribed = true;
            }

            if (trackedMatchNetworkManager != null && !matchNetworkSubscribed)
            {
                networkSubscriptions.Add(trackedMatchNetworkManager.ConnectedStream.Subscribe(_ => EnqueueNetworkState("MatchRUDP", "Connected", "Connected", "connected")));
                networkSubscriptions.Add(trackedMatchNetworkManager.DisconnectedStream.Subscribe(_ => EnqueueNetworkState("MatchRUDP", "Disconnected", "Disconnected", "disconnected")));
                networkSubscriptions.Add(trackedMatchNetworkManager.DataReceivedStream.Subscribe(json => EnqueueNetworkJson("MatchRUDP", "Received", json)));
                matchNetworkSubscribed = true;
            }
        }

        private void DisposeNetworkSubscriptions()
        {
            for (var i = 0; i < networkSubscriptions.Count; i++)
            {
                networkSubscriptions[i]?.Dispose();
            }

            networkSubscriptions.Clear();
            generalNetworkSubscribed = false;
            matchNetworkSubscribed = false;
        }

        private void UnhookTrackedNetworkManagers()
        {
            trackedGeneralNetworkManager = null;
            trackedMatchNetworkManager = null;
            trackedClientNetworkManager = null;
            trackedWaitRoomNetworkManager = null;
        }

        private void HookLogCapture()
        {
            if (logHooked)
            {
                return;
            }

            Application.logMessageReceivedThreaded += HandleLogMessageReceivedThreaded;
            logHooked = true;
        }

        private void UnhookLogCapture()
        {
            if (!logHooked)
            {
                return;
            }

            Application.logMessageReceivedThreaded -= HandleLogMessageReceivedThreaded;
            logHooked = false;
        }

        private void HandleLogMessageReceivedThreaded(string condition, string stackTrace, LogType type)
        {
            if (!captureLogs)
            {
                return;
            }

            var timeLabel = DateTime.Now.ToString("HH:mm:ss");
            pendingLogs.Enqueue(new QueuedLog(timeLabel, condition ?? string.Empty, stackTrace ?? string.Empty, type));
        }

        private void DrainQueues()
        {
            while (pendingLogs.TryDequeue(out var log))
            {
                logEntries.Add(new LogEntry
                {
                    TimeLabel = log.TimeLabel,
                    Message = log.Message,
                    StackTrace = log.StackTrace,
                    Type = log.Type
                });
            }

            while (pendingEvents.TryDequeue(out var evt))
            {
                eventEntries.Add(new EventEntry
                {
                    TimeLabel = evt.TimeLabel,
                    Channel = evt.Channel,
                    Name = evt.Name,
                    Summary = evt.Summary
                });
            }

            while (pendingNetworkEntries.TryDequeue(out var net))
            {
                networkEntries.Add(new NetworkEntry
                {
                    TimeLabel = net.TimeLabel,
                    Source = net.Source,
                    Direction = net.Direction,
                    MessageType = net.MessageType,
                    Summary = net.Summary
                });
            }

            TrimToLimit(logEntries, maxLogEntries);
            TrimToLimit(eventEntries, maxEventEntries);
            TrimToLimit(networkEntries, maxNetworkEntries);
        }

        private void EnsureEventSubscriptions()
        {
            if (brokerSubscriptions.Count > 0)
            {
                return;
            }

            SubscribeEvent<PlayerDamageEvent>("GameEventBroker", evt => $"target={evt.TargetID()} attacker={evt.AttackerID()} damage={evt.Damage()} remaining={evt.RemainingHp()}");
            SubscribeEvent<PlayerKillEvent>("GameEventBroker", evt => $"killer={evt.KillerID()} victim={evt.VictimID()} weapon={evt.WeaponType()} headshot={evt.IsHeadshot()}");
            SubscribeEvent<PlayerDeadEvent>("GameEventBroker", evt => $"player={evt.PlayerName()} id={evt.PlayerID()} killer={evt.KillerID()} reason={evt.Reason()} team={evt.PlayerTeam()}");
            SubscribeEvent<PlayerRespawnEvent>("GameEventBroker", evt => $"player={evt.PlayerID()} position={FormatVector2(evt.Position())}");
            SubscribeEvent<RespawnCountdownEvent>("GameEventBroker", evt => $"player={evt.PlayerID()} countdown={evt.CountdownSeconds()}");
            SubscribeEvent<RoundStartEvent>("GameEventBroker", evt => $"round={evt.RoundNumber()}/{evt.TotalRounds()}");
            SubscribeEvent<RoundEndEvent>("GameEventBroker", evt => $"round={evt.RoundNumber()} winner={evt.WinningTeam()}");
            SubscribeEvent<MatchPauseEvent>("GameEventBroker", evt => $"pausedBy={evt.PausedByPlayerID()}");
            SubscribeEvent<MatchResumeEvent>("GameEventBroker", evt => $"resumedBy={evt.ResumedByPlayerID()}");
            SubscribeEvent<PlayerJoinedEvent>("GameEventBroker", evt => $"player={evt.PlayerName()} id={evt.PlayerID()} team={evt.Team()}");
            SubscribeEvent<PlayerLeftEvent>("GameEventBroker", evt => $"player={evt.PlayerID()} reason={evt.Reason()}");
            SubscribeEvent<PlayerTeamSwitchEvent>("GameEventBroker", evt => $"player={evt.PlayerID()} team={evt.NewTeam()}");
            SubscribeEvent<WeaponChangeEvent>("GameEventBroker", evt => $"player={evt.PlayerID()} weapon={evt.WeaponType()} slot={evt.SlotIndex()}");
            SubscribeEvent<AmmoUpdateEvent>("GameEventBroker", evt => $"player={evt.PlayerID()} weapon={evt.WeaponType()} ammo={evt.CurrentAmmo()}/{evt.MaxAmmo()}");
            SubscribeEvent<GrenadeThrowEvent>("GameEventBroker", evt => $"player={evt.PlayerID()} grenade={evt.GrenadeType()} position={FormatVector2(evt.Position())}");
            SubscribeEvent<PlayerReloadEvent>("GameEventBroker", evt => $"player={evt.PlayerID()} weapon={evt.WeaponType()} empty={evt.IsEmpty()}");
            SubscribeEvent<PlayerMeleeEvent>("GameEventBroker", evt => $"player={evt.PlayerID()} weapon={evt.WeaponID()} position={FormatVector2(evt.Position())}");
            SubscribeEvent<VoteEvent>("GameEventBroker", evt => $"vote={evt.VoteID()} type={evt.VoteType()} by={evt.InitiatedBy()} target={evt.TargetID()} duration={evt.Duration()}");
            SubscribeEvent<VoteResultEvent>("GameEventBroker", evt => $"vote={evt.VoteID()} passed={evt.Passed()} message={evt.Message()}");
            SubscribeEvent<PlayerSpectatingEvent>("GameEventBroker", evt => $"player={evt.PlayerID()} spectating={evt.IsSpectating()}");
            SubscribeEvent<PlayerReviveEvent>("GameEventBroker", evt => $"player={evt.PlayerID()} revivedBy={evt.RevivedByPlayerID()} position={FormatVector2(evt.Position())}");
            SubscribeEvent<BuffEvent>("GameEventBroker", evt => $"player={evt.PlayerID()} buff={evt.BuffType()} duration={evt.Duration()} value={evt.Value():F2} debuff={evt.IsDebuff()}");
            SubscribeEvent<BuffExpiredEvent>("GameEventBroker", evt => $"player={evt.PlayerID()} buff={evt.BuffType()}");
            SubscribeEvent<ObjectSpawnedEvent>("GameEventBroker", evt => $"object={evt.ObjectType()} id={evt.ObjectID()} position={FormatVector2(evt.Position())} rotation={evt.Rotation():F1}");
            SubscribeEvent<ObjectDestroyedEvent>("GameEventBroker", evt => $"object={evt.ObjectID()} destroyedBy={evt.DestroyedBy()} position={FormatVector2(evt.Position())}");
            SubscribeEvent<PingEvent>("GameEventBroker", evt => $"player={evt.PlayerID()} clientTs={evt.ClientTimestamp()} serverTs={evt.ServerTimestamp()}");
            SubscribeEvent<WarmupEvent>("GameEventBroker", evt => $"start={evt.IsStart()} duration={evt.Duration()}");
            SubscribeEvent<MatchTimeSyncEvent>("GameEventBroker", evt => $"remaining={evt.RemainingTime()} serverTs={evt.ServerTimestamp()}");
        }

        private void DisposeEventSubscriptions()
        {
            for (var i = 0; i < brokerSubscriptions.Count; i++)
            {
                brokerSubscriptions[i]?.Dispose();
            }

            brokerSubscriptions.Clear();
        }

        private void SubscribeEvent<T>(string channel, Func<T, string> formatter)
        {
            var subscription = GameEventBroker.Subscribe<T>(evt =>
            {
                if (!captureEvents || evt == null)
                {
                    return;
                }

                var summary = formatter != null ? formatter(evt) : string.Empty;
                var timeLabel = DateTime.Now.ToString("HH:mm:ss");
                pendingEvents.Enqueue(new QueuedEvent(timeLabel, channel, typeof(T).Name, summary));
            });

            brokerSubscriptions.Add(subscription);
        }

        private void SyncTrackedRegistry()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var current = PlayerRegistry.Instance;
            if (trackedRegistry == current)
            {
                return;
            }

            UnhookTrackedRegistry();
            trackedRegistry = current;

            if (trackedRegistry == null)
            {
                return;
            }

            trackedRegistry.OnPlayerRegistered += HandlePlayerRegistryEvent;
            trackedRegistry.OnPlayerUnregistered += HandlePlayerRegistryEvent;
            trackedRegistry.OnPlayerHealthChanged += HandlePlayerHealthChanged;
            trackedRegistry.OnPlayerArmorChanged += HandlePlayerArmorChanged;
            trackedRegistry.OnPlayerBoosterChanged += HandlePlayerBoosterChanged;
            trackedRegistry.OnPlayerDied += HandlePlayerRegistryEvent;
            trackedRegistry.OnPlayerSpawned += HandlePlayerRegistryEvent;
            trackedRegistry.OnPlayerRespawned += HandlePlayerRegistryEvent;
            trackedRegistry.OnPlayerStatusChanged += HandlePlayerStatusChanged;
        }

        private void UnhookTrackedRegistry()
        {
            if (trackedRegistry == null)
            {
                return;
            }

            trackedRegistry.OnPlayerRegistered -= HandlePlayerRegistryEvent;
            trackedRegistry.OnPlayerUnregistered -= HandlePlayerRegistryEvent;
            trackedRegistry.OnPlayerHealthChanged -= HandlePlayerHealthChanged;
            trackedRegistry.OnPlayerArmorChanged -= HandlePlayerArmorChanged;
            trackedRegistry.OnPlayerBoosterChanged -= HandlePlayerBoosterChanged;
            trackedRegistry.OnPlayerDied -= HandlePlayerRegistryEvent;
            trackedRegistry.OnPlayerSpawned -= HandlePlayerRegistryEvent;
            trackedRegistry.OnPlayerRespawned -= HandlePlayerRegistryEvent;
            trackedRegistry.OnPlayerStatusChanged -= HandlePlayerStatusChanged;
            trackedRegistry = null;
        }

        private void HandlePlayerRegistryEvent(AbstractPlayer player)
        {
            if (player == null || !captureEvents)
            {
                return;
            }

            EnqueueEvent("PlayerRegistry", player.GetType().Name, $"{player.gameObject.name} ({ShortGuid(player.UniqueID())})");
        }

        private void HandlePlayerHealthChanged(AbstractPlayer player, float newHp)
        {
            if (player == null || !captureEvents)
            {
                return;
            }

            EnqueueEvent("PlayerRegistry", nameof(PlayerRegistry.OnPlayerHealthChanged), $"{player.gameObject.name} hp={newHp:F1}/{player.GetMaxHP():F1}");
        }

        private void HandlePlayerArmorChanged(AbstractPlayer player, float newArmor)
        {
            if (player == null || !captureEvents)
            {
                return;
            }

            EnqueueEvent("PlayerRegistry", nameof(PlayerRegistry.OnPlayerArmorChanged), $"{player.gameObject.name} armor={newArmor:F1}/{player.GetMaxArmor():F1}");
        }

        private void HandlePlayerBoosterChanged(AbstractPlayer player, float newBooster)
        {
            if (player == null || !captureEvents)
            {
                return;
            }

            EnqueueEvent("PlayerRegistry", nameof(PlayerRegistry.OnPlayerBoosterChanged), $"{player.gameObject.name} booster={newBooster:F1}/{player.GetMaxBooster():F1}");
        }

        private void HandlePlayerStatusChanged(AbstractPlayer player, OpenGS.PlayerStatus status)
        {
            if (player == null || !captureEvents)
            {
                return;
            }

            EnqueueEvent("PlayerRegistry", nameof(PlayerRegistry.OnPlayerStatusChanged), $"{player.gameObject.name} status={(status != null ? "updated" : "null")}");
        }

        private void EnqueueEvent(string channel, string name, string summary)
        {
            if (!captureEvents)
            {
                return;
            }

            var timeLabel = DateTime.Now.ToString("HH:mm:ss");
            pendingEvents.Enqueue(new QueuedEvent(timeLabel, channel, name, summary));
        }

        private void EnqueueNetworkState(string source, string direction, string messageType, string summary)
        {
            if (!captureNetwork)
            {
                return;
            }

            var timeLabel = DateTime.Now.ToString("HH:mm:ss");
            pendingNetworkEntries.Enqueue(new QueuedNetworkEntry(timeLabel, source, direction, messageType, summary));
        }

        private void EnqueueNetworkJson(string source, string direction, JObject json)
        {
            if (!captureNetwork || json == null)
            {
                return;
            }

            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());
            if (string.IsNullOrWhiteSpace(messageType))
            {
                messageType = "(no MessageType)";
            }

            EnqueueNetworkState(source, direction, messageType, SummarizeNetworkJson(json));
        }

        private void CacheSelectionPreview()
        {
            if (autoAddSelectionAsWatch && Selection.activeObject != null)
            {
                pendingAddTarget = Selection.activeObject;
            }
        }

        private void AddWatch(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (watchItems.Any(item => item != null && item.Target == target))
            {
                return;
            }

            watchItems.Add(new WatchItem
            {
                Target = target,
                Label = GetWatchLabel(target),
                Expanded = true
            });
        }

        private IEnumerable<LogEntry> GetVisibleLogs()
        {
            for (var i = 0; i < logEntries.Count; i++)
            {
                var entry = logEntries[i];
                if (entry == null)
                {
                    continue;
                }

                if (!ShouldShowLogType(entry.Type))
                {
                    continue;
                }

                if (!MatchesFilter(entry.Message, logSearch) && !MatchesFilter(entry.StackTrace, logSearch))
                {
                    continue;
                }

                yield return entry;
            }
        }

        private IEnumerable<EventEntry> GetVisibleEvents()
        {
            for (var i = 0; i < eventEntries.Count; i++)
            {
                var entry = eventEntries[i];
                if (entry == null)
                {
                    continue;
                }

                if (!MatchesFilter(entry.Name, eventSearch) &&
                    !MatchesFilter(entry.Channel, eventSearch) &&
                    !MatchesFilter(entry.Summary, eventSearch))
                {
                    continue;
                }

                yield return entry;
            }
        }

        private IEnumerable<NetworkEntry> GetVisibleNetworkEntries()
        {
            for (var i = 0; i < networkEntries.Count; i++)
            {
                var entry = networkEntries[i];
                if (entry == null)
                {
                    continue;
                }

                if (!MatchesFilter(entry.Source, networkSearch) &&
                    !MatchesFilter(entry.Direction, networkSearch) &&
                    !MatchesFilter(entry.MessageType, networkSearch) &&
                    !MatchesFilter(entry.Summary, networkSearch))
                {
                    continue;
                }

                yield return entry;
            }
        }

        private string BuildVisibleLogText()
        {
            var builder = new StringBuilder();
            foreach (var entry in GetVisibleLogs())
            {
                builder.Append('[').Append(entry.TimeLabel).Append("] ");
                builder.Append(entry.Type).Append(' ');
                builder.AppendLine(entry.Message ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(entry.StackTrace))
                {
                    builder.AppendLine(entry.StackTrace);
                }
            }

            return builder.ToString();
        }

        private string BuildVisibleEventText()
        {
            var builder = new StringBuilder();
            foreach (var entry in GetVisibleEvents())
            {
                builder.Append('[').Append(entry.TimeLabel).Append("] ");
                builder.Append(entry.Channel).Append('.').Append(entry.Name).Append(" - ");
                builder.AppendLine(entry.Summary ?? string.Empty);
            }

            return builder.ToString();
        }

        private string BuildVisibleNetworkText()
        {
            var builder = new StringBuilder();
            foreach (var entry in GetVisibleNetworkEntries())
            {
                builder.Append('[').Append(entry.TimeLabel).Append("] ");
                builder.Append(entry.Source).Append(' ').Append(entry.Direction).Append(' ');
                builder.Append(entry.MessageType).Append(" - ");
                builder.AppendLine(entry.Summary ?? string.Empty);
            }

            return builder.ToString();
        }

        private bool ShouldShowLogType(LogType type)
        {
            return type switch
            {
                LogType.Log => showInfoLogs,
                LogType.Warning => showWarnings,
                LogType.Error => showErrors,
                LogType.Exception => showExceptions,
                LogType.Assert => showExceptions,
                _ => true
            };
        }

        private static void TrimToLimit<T>(List<T> list, int limit)
        {
            if (list == null)
            {
                return;
            }

            if (limit < 1)
            {
                limit = 1;
            }

            if (list.Count <= limit)
            {
                return;
            }

            var removeCount = list.Count - limit;
            list.RemoveRange(0, removeCount);
        }

        private static GameObject GetGameObject(UnityEngine.Object target)
        {
            switch (target)
            {
                case GameObject gameObject:
                    return gameObject;
                case Component component:
                    return component.gameObject;
                default:
                    return null;
            }
        }

        private static string GetWatchLabel(UnityEngine.Object target)
        {
            var go = GetGameObject(target);
            if (go != null)
            {
                return $"{go.name} ({target.GetType().Name})";
            }

            return target != null ? $"{target.name} ({target.GetType().Name})" : "(null)";
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            var current = transform;

            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        private static string ShortGuid(Guid guid)
        {
            var text = guid.ToString();
            return text.Length > 8 ? text.Substring(0, 8) : text;
        }

        private static string FormatGauge(float value, float max)
        {
            return $"{value:F1}/{max:F1}";
        }

        private static string FormatVector2(Vector2 value)
        {
            return $"({value.x:F2}, {value.y:F2})";
        }

        private static string FormatVector3(Vector3 value)
        {
            return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
        }

        private static string SummarizeNetworkJson(JObject json)
        {
            if (json == null)
            {
                return string.Empty;
            }

            var keys = new[]
            {
                "MessageType",
                "RoomID",
                "RoomId",
                "PlayerID",
                "PlayerId",
                "TargetPlayerID",
                "TargetPlayerId",
                "AccountName",
                "Progress",
                "Message",
                "Success",
                "Error",
                "IP",
                "IPAddress",
                "Port",
                "UdpPort",
                "Reason",
                "Countdown"
            };

            var parts = new List<string>();
            foreach (var key in keys)
            {
                if (json.TryGetValue(key, out var token) && token != null && token.Type != JTokenType.Null)
                {
                    parts.Add($"{key}={token}");
                }
            }

            if (parts.Count == 0)
            {
                var raw = json.ToString(Newtonsoft.Json.Formatting.None);
                return raw.Length > 240 ? raw.Substring(0, 240) + "..." : raw;
            }

            var summary = string.Join(" | ", parts);
            return summary.Length > 300 ? summary.Substring(0, 300) + "..." : summary;
        }

        private static GeneralServerNetworkManager TryResolveGeneralNetworkManager()
        {
            try
            {
                return DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
            }
            catch
            {
                return null;
            }
        }

        private static MatchRUDPServerNetworkManager TryResolveMatchNetworkManager()
        {
            try
            {
                return DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
            }
            catch
            {
                return null;
            }
        }

        private string DescribeGeneralNetworkManager()
        {
            if (trackedGeneralNetworkManager == null)
            {
                return "not resolved";
            }

            return $"online={trackedGeneralNetworkManager.Online}";
        }

        private string DescribeMatchNetworkManager()
        {
            if (trackedMatchNetworkManager == null)
            {
                return "not resolved";
            }

            return $"connected={trackedMatchNetworkManager.IsConnected()}";
        }

        private string DescribeClientNetworkManager()
        {
            if (trackedClientNetworkManager == null)
            {
                return "not found";
            }

            var tcpClient = GetPrivateFieldValue<TcpClient>(trackedClientNetworkManager, "_tcpClient");
            var serverPeer = GetPrivateFieldValue<object>(trackedClientNetworkManager, "_serverPeer");
            var matchAttempted = GetPrivateFieldValue<bool>(trackedClientNetworkManager, "_matchUdpConnectAttempted");
            var tcpConnected = tcpClient != null && tcpClient.Connected;
            return $"player={trackedClientNetworkManager.ClientPlayerId} tcp={tcpConnected} udpPeer={(serverPeer != null ? "present" : "none")} matchAttempted={matchAttempted} matchRoom={trackedClientNetworkManager.CurrentMatchRoomId}";
        }

        private string DescribeWaitRoomNetworkManager()
        {
            if (trackedWaitRoomNetworkManager == null)
            {
                return "not found";
            }

            return $"room={trackedWaitRoomNetworkManager.CurrentRoomId} ready={trackedWaitRoomNetworkManager.IsReady} players={trackedWaitRoomNetworkManager.CurrentPlayers?.Count ?? 0}";
        }

        private static T GetPrivateFieldValue<T>(object instance, string fieldName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(fieldName))
            {
                return default;
            }

            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return default;
            }

            var value = field.GetValue(instance);
            if (value is T typed)
            {
                return typed;
            }

            return default;
        }

        private static string Colorize(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{EscapeRichText(text)}</color>";
        }

        private static string EscapeRichText(string text)
        {
            return string.IsNullOrEmpty(text)
                ? string.Empty
                : text.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static Color GetLogColor(LogType type)
        {
            return type switch
            {
                LogType.Warning => new Color(1f, 0.82f, 0.25f, 1f),
                LogType.Error => new Color(1f, 0.45f, 0.45f, 1f),
                LogType.Exception => new Color(1f, 0.35f, 0.35f, 1f),
                LogType.Assert => new Color(1f, 0.62f, 0.35f, 1f),
                _ => new Color(0.85f, 0.95f, 1f, 1f)
            };
        }

        private static bool MatchesFilter(string value, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
