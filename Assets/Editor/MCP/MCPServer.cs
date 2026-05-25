using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Object = UnityEngine.Object;
using TMPro;

namespace OpenGSR.Editor.MCP
{
    [InitializeOnLoad]
    public static class MCPServer
    {
        private static TcpListener _listener;
        private static Thread _serverThread;
        private static CancellationTokenSource _cts;
        private static readonly int Port = 51234;

        private static readonly ConcurrentQueue<Action> _mainThreadQueue = new();
        private static SynchronizationContext _mainThreadContext;

        static MCPServer()
        {
            _mainThreadContext = SynchronizationContext.Current;
            EditorApplication.update += ProcessMainThreadQueue;
            EditorApplication.quitting += StopServer;
            StartServer();
        }

        [MenuItem("OpenGSR/MCP/Start Server")]
        public static void StartServer()
        {
            if (_serverThread?.IsAlive == true) return;

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Loopback, Port);
            _listener.Start();

            _serverThread = new Thread(RunServer) { IsBackground = true, Name = "MCP" };
            _serverThread.Start();
            Debug.Log($"[MCP] Server listening on port {Port}");
        }

        [MenuItem("OpenGSR/MCP/Stop Server")]
        public static void StopServer()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _serverThread = null;
            Debug.Log("[MCP] Server stopped");
        }

        private static void ProcessMainThreadQueue()
        {
            while (_mainThreadQueue.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception ex) { Debug.LogError($"[MCP] {ex}"); }
            }
        }

        private static void RunServer()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var client = _listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
        }

        private static void HandleClient(object state)
        {
            using var client = (TcpClient)state;
            client.ReceiveTimeout = 0;
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { NewLine = "\n", AutoFlush = true };

            while (!_cts.Token.IsCancellationRequested)
            {
                var line = reader.ReadLine();
                if (line == null) break;

                var response = HandleRequest(line);
                writer.WriteLine(response);
            }
        }

        private static string HandleRequest(string json)
        {
            try
            {
                var request = JObject.Parse(json);
                var id = request["id"];
                var method = request["method"]?.ToString();
                var jparams = request["params"] as JObject ?? new JObject();

                var result = Dispatch(method, jparams);

                return JsonConvert.SerializeObject(new JObject
                {
                    ["jsonrpc"] = "2.0",
                    ["id"] = id,
                    ["result"] = result
                });
            }
            catch (JsonException ex)
            {
                return JsonConvert.SerializeObject(new JObject
                {
                    ["jsonrpc"] = "2.0",
                    ["id"] = null,
                    ["error"] = new JObject { ["code"] = -32700, ["message"] = $"Parse error: {ex.Message}" }
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new JObject
                {
                    ["jsonrpc"] = "2.0",
                    ["id"] = null,
                    ["error"] = new JObject { ["code"] = -32603, ["message"] = ex.Message }
                });
            }
        }

        private static JToken Dispatch(string method, JObject jparams)
        {
            JToken result = null;
            Exception error = null;

            _mainThreadContext.Send(_ =>
            {
                try
                {
                    result = method switch
                    {
                        "get_scene_hierarchy" => GetSceneHierarchy(jparams),
                        "get_game_object_info" => GetGameObjectInfo(jparams),
                        "find_game_objects" => FindGameObjects(jparams),
                        "create_game_object" => CreateGameObject(jparams),
                        "delete_game_object" => DeleteGameObject(jparams),
                        "set_transform_position" => SetTransformPosition(jparams),
                        "set_transform_rotation" => SetTransformRotation(jparams),
                        "set_transform_scale" => SetTransformScale(jparams),
                        "add_component" => AddComponent(jparams),
                        "remove_component" => RemoveComponent(jparams),
                        "set_parent" => SetParent(jparams),
                        "set_active" => SetActive(jparams),
                        "duplicate_game_object" => DuplicateGameObject(jparams),
                        "set_property" => SetProperty(jparams),
                        "rename_game_object" => RenameGameObject(jparams),
                        "set_tag" => SetTag(jparams),
                        "set_layer" => SetLayer(jparams),
                        "set_static_flags" => SetStaticFlags(jparams),
                        "select_and_frame" => SelectAndFrame(jparams),
                        "find_assets" => FindAssets(jparams),
                        "get_asset_info" => GetAssetInfo(jparams),
                        "instantiate_prefab" => InstantiatePrefab(jparams),
                        "save_prefab" => SavePrefab(jparams),
                        "create_material" => CreateMaterial(jparams),
                        "create_folder" => CreateFolder(jparams),
                        "create_script" => CreateScript(jparams),
                        "open_scene" => OpenScene(jparams),
                        "get_project_structure" => GetProjectStructure(jparams),
                        "save_scene" => SaveScene(jparams),
                        "get_all_scenes" => GetAllScenes(jparams),
                        "set_active_scene" => SetActiveScene(jparams),
                        "find_objects_by_component" => FindObjectsByComponent(jparams),
                        "batch_set_property" => BatchSetProperty(jparams),
                        "unpack_prefab" => UnpackPrefab(jparams),
                        "revert_prefab_overrides" => RevertPrefabOverrides(jparams),
                        "duplicate_asset" => DuplicateAsset(jparams),
                        "delete_asset" => DeleteAsset(jparams),
                        "move_asset" => MoveAsset(jparams),
                        "create_ui_element" => CreateUIElement(jparams),
                        "create_light" => CreateLight(jparams),
                        "create_camera" => CreateCamera(jparams),
                        "set_play_mode" => SetPlayMode(jparams),
                        "get_all_tags" => GetAllTags(jparams),
                        "get_all_layers" => GetAllLayers(jparams),
                        "set_material_property" => SetMaterialProperty(jparams),
                        "set_renderer_material" => SetRendererMaterial(jparams),
                        "get_asset_dependencies" => GetAssetDependencies(jparams),
                        "create_physics_material" => CreatePhysicsMaterial(jparams),
                        "create_particle_system" => CreateParticleSystem(jparams),
                        "create_audio_source" => CreateAudioSource(jparams),
                        "create_animator_controller" => CreateAnimatorController(jparams),
                        "refresh_and_compile" => RefreshAndCompile(jparams),
                        "build_project" => BuildProject(jparams),
                        "get_console_logs" => GetConsoleLogs(jparams),
                        "list_tools" => ListTools(),
                        _ => throw new Exception($"Method not found: {method}")
                    };
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            }, null);

            if (error != null)
                return new JObject { ["error"] = error.Message };
            return result;
        }

        // ─── Tool Implementations ───

        private static JToken ListTools()
        {
            return new JArray
            {
                T("get_scene_hierarchy", "Get the full hierarchy of the current scene", O()),
                T("get_game_object_info", "Get detailed info about a GameObject", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"),
                    P("include_properties", "boolean", "Include serialized property values (default true)"))),
                T("find_game_objects", "Find GameObjects by name", O(
                    P("name", "string", "Name to search (partial match)"),
                    P("max_results", "number", "Max results (default 50)"))),
                T("create_game_object", "Create a new GameObject", O(
                    P("name", "string", "Name of the new GameObject"),
                    P("type", "string", "empty or primitive", E("empty", "primitive")),
                    P("primitive_type", "string", "Primitive shape if type=primitive", E("Cube","Sphere","Capsule","Cylinder","Plane","Quad")),
                    P("parent_path", "string", "Parent in hierarchy"),
                    P("select", "boolean", "Select after creation"))),
                T("delete_game_object", "Delete a GameObject", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"))),
                T("set_transform_position", "Set world/local position of a GameObject", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"),
                    P("x", "number", "X position"), P("y", "number", "Y position"), P("z", "number", "Z position"),
                    P("space", "string", "world or local (default world)", E("world", "local")))),
                T("set_transform_rotation", "Set euler rotation of a GameObject", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"),
                    P("x", "number", "X rotation"), P("y", "number", "Y rotation"), P("z", "number", "Z rotation"))),
                T("set_transform_scale", "Set local scale of a GameObject", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"),
                    P("x", "number", "X scale"), P("y", "number", "Y scale"), P("z", "number", "Z scale"))),
                T("add_component", "Add a component to a GameObject", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"),
                    P("type", "string", "Full type name (e.g. UnityEngine.Rigidbody, TMPro.TextMeshProUGUI)"))),
                T("remove_component", "Remove a component from a GameObject", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"),
                    P("type", "string", "Type name of the component to remove"))),
                T("set_parent", "Reparent a GameObject", O(
                    P("path", "string", "Path of child"),
                    P("instance_id", "number", "Instance ID of child"),
                    P("parent_path", "string", "New parent path (null to detach)"),
                    P("world_position_stays", "boolean", "Keep world position (default true)"))),
                T("set_active", "Set active state of a GameObject", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"),
                    P("active", "boolean", "Active state"))),
                T("duplicate_game_object", "Duplicate a GameObject", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"))),
                T("set_property", "Set a serialized property on a component", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"),
                    P("component_type", "string", "Component type name"),
                    P("property", "string", "Serialized property path (e.g. m_Enabled, m_Color.r)"),
                    P("value", "object", "New value (string, number, bool, object, or null to clear reference)"))),
                T("rename_game_object", "Rename a GameObject", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"),
                    P("name", "string", "New name"))),
                T("set_tag", "Set the tag of a GameObject", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"),
                    P("tag", "string", "Tag name"))),
                T("set_layer", "Set the layer of a GameObject", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"),
                    P("layer", "string", "Layer name"),
                    P("layer_index", "number", "Layer index (alternative to name)"))),
                T("set_static_flags", "Set static/batching flags on a GameObject", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"),
                    P("flags", "array", "Array of flags: BatchingStatic, LightmapStatic, NavigationStatic, OccluderStatic, OccludeeStatic, OffMeshLinkGeneration, ReflectionProbeStatic, All"))),
                T("select_and_frame", "Select and frame a GameObject in Scene view", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"))),
                T("find_assets", "Search project assets by name, type, or path", O(
                    P("name", "string", "Asset name filter"),
                    P("type", "string", "Asset type filter (e.g. Material, Texture2D, GameObject)"),
                    P("path", "string", "Path filter"),
                    P("max_results", "number", "Max results (default 50)"),
                    P("sort_by_name", "boolean", "Sort results by name"))),
                T("get_asset_info", "Get detailed info about a project asset", O(
                    P("path", "string", "Asset path"),
                    P("guid", "string", "Asset GUID (alternative to path)"))),
                T("instantiate_prefab", "Instantiate a prefab/asset into the current scene", O(
                    P("path", "string", "Prefab asset path"),
                    P("guid", "string", "Prefab asset GUID"),
                    P("parent_path", "string", "Parent in hierarchy"),
                    P("name", "string", "Override instance name"),
                    P("position", "object", "World position {x, y, z}"))),
                T("save_prefab", "Save a GameObject as a prefab (create or update)", O(
                    P("instance_id", "number", "Unity instance ID"),
                    P("path", "string", "Path in hierarchy of the GameObject"),
                    P("prefab_path", "string", "Target prefab path in project (default Assets/<name>.prefab)"))),
                T("create_material", "Create a new Material asset", O(
                    P("path", "string", "Target path (e.g. Assets/Materials/MyMat.mat)"),
                    P("shader", "string", "Shader name (default Standard)"),
                    P("color", "object", "Main color {r, g, b, a}"))),
                T("create_folder", "Create a folder in the project", O(
                    P("path", "string", "Folder path (e.g. Assets/MyFolder)"))),
                T("create_script", "Create a new C# script asset with template", O(
                    P("path", "string", "Script path (e.g. Assets/Scripts/MyScript.cs)"),
                    P("template", "string", "Code template", E("MonoBehaviour", "ScriptableObject", "Singleton", "Weapon", "BulletAgent", "UIManager")),
                    P("namespace", "string", "Optional namespace"))),
                T("open_scene", "Open a scene by path", O(
                    P("path", "string", "Scene asset path"),
                    P("mode", "string", "single, additive, or additivewithoutloading", E("single", "additive", "additivewithoutloading")))),
                T("get_project_structure", "Get the project's folder structure", O(
                    P("path", "string", "Root path (default Assets)"),
                    P("max_depth", "number", "Max folder depth (default 3)"),
                    P("include_files", "boolean", "Include files in output (default true)"),
                    P("type", "string", "Filter by asset type"))),
                T("save_scene", "Save the current scene", O()),
                T("get_all_scenes", "List all scenes in the project", O()),
                T("set_active_scene", "Set the active scene (must already be open)", O(
                    P("path", "string", "Scene path"),
                    P("name", "string", "Scene name (alternative to path)"))),
                T("find_objects_by_component", "Find GameObjects with a specific component type", O(
                    P("type", "string", "Component type name"),
                    P("max_results", "number", "Max results (default 100)"))),
                T("batch_set_property", "Set a property on all components of a given type", O(
                    P("component_type", "string", "Component type name"),
                    P("property", "string", "Serialized property path"),
                    P("value", "object", "New value"))),
                T("unpack_prefab", "Unpack a prefab instance (completely)", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"))),
                T("revert_prefab_overrides", "Revert all overrides on a prefab instance", O(
                    P("path", "string", "Path in hierarchy"),
                    P("instance_id", "number", "Unity instance ID"))),
                T("duplicate_asset", "Duplicate a project asset", O(
                    P("path", "string", "Source asset path"),
                    P("new_path", "string", "Destination path (default: <name> (Copy).ext)"))),
                T("delete_asset", "Delete a project asset", O(
                    P("path", "string", "Asset path to delete"))),
                T("move_asset", "Move or rename a project asset", O(
                    P("path", "string", "Current asset path"),
                    P("new_path", "string", "New asset path"))),
                T("create_ui_element", "Create a UI element under a Canvas", O(
                    P("type", "string", "UI type", E("Button", "Text", "Image", "Slider", "Panel", "InputField", "Toggle")),
                    P("parent_path", "string", "Parent transform path (default Canvas)"),
                    P("name", "string", "Element name"),
                    P("text", "string", "Text content (for Text elements)"),
                    P("size", "object", "Size delta {x, y}"))),
                T("create_light", "Create a light", O(
                    P("light_type", "string", "Type", E("directional", "point", "spot", "area")),
                    P("name", "string", "Light name"),
                    P("color", "object", "Light color {r, g, b}"),
                    P("intensity", "number", "Light intensity"),
                    P("range", "number", "Light range (point/spot)"),
                    P("shadow", "boolean", "Enable shadows"),
                    P("parent_path", "string", "Parent transform path"))),
                T("create_camera", "Create a Camera", O(
                    P("name", "string", "Camera name"),
                    P("clear_flags", "string", "skybox, solid, depth, nothing", E("skybox", "solid", "depth", "nothing")),
                    P("fov", "number", "Field of view"),
                    P("near", "number", "Near clip plane"),
                    P("far", "number", "Far clip plane"),
                    P("orthographic", "boolean", "Orthographic mode"),
                    P("orthographic_size", "number", "Orthographic size"),
                    P("culling_mask", "string", "Comma-separated layer names"),
                    P("parent_path", "string", "Parent transform path"))),
                T("set_play_mode", "Enter or exit Play Mode", O(
                    P("enter", "boolean", "true=enter, false=exit play mode"))),
                T("get_all_tags", "List all available tags", O()),
                T("get_all_layers", "List all available layers", O()),
                T("set_material_property", "Set properties on a material", O(
                    P("path", "string", "Material asset path"),
                    P("guid", "string", "Material GUID"),
                    P("color", "object", "Main color {r, g, b, a}"),
                    P("main_texture", "object", "Main texture {path: ...} or {guid: ...}"),
                    P("float_properties", "object", "Map of float property names to values"),
                    P("shader", "string", "Shader name to switch to"),
                    P("render_queue", "number", "Render queue value"))),
                T("set_renderer_material", "Assign a material to a Renderer", O(
                    P("path", "string", "GameObject path"),
                    P("instance_id", "number", "Unity instance ID"),
                    P("material_path", "string", "Material asset path"),
                    P("material_guid", "string", "Material GUID"),
                    P("slot", "number", "Material slot index (default 0)"),
                    P("all_slots", "boolean", "Apply to all material slots"))),
                T("get_asset_dependencies", "Get dependencies of an asset", O(
                    P("path", "string", "Asset path"),
                    P("guid", "string", "Asset GUID"),
                    P("recursive", "boolean", "Include recursive dependencies"))),
                T("create_physics_material", "Create a PhysicMaterial asset", O(
                    P("path", "string", "Target path (e.g. Assets/Physics/Ice.physicsMaterial)"),
                    P("static_friction", "number", "Static friction (default 0.6)"),
                    P("dynamic_friction", "number", "Dynamic friction (default 0.6)"),
                    P("bounciness", "number", "Bounciness (default 0)"),
                    P("friction_combine", "string", "Friction combine mode", E("Average", "Minimum", "Maximum", "Multiply")),
                    P("bounce_combine", "string", "Bounce combine mode", E("Average", "Minimum", "Maximum", "Multiply")))),
                T("create_particle_system", "Create a ParticleSystem GameObject with optional preset", O(
                    P("name", "string", "GameObject name"),
                    P("parent_path", "string", "Parent transform path"),
                    P("preset", "string", "Quick preset", E("none", "fire", "smoke", "sparks", "explosion")),
                    P("looping", "boolean", "Looping (default true)"),
                    P("duration", "number", "Duration in seconds (default 5)"),
                    P("start_speed", "number", "Start speed (default 5)"),
                    P("start_size", "number", "Start size (default 1)"),
                    P("start_color", "object", "Start color {r, g, b, a}"),
                    P("rate_over_time", "number", "Emission rate per second (default 10)"),
                    P("max_particles", "number", "Max particles (default 1000)"))),
                T("create_audio_source", "Create an AudioSource on a GameObject", O(
                    P("path", "string", "GameObject path"),
                    P("instance_id", "number", "Unity instance ID"),
                    P("audio_clip_path", "string", "AudioClip asset path"),
                    P("audio_clip_guid", "string", "AudioClip GUID"),
                    P("spatial_blend", "number", "3D spatial blend 0=2D 1=3D (default 0)"),
                    P("loop", "boolean", "Loop (default false)"),
                    P("play_on_awake", "boolean", "Play on awake (default true)"),
                    P("volume", "number", "Volume 0-1 (default 1)"),
                    P("pitch", "number", "Pitch (default 1)"))),
                T("create_animator_controller", "Create an Animator Controller asset", O(
                    P("path", "string", "Target path (e.g. Assets/Animations/MyController.controller)"),
                    P("default_state", "string", "Default state name"))),
                T("refresh_and_compile", "Force AssetDatabase refresh and script compilation", O(
                    P("wait", "boolean", "Wait for compilation to finish (default false)"))),
                T("build_project", "Build the project for a target platform", O(
                    P("target", "string", "Build target", E("StandaloneWindows64", "StandaloneWindows", "StandaloneOSX", "StandaloneLinux64", "Android", "iOS", "WebGL")),
                    P("output_path", "string", "Build output path (default Builds/)"),
                    P("scenes", "array", "Array of scene paths to include (default: all enabled in Build Settings)"),
                    P("development", "boolean", "Development build (default false)"),
                    P("clean", "boolean", "Clean build (delete existing)"))),
                T("get_console_logs", "Get recent Unity Console log entries", O(
                    P("count", "number", "Number of entries (default 20)"),
                    P("mode", "string", "Filter mode", E("all", "error", "warning", "message")))),
            };
        }

        private static JObject T(string name, string desc, JObject schema)
        {
            return new JObject
            {
                ["name"] = name,
                ["description"] = desc,
                ["inputSchema"] = schema
            };
        }

        private static JObject O(params JObject[] props)
        {
            var p = new JObject();
            var r = new JArray();
            foreach (var prop in props)
            {
                p[prop["name"].ToString()] = prop["def"];
                if (prop["required"]?.Value<bool>() == true)
                    r.Add(prop["name"].ToString());
            }
            var result = new JObject { ["type"] = "object", ["properties"] = p };
            if (r.Count > 0)
                result["required"] = r;
            return result;
        }

        private static JObject P(string name, string type, string desc, JArray values = null)
        {
            var def = new JObject { ["type"] = type, ["description"] = desc };
            if (values != null) def["enum"] = values;
            return new JObject { ["name"] = name, ["def"] = def };
        }

        private static JObject Req(JObject prop)
        {
            prop["required"] = true;
            return prop;
        }

        private static JArray E(params string[] values)
        {
            return new JArray(values);
        }

        private static GameObject ResolveGameObject(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            var instanceId = jparams["instance_id"]?.Value<int>();

            if (instanceId.HasValue && instanceId.Value != 0)
            {
#pragma warning disable 0618
                var obj = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
#pragma warning restore 0618
                if (obj != null) return obj;
            }

            if (!string.IsNullOrEmpty(path))
            {
                var go = GameObject.Find(path);
                if (go != null) return go;

                go = GameObject.Find("/" + path);
                if (go != null) return go;
            }

            return null;
        }

        private static string GetGameObjectPath(GameObject go)
        {
            var path = go.name;
            var parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        private static JObject SerializeGameObject(GameObject go)
        {
            var components = go.GetComponents<Component>()
                .Where(c => c != null)
                .Select(c => new JObject
                {
                    ["type"] = c.GetType().Name,
                    ["enabled"] = (c is Behaviour b) ? b.enabled : (JToken)true
                });

            return new JObject
            {
                ["name"] = go.name,
                ["path"] = GetGameObjectPath(go),
                ["instance_id"] = go.GetInstanceID(),
                ["active"] = go.activeSelf,
                ["tag"] = go.tag,
                ["layer"] = go.layer,
                ["components"] = new JArray(components),
                ["position"] = new JObject { ["x"] = go.transform.position.x, ["y"] = go.transform.position.y, ["z"] = go.transform.position.z },
                ["rotation"] = new JObject { ["x"] = go.transform.eulerAngles.x, ["y"] = go.transform.eulerAngles.y, ["z"] = go.transform.eulerAngles.z },
                ["scale"] = new JObject { ["x"] = go.transform.localScale.x, ["y"] = go.transform.localScale.y, ["z"] = go.transform.localScale.z },
                ["child_count"] = go.transform.childCount,
            };
        }

        // ─── Tool: get_scene_hierarchy ───

        private static JToken GetSceneHierarchy(JObject jparams)
        {
            var rootGOs = SceneManager.GetActiveScene().GetRootGameObjects()
                .OrderBy(go => go.transform.GetSiblingIndex());

            var roots = new JArray();
            foreach (var go in rootGOs)
            {
                roots.Add(BuildHierarchyNode(go));
            }

            return new JObject
            {
                ["scene"] = SceneManager.GetActiveScene().name,
                ["game_object_count"] = Resources.FindObjectsOfTypeAll<GameObject>()
                    .Count(go => go.scene.isLoaded && !go.hideFlags.HasFlag(HideFlags.HideInHierarchy)),
                ["roots"] = roots
            };
        }

        private static JObject BuildHierarchyNode(GameObject go)
        {
            var children = new JArray();
            for (var i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i).gameObject;
                children.Add(BuildHierarchyNode(child));
            }

            return new JObject
            {
                ["name"] = go.name,
                ["instance_id"] = go.GetInstanceID(),
                ["active"] = go.activeSelf,
                ["children"] = children
            };
        }

        // ─── Tool: get_game_object_info ───

        private static JToken GetGameObjectInfo(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null)
                return new JObject { ["error"] = "GameObject not found" };

            var includeProperties = jparams["include_properties"]?.Value<bool>() ?? true;

            var comps = new JArray();
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                var cjo = new JObject
                {
                    ["type"] = c.GetType().FullName,
                    ["enabled"] = (c is Behaviour b) ? b.enabled : (JToken)true,
                };

                if (includeProperties)
                {
                    using var so = new SerializedObject(c);
                    var prop = so.GetIterator();
                    var props = new JObject();
                    if (prop.NextVisible(true))
                    {
                        do
                        {
                            props[prop.propertyPath] = SerializePropertyValue(prop);
                        } while (prop.NextVisible(false));
                    }
                    cjo["properties"] = props;
                }

                comps.Add(cjo);
            }

            return new JObject
            {
                ["name"] = go.name,
                ["path"] = GetGameObjectPath(go),
                ["instance_id"] = go.GetInstanceID(),
                ["active"] = go.activeSelf,
                ["tag"] = go.tag,
                ["layer"] = go.layer,
                ["transform"] = new JObject
                {
                    ["position"] = Vec3ToJObject(go.transform.position),
                    ["local_position"] = Vec3ToJObject(go.transform.localPosition),
                    ["rotation"] = Vec3ToJObject(go.transform.eulerAngles),
                    ["local_rotation"] = Vec3ToJObject(go.transform.localEulerAngles),
                    ["local_scale"] = Vec3ToJObject(go.transform.localScale),
                },
                ["components"] = comps,
            };
        }

        private static JObject Vec3ToJObject(Vector3 v) => new()
        {
            ["x"] = v.x, ["y"] = v.y, ["z"] = v.z
        };

        private static JToken SerializePropertyValue(SerializedProperty prop)
        {
            return prop.propertyType switch
            {
                SerializedPropertyType.Integer => prop.intValue,
                SerializedPropertyType.Boolean => prop.boolValue,
                SerializedPropertyType.Float => prop.floatValue,
                SerializedPropertyType.String => prop.stringValue,
                SerializedPropertyType.Color => new JObject { ["r"] = prop.colorValue.r, ["g"] = prop.colorValue.g, ["b"] = prop.colorValue.b, ["a"] = prop.colorValue.a },
                SerializedPropertyType.Vector3 => new JObject { ["x"] = prop.vector3Value.x, ["y"] = prop.vector3Value.y, ["z"] = prop.vector3Value.z },
                SerializedPropertyType.Vector2 => new JObject { ["x"] = prop.vector2Value.x, ["y"] = prop.vector2Value.y },
                SerializedPropertyType.Enum => prop.enumDisplayNames[prop.enumValueIndex],
                SerializedPropertyType.ObjectReference => prop.objectReferenceValue?.name ?? "None",
                _ => prop.boxedValue?.ToString() ?? prop.propertyType.ToString()
            };
        }

        // ─── Tool: find_game_objects ───

        private static JToken FindGameObjects(JObject jparams)
        {
            var nameFilter = jparams["name"]?.ToString() ?? "";
            var maxResults = jparams["max_results"]?.Value<int>() ?? 50;

            var allGOs = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(go => go.scene.isLoaded && !go.hideFlags.HasFlag(HideFlags.HideInHierarchy));

            if (!string.IsNullOrEmpty(nameFilter))
            {
                allGOs = allGOs.Where(go =>
                    go.name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var results = new JArray();
            foreach (var go in allGOs.Take(maxResults))
            {
                results.Add(new JObject
                {
                    ["name"] = go.name,
                    ["path"] = GetGameObjectPath(go),
                    ["instance_id"] = go.GetInstanceID(),
                });
            }

            return new JObject { ["count"] = results.Count, ["results"] = results };
        }

        // ─── Tool: create_game_object ───

        private static JToken CreateGameObject(JObject jparams)
        {
            var name = jparams["name"]?.ToString() ?? "New GameObject";
            var type = jparams["type"]?.ToString() ?? "empty";
            var parentPath = jparams["parent_path"]?.ToString();

            GameObject go;
            Undo.IncrementCurrentGroup();
            var groupIndex = Undo.GetCurrentGroup();

            if (type == "primitive")
            {
                var primitiveType = jparams["primitive_type"]?.ToString() ?? "Cube";
                if (!Enum.TryParse<PrimitiveType>(primitiveType, true, out var pt))
                    pt = PrimitiveType.Cube;
                go = GameObject.CreatePrimitive(pt);
                go.name = name;
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            }
            else
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            }

            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = GameObject.Find(parentPath);
                if (parent != null)
                {
                    Undo.SetTransformParent(go.transform, parent.transform, $"Parent {name}");
                }
            }

            Undo.CollapseUndoOperations(groupIndex);

            if (Selection.activeTransform == null || jparams["select"]?.Value<bool>() == true)
            {
                Selection.activeGameObject = go;
                SceneView.FrameLastActiveSceneView();
            }

            return SerializeGameObject(go);
        }

        // ─── Tool: delete_game_object ───

        private static JToken DeleteGameObject(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return new JObject { ["error"] = "GameObject not found", ["success"] = false };

            Undo.DestroyObjectImmediate(go);
            return new JObject { ["success"] = true, ["deleted"] = jparams["path"] ?? jparams["instance_id"] };
        }

        // ─── Transform tools ───

        private static JToken SetTransformPosition(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");
            if (jparams["x"] == null && jparams["y"] == null && jparams["z"] == null)
                return ErrorResult("Provide x, y, z values");

            var space = jparams["space"]?.ToString() == "local" ? Space.Self : Space.World;
            var pos = space == Space.World ? go.transform.position : go.transform.localPosition;

            Undo.RecordObject(go.transform, "Set Position");
            if (space == Space.World)
                go.transform.position = new Vector3(
                    jparams["x"]?.Value<float>() ?? pos.x,
                    jparams["y"]?.Value<float>() ?? pos.y,
                    jparams["z"]?.Value<float>() ?? pos.z);
            else
                go.transform.localPosition = new Vector3(
                    jparams["x"]?.Value<float>() ?? pos.x,
                    jparams["y"]?.Value<float>() ?? pos.y,
                    jparams["z"]?.Value<float>() ?? pos.z);

            EditorSceneManager.MarkSceneDirty(go.scene);
            return SuccessResult();
        }

        private static JToken SetTransformRotation(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            Undo.RecordObject(go.transform, "Set Rotation");

            if (jparams["x"] != null || jparams["y"] != null || jparams["z"] != null)
            {
                var euler = go.transform.eulerAngles;
                go.transform.eulerAngles = new Vector3(
                    jparams["x"]?.Value<float>() ?? euler.x,
                    jparams["y"]?.Value<float>() ?? euler.y,
                    jparams["z"]?.Value<float>() ?? euler.z);
            }

            EditorSceneManager.MarkSceneDirty(go.scene);
            return SuccessResult();
        }

        private static JToken SetTransformScale(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            Undo.RecordObject(go.transform, "Set Scale");
            var scale = go.transform.localScale;
            go.transform.localScale = new Vector3(
                jparams["x"]?.Value<float>() ?? scale.x,
                jparams["y"]?.Value<float>() ?? scale.y,
                jparams["z"]?.Value<float>() ?? scale.z);

            EditorSceneManager.MarkSceneDirty(go.scene);
            return SuccessResult();
        }

        // ─── Tool: add_component ───

        private static JToken AddComponent(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            var typeName = jparams["type"]?.ToString();
            if (string.IsNullOrEmpty(typeName)) return ErrorResult("Provide component type name");

            var type = ResolveType(typeName);
            if (type == null) return ErrorResult($"Component type '{typeName}' not found");

            var comp = Undo.AddComponent(go, type);
            return new JObject
            {
                ["success"] = true,
                ["type"] = type.Name,
                ["instance_id"] = comp.GetInstanceID()
            };
        }

        // ─── Tool: remove_component ───

        private static JToken RemoveComponent(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            var typeName = jparams["type"]?.ToString();
            if (string.IsNullOrEmpty(typeName)) return ErrorResult("Provide component type name");

            var type = ResolveType(typeName);
            if (type == null) return ErrorResult($"Component type '{typeName}' not found");

            var comp = go.GetComponent(type);
            if (comp == null) return ErrorResult($"Component '{typeName}' not found on GameObject");

            Undo.DestroyObjectImmediate(comp);
            return SuccessResult();
        }

        // ─── Tool: set_parent ───

        private static JToken SetParent(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            var parentPath = jparams["parent_path"]?.ToString();
            var worldPositionStays = jparams["world_position_stays"]?.Value<bool>() ?? true;

            if (string.IsNullOrEmpty(parentPath))
            {
                Undo.SetTransformParent(go.transform, null, "Detach GameObject");
            }
            else
            {
                var parent = GameObject.Find(parentPath);
                if (parent == null) return ErrorResult($"Parent '{parentPath}' not found");
                Undo.SetTransformParent(go.transform, parent.transform, "Reparent GameObject");
            }

            EditorSceneManager.MarkSceneDirty(go.scene);
            return SuccessResult();
        }

        // ─── Tool: set_active ───

        private static JToken SetActive(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            var active = jparams["active"]?.Value<bool>();
            if (active == null) return ErrorResult("Provide 'active' boolean");

            Undo.RecordObject(go, "Set Active");
            go.SetActive(active.Value);
            EditorSceneManager.MarkSceneDirty(go.scene);
            return SuccessResult();
        }

        // ─── Tool: duplicate_game_object ───

        private static JToken DuplicateGameObject(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            Undo.IncrementCurrentGroup();
            var clone = Object.Instantiate(go, go.transform.parent);
            clone.name = go.name + " (Clone)";
            Undo.RegisterCreatedObjectUndo(clone, $"Duplicate {go.name}");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            Selection.activeGameObject = clone;
            return SerializeGameObject(clone);
        }

        // ─── Tool: set_property ───

        private static JToken SetProperty(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            var componentType = jparams["component_type"]?.ToString();
            var propertyPath = jparams["property"]?.ToString();
            var value = jparams["value"];

            if (string.IsNullOrEmpty(componentType)) return ErrorResult("Provide 'component_type'");
            if (string.IsNullOrEmpty(propertyPath)) return ErrorResult("Provide 'property' path");
            if (value == null) return ErrorResult("Provide 'value'");

            var type = ResolveType(componentType);
            if (type == null) return ErrorResult($"Component type '{componentType}' not found");

            var comp = go.GetComponent(type);
            if (comp == null) return ErrorResult($"Component '{componentType}' not found on GameObject");

            using var so = new SerializedObject(comp);
            var prop = so.FindProperty(propertyPath);
            if (prop == null) return ErrorResult($"Property '{propertyPath}' not found on {componentType}");

            Undo.RecordObject(comp, $"Set {propertyPath}");

            if (!ApplyValue(prop, value))
                return ErrorResult($"Failed to apply value to property '{propertyPath}'");

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(go.scene);
            return SuccessResult();
        }

        private static bool ApplyValue(SerializedProperty prop, JToken value)
        {
            try
            {
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Float:
                        prop.floatValue = value.Value<float>();
                        return true;
                    case SerializedPropertyType.Integer:
                        prop.intValue = value.Value<int>();
                        return true;
                    case SerializedPropertyType.Boolean:
                        prop.boolValue = value.Value<bool>();
                        return true;
                    case SerializedPropertyType.String:
                        prop.stringValue = value.ToString();
                        return true;
                    case SerializedPropertyType.Color:
                        prop.colorValue = new Color(
                            value["r"]?.Value<float>() ?? 0,
                            value["g"]?.Value<float>() ?? 0,
                            value["b"]?.Value<float>() ?? 0,
                            value["a"]?.Value<float>() ?? 1);
                        return true;
                    case SerializedPropertyType.Vector2:
                        prop.vector2Value = new Vector2(
                            value["x"]?.Value<float>() ?? 0,
                            value["y"]?.Value<float>() ?? 0);
                        return true;
                    case SerializedPropertyType.Vector3:
                        prop.vector3Value = new Vector3(
                            value["x"]?.Value<float>() ?? 0,
                            value["y"]?.Value<float>() ?? 0,
                            value["z"]?.Value<float>() ?? 0);
                        return true;
                    case SerializedPropertyType.Enum:
                        var names = prop.enumDisplayNames;
                        var val = value.ToString();
                        for (var i = 0; i < names.Length; i++)
                        {
                            if (string.Equals(names[i], val, StringComparison.OrdinalIgnoreCase))
                            {
                                prop.enumValueIndex = i;
                                return true;
                            }
                        }
                        return false;
                    case SerializedPropertyType.ObjectReference:
                        if (value.Type == JTokenType.Null)
                        {
                            prop.objectReferenceValue = null;
                            return true;
                        }
                        if (value is JObject sceneObj && sceneObj["instance_id"] != null)
                        {
                            var instanceId = sceneObj["instance_id"]!.Value<int>();
#pragma warning disable 0618
                            var sceneObjectRef = EditorUtility.InstanceIDToObject(instanceId);
#pragma warning restore 0618
                            if (sceneObjectRef != null)
                            {
                                prop.objectReferenceValue = sceneObjectRef;
                                return true;
                            }
                        }
                        if (value is JObject refObj)
                        {
                            var guid = refObj["guid"]?.ToString();
                            var path = refObj["path"]?.ToString();
                            var componentTypeName = refObj["component_type"]?.ToString();
                            if (!string.IsNullOrEmpty(guid))
                                path = AssetDatabase.GUIDToAssetPath(guid);
                            if (!string.IsNullOrEmpty(path))
                            {
                                if (!string.IsNullOrEmpty(componentTypeName))
                                {
                                    var go = GameObject.Find(path) ?? GameObject.Find("/" + path);
                                    if (go != null)
                                    {
                                        var componentType = ResolveType(componentTypeName);
                                        if (componentType != null)
                                        {
                                            var component = go.GetComponent(componentType);
                                            if (component != null)
                                            {
                                                prop.objectReferenceValue = component;
                                                return true;
                                            }
                                        }
                                    }
                                }

                                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                                if (asset != null)
                                {
                                    prop.objectReferenceValue = asset;
                                    return true;
                                }

                                var sceneGo = GameObject.Find(path) ?? GameObject.Find("/" + path);
                                if (sceneGo != null)
                                {
                                    prop.objectReferenceValue = sceneGo;
                                    return true;
                                }
                            }
                        }
                        if (value.Type == JTokenType.String)
                        {
                            var s = value.ToString();
                            var asset = ResolveAsset(s);
                            if (asset != null)
                            {
                                prop.objectReferenceValue = asset;
                                return true;
                            }

                            var sceneGo = GameObject.Find(s) ?? GameObject.Find("/" + s);
                            if (sceneGo != null)
                            {
                                prop.objectReferenceValue = sceneGo;
                                return true;
                            }
                        }
                        return false;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        // ─── Tool: rename_game_object ───

        private static JToken RenameGameObject(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            var newName = jparams["name"]?.ToString();
            if (string.IsNullOrEmpty(newName)) return ErrorResult("Provide new 'name'");

            Undo.RecordObject(go, "Rename");
            go.name = newName;
            EditorSceneManager.MarkSceneDirty(go.scene);
            return SuccessResult();
        }

        // ─── Tool: set_tag ───

        private static JToken SetTag(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            var tag = jparams["tag"]?.ToString();
            if (string.IsNullOrEmpty(tag)) return ErrorResult("Provide 'tag' value");

            Undo.RecordObject(go, "Set Tag");
            go.tag = tag;
            EditorSceneManager.MarkSceneDirty(go.scene);
            return SuccessResult();
        }

        // ─── Tool: set_layer ───

        private static JToken SetLayer(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            var layerName = jparams["layer"]?.ToString();
            if (!string.IsNullOrEmpty(layerName))
            {
                var layer = LayerMask.NameToLayer(layerName);
                if (layer == -1) return ErrorResult($"Layer '{layerName}' not found");
                Undo.RecordObject(go, "Set Layer");
                go.layer = layer;
            }
            else if (jparams["layer_index"] != null)
            {
                Undo.RecordObject(go, "Set Layer");
                go.layer = jparams["layer_index"].Value<int>();
            }
            else
            {
                return ErrorResult("Provide 'layer' (name) or 'layer_index'");
            }

            EditorSceneManager.MarkSceneDirty(go.scene);
            return SuccessResult();
        }

        // ─── Tool: set_static_flags ───

        private static JToken SetStaticFlags(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            if (jparams["flags"] == null) return ErrorResult("Provide 'flags' array of strings");

            StaticEditorFlags flags = 0;
            foreach (var flag in jparams["flags"])
            {
                var f = flag.ToString();
                if (Enum.TryParse<StaticEditorFlags>(f, true, out var parsed))
                    flags |= parsed;
                else if (string.Equals(f, "All", StringComparison.OrdinalIgnoreCase))
                    flags = (StaticEditorFlags)(-1);
            }

            Undo.RecordObject(go, "Set Static Flags");
            GameObjectUtility.SetStaticEditorFlags(go, flags);
            EditorSceneManager.MarkSceneDirty(go.scene);
            return SuccessResult();
        }

        // ─── Tool: select_and_frame ───

        private static JToken SelectAndFrame(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            Selection.activeGameObject = go;
            SceneView.FrameLastActiveSceneView();
            EditorGUIUtility.PingObject(go);

            return new JObject { ["success"] = true, ["selected"] = go.name };
        }

        // ─── Tool: find_assets ───

        private static JToken FindAssets(JObject jparams)
        {
            var filter = new List<string>();
            if (!string.IsNullOrEmpty(jparams["name"]?.ToString()))
                filter.Add(jparams["name"].ToString());
            if (!string.IsNullOrEmpty(jparams["type"]?.ToString()))
                filter.Add($"t:{jparams["type"]}");
            if (!string.IsNullOrEmpty(jparams["path"]?.ToString()))
                filter.Add(jparams["path"].ToString());

            var filterStr = filter.Count > 0 ? string.Join(" ", filter) : "";
            var maxResults = jparams["max_results"]?.Value<int>() ?? 50;
            var sortByName = jparams["sort_by_name"]?.Value<bool>() ?? false;

            var guids = AssetDatabase.FindAssets(filterStr, null);
            var results = new JArray();

            foreach (var guid in guids.Take(maxResults))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path);

                results.Add(new JObject
                {
                    ["name"] = name,
                    ["path"] = path,
                    ["guid"] = guid,
                    ["type"] = AssetDatabase.GetMainAssetTypeAtPath(path)?.Name ?? "Unknown",
                });
            }

            return new JObject { ["count"] = results.Count, ["results"] = results };
        }

        // ─── Tool: get_asset_info ───

        private static JToken GetAssetInfo(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            var guid = jparams["guid"]?.ToString();

            if (!string.IsNullOrEmpty(guid))
                path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
                return ErrorResult("Provide 'path' or 'guid'");

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
                return ErrorResult($"Asset not found at '{path}'");

            var info = new JObject
            {
                ["name"] = asset.name,
                ["path"] = path,
                ["guid"] = AssetDatabase.AssetPathToGUID(path),
                ["type"] = asset.GetType().FullName,
                ["is_folder"] = AssetDatabase.IsValidFolder(path),
            };

            if (asset is GameObject prefab)
            {
                var prefabType = PrefabUtility.GetPrefabAssetType(prefab);
                info["prefab_type"] = prefabType.ToString();

                var roots = new JArray();
                foreach (Transform child in prefab.transform)
                    roots.Add(child.name);
                info["root_children"] = roots;
            }

            if (asset is Material mat)
            {
                info["shader"] = mat.shader?.name;
                info["color"] = mat.color != Color.white
                    ? new JObject { ["r"] = mat.color.r, ["g"] = mat.color.g, ["b"] = mat.color.b, ["a"] = mat.color.a }
                    : null;
            }

            if (asset is Texture2D tex)
            {
                info["dimensions"] = new JObject { ["width"] = tex.width, ["height"] = tex.height };
                info["format"] = tex.format.ToString();
            }

            return info;
        }

        // ─── Tool: instantiate_prefab ───

        private static JToken InstantiatePrefab(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            var guid = jparams["guid"]?.ToString();

            if (!string.IsNullOrEmpty(guid))
                path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
                return ErrorResult("Provide 'path' or 'guid' to the prefab/asset");

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
                return ErrorResult($"Asset not found at '{path}'");

            var parentPath = jparams["parent_path"]?.ToString();
            var position = jparams["position"] as JObject;
            var name = jparams["name"]?.ToString();

            Undo.IncrementCurrentGroup();

            GameObject instance;
            if (asset is GameObject prefab)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
            else
            {
                instance = (GameObject)Object.Instantiate(asset);
                if (instance is GameObject go)
                    instance = go;
            }

            if (!string.IsNullOrEmpty(name))
                instance.name = name;

            Undo.RegisterCreatedObjectUndo(instance, $"Instantiate {instance.name}");

            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = GameObject.Find(parentPath);
                if (parent != null)
                    instance.transform.SetParent(parent.transform);
            }

            if (position != null)
            {
                instance.transform.position = new Vector3(
                    position["x"]?.Value<float>() ?? 0,
                    position["y"]?.Value<float>() ?? 0,
                    position["z"]?.Value<float>() ?? 0);
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            Selection.activeGameObject = instance;

            return SerializeGameObject(instance);
        }

        // ─── Tool: save_prefab ───

        private static JToken SavePrefab(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            var prefabPath = jparams["prefab_path"]?.ToString();
            if (string.IsNullOrEmpty(prefabPath))
            {
                prefabPath = "Assets/" + go.name + ".prefab";
                var dir = Path.GetDirectoryName(prefabPath);
                if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
                    prefabPath = "Assets/" + go.name + ".prefab";
            }

            if (!prefabPath.StartsWith("Assets/"))
                return ErrorResult("Path must be within Assets/");

            Undo.IncrementCurrentGroup();

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null && PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.Connected)
            {
                PrefabUtility.ApplyPrefabInstance(go, InteractionMode.UserAction);
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                return new JObject { ["success"] = true, ["prefab_path"] = prefabPath, ["mode"] = "updated" };
            }
            else
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                if (prefab == null)
                    return ErrorResult("Failed to save prefab");
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            AssetDatabase.Refresh();

            return new JObject { ["success"] = true, ["prefab_path"] = prefabPath, ["mode"] = "created" };
        }

        // ─── Tool: create_material ───

        private static JToken CreateMaterial(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            if (string.IsNullOrEmpty(path))
                path = "Assets/NewMaterial.mat";

            if (!path.StartsWith("Assets/"))
                return ErrorResult("Path must be within Assets/");

            var shaderName = jparams["shader"]?.ToString() ?? "Standard";
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                var shaders = Resources.FindObjectsOfTypeAll<Shader>();
                shader = shaders.FirstOrDefault(s =>
                    s.name.IndexOf(shaderName, StringComparison.OrdinalIgnoreCase) >= 0);
                if (shader == null)
                    return ErrorResult($"Shader '{shaderName}' not found");
            }

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            var material = new Material(shader);

            if (jparams["color"] is JObject color)
            {
                material.color = new Color(
                    color["r"]?.Value<float>() ?? 1,
                    color["g"]?.Value<float>() ?? 1,
                    color["b"]?.Value<float>() ?? 1,
                    color["a"]?.Value<float>() ?? 1);
            }

            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.Refresh();

            return new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["shader"] = shader.name,
            };
        }

        // ─── Tool: create_folder ───

        private static JToken CreateFolder(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            if (string.IsNullOrEmpty(path))
                return ErrorResult("Provide 'path' for the folder");

            if (!path.StartsWith("Assets/"))
                return ErrorResult("Path must be within Assets/");

            if (AssetDatabase.IsValidFolder(path))
                return new JObject { ["success"] = true, ["path"] = path, ["existing"] = true };

            var guid = AssetDatabase.CreateFolder(Path.GetDirectoryName(path), Path.GetFileName(path));
            if (string.IsNullOrEmpty(guid))
                return ErrorResult($"Failed to create folder at '{path}'");

            AssetDatabase.Refresh();
            return new JObject
            {
                ["success"] = true,
                ["path"] = AssetDatabase.GUIDToAssetPath(guid),
                ["guid"] = guid,
            };
        }

        // ─── Tool: create_script ───

        private static JToken CreateScript(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            if (string.IsNullOrEmpty(path))
                return ErrorResult("Provide 'path' for the script (e.g. Assets/Scripts/MyScript.cs)");

            if (!path.EndsWith(".cs"))
                path += ".cs";
            if (!path.StartsWith("Assets/"))
                return ErrorResult("Path must be within Assets/");

            var template = jparams["template"]?.ToString() ?? "MonoBehaviour";
            var namespaceName = jparams["namespace"]?.ToString();

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            var className = Path.GetFileNameWithoutExtension(path);

            var templateContent = template.ToLower() switch
            {
                "monobehaviour" => $@"using UnityEngine;

{(string.IsNullOrEmpty(namespaceName) ? "" : $"namespace {namespaceName}\n{{")}
public class {className} : MonoBehaviour
{{
    void Start()
    {{

    }}

    void Update()
    {{

    }}
}}
{(string.IsNullOrEmpty(namespaceName) ? "" : "}}")}",

                "scriptableobject" => $@"using UnityEngine;

{(string.IsNullOrEmpty(namespaceName) ? "" : $"namespace {namespaceName}\n{{")}
[CreateAssetMenu(fileName = ""{className}"", menuName = ""ScriptableObjects/{className}"")]
public class {className} : ScriptableObject
{{

}}
{(string.IsNullOrEmpty(namespaceName) ? "" : "}}")}",

                "singleton" => $@"using UnityEngine;

{(string.IsNullOrEmpty(namespaceName) ? "" : $"namespace {namespaceName}\n{{")}
public class {className} : MonoBehaviour
{{
    private static {className} _instance;
    public static {className} Instance
    {{
        get
        {{
            if (_instance == null)
                _instance = FindObjectOfType<{className}>();
            return _instance;
        }}
    }}

    void Awake()
    {{
        if (_instance != null && _instance != this)
        {{
            Destroy(gameObject);
            return;
        }}
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }}
}}
{(string.IsNullOrEmpty(namespaceName) ? "" : "}}")}",

                "weapon" or "guncontroller" => $@"using UnityEngine;
using Zenject;

{(string.IsNullOrEmpty(namespaceName) ? "" : $"namespace {namespaceName}\n{{")}
public class {className} : AbstractGunController
{{
    [SerializeField] private BulletAgent _bulletPrefab;

    protected override void CreateBullet()
    {{
        var bullet = Instantiate(_bulletPrefab, muzzle.position, muzzle.rotation);
        bullet.Launch(muzzle.forward, bulletSpeed, damage);
    }}

    public class Factory : PlaceholderFactory<{className}> {{ }}
}}
{(string.IsNullOrEmpty(namespaceName) ? "" : "}}")}",

                "bulletagent" or "bullet" => $@"using UnityEngine;

{(string.IsNullOrEmpty(namespaceName) ? "" : $"namespace {namespaceName}\n{{")}
public class {className} : AbstractBulletAgent
{{
    [SerializeField] private float _lifeTime = 5f;

    private float _speed;
    private float _damage;
    private Vector3 _direction;
    private float _spawnTime;

    public override void Launch(Vector3 direction, float speed, float damage)
    {{
        _direction = direction.normalized;
        _speed = speed;
        _damage = damage;
        _spawnTime = Time.time;
    }}

    void Update()
    {{
        transform.position += _direction * _speed * Time.deltaTime;

        if (Time.time - _spawnTime > _lifeTime)
            Destroy(gameObject);
    }}

    void OnTriggerEnter(Collider other)
    {{
        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
            damageable.AddDamage(_damage);
        Destroy(gameObject);
    }}
}}
{(string.IsNullOrEmpty(namespaceName) ? "" : "}}")}",

                "uimanager" => $@"using UnityEngine;
using TMPro;

{(string.IsNullOrEmpty(namespaceName) ? "" : $"namespace {namespaceName}\n{{")}
public class {className} : MonoBehaviour
{{
    [SerializeField] private TextMeshProUGUI _label;

    void OnEnable()
    {{
        PlayerRegistry.OnPlayerHealthChanged += OnHealthChanged;
        PlayerRegistry.OnPlayerAmmoChanged += OnAmmoChanged;
    }}

    void OnDisable()
    {{
        PlayerRegistry.OnPlayerHealthChanged -= OnHealthChanged;
        PlayerRegistry.OnPlayerAmmoChanged -= OnAmmoChanged;
    }}

    private void OnHealthChanged(int health, int maxHealth)
    {{
        if (_label != null)
            _label.text = $""{{health}}/{{maxHealth}}"";
    }}

    private void OnAmmoChanged(int current, int max)
    {{
    }}
}}
{(string.IsNullOrEmpty(namespaceName) ? "" : "}}")}",

                _ => $@"using UnityEngine;\n\npublic class {className} : MonoBehaviour {{\n}}\n"
            };

            File.WriteAllText(path, templateContent);
            AssetDatabase.Refresh();

            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            ProjectWindowUtil.ShowCreatedAsset(asset);

            return new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["class_name"] = className,
            };
        }

        // ─── Tool: open_scene ───

        private static JToken OpenScene(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            if (string.IsNullOrEmpty(path))
                return ErrorResult("Provide 'path' to the scene");

            if (!path.StartsWith("Assets/"))
                path = "Assets/" + path;

            var mode = jparams["mode"]?.ToString()?.ToLower() ?? "single";
            var openMode = mode switch
            {
                "additive" => OpenSceneMode.Additive,
                "additivewithoutloading" => OpenSceneMode.AdditiveWithoutLoading,
                _ => OpenSceneMode.Single
            };

            var scene = EditorSceneManager.OpenScene(path, openMode);
            return new JObject
            {
                ["success"] = true,
                ["scene"] = scene.name,
                ["path"] = path,
                ["mode"] = openMode.ToString(),
            };
        }

        // ─── Tool: get_project_structure ───

        private static JToken GetProjectStructure(JObject jparams)
        {
            var rootPath = jparams["path"]?.ToString() ?? "Assets";
            if (!rootPath.StartsWith("Assets/") && rootPath != "Assets")
                return ErrorResult("Path must be within Assets/");

            var maxDepth = jparams["max_depth"]?.Value<int>() ?? 3;
            var includeFiles = jparams["include_files"]?.Value<bool>() ?? true;
            var typeFilter = jparams["type"]?.ToString();

            var root = new JObject
            {
                ["name"] = rootPath,
                ["path"] = rootPath,
            };

            BuildFolderTree(root, rootPath, 0, maxDepth, includeFiles, typeFilter);
            return root;
        }

        private static void BuildFolderTree(JObject parent, string folderPath, int depth, int maxDepth, bool includeFiles, string typeFilter)
        {
            if (depth >= maxDepth) return;

            var dirs = new JArray();
            var files = new JArray();

            foreach (var entry in Directory.GetFileSystemEntries(folderPath).OrderBy(e => e))
            {
                if (entry.EndsWith(".meta")) continue;
                if (entry.EndsWith(".DS_Store")) continue;

                var name = Path.GetFileName(entry);

                if (Directory.Exists(entry))
                {
                    if (name == "Editor" || name.StartsWith(".")) continue;

                    var child = new JObject
                    {
                        ["name"] = name,
                        ["path"] = entry.Replace("\\", "/"),
                    };
                    BuildFolderTree(child, entry, depth + 1, maxDepth, includeFiles, typeFilter);
                    dirs.Add(child);
                }
                else if (includeFiles)
                {
                    var ext = Path.GetExtension(entry)?.ToLower();
                    var assetPath = entry.Replace("\\", "/");

                    if (!string.IsNullOrEmpty(typeFilter))
                    {
                        var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                        if (assetType == null || !assetType.Name.StartsWith(typeFilter, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    files.Add(new JObject
                    {
                        ["name"] = name,
                        ["path"] = assetPath,
                        ["ext"] = ext,
                    });
                }
            }

            parent["folders"] = dirs;
            if (files.Count > 0)
                parent["files"] = files;
        }

        // ─── Tool: save_scene ───

        private static JToken SaveScene(JObject jparams)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            return new JObject { ["success"] = true, ["scene"] = scene.name, ["path"] = scene.path };
        }

        // ─── Tool: get_all_scenes ───

        private static JToken GetAllScenes(JObject jparams)
        {
            var guids = AssetDatabase.FindAssets("t:Scene", null);
            var scenes = new JArray();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                scenes.Add(new JObject
                {
                    ["name"] = Path.GetFileNameWithoutExtension(path),
                    ["path"] = path,
                    ["guid"] = guid,
                });
            }

            return new JObject { ["count"] = scenes.Count, ["scenes"] = scenes };
        }

        // ─── Tool: set_active_scene ───

        private static JToken SetActiveScene(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            var name = jparams["name"]?.ToString();

            Scene target = default;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if ((!string.IsNullOrEmpty(path) && s.path == path) ||
                    (!string.IsNullOrEmpty(name) && s.name == name))
                {
                    target = s;
                    break;
                }
            }

            if (!target.IsValid())
                return ErrorResult("Scene not found (make sure it's already open)");

            SceneManager.SetActiveScene(target);
            return new JObject { ["success"] = true, ["scene"] = target.name };
        }

        // ─── Tool: find_objects_by_component ───

        private static JToken FindObjectsByComponent(JObject jparams)
        {
            var typeName = jparams["type"]?.ToString();
            if (string.IsNullOrEmpty(typeName)) return ErrorResult("Provide component 'type'");

            var type = ResolveType(typeName);
            if (type == null) return ErrorResult($"Type '{typeName}' not found");

            var maxResults = jparams["max_results"]?.Value<int>() ?? 100;

            var results = new JArray();
            var allGOs = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(go => go.scene.isLoaded && !go.hideFlags.HasFlag(HideFlags.HideInHierarchy));

            foreach (var go in allGOs)
            {
                if (results.Count >= maxResults) break;
                var comp = go.GetComponent(type);
                if (comp != null)
                {
                    results.Add(SerializeGameObject(go));
                }
            }

            return new JObject { ["count"] = results.Count, ["results"] = results };
        }

        // ─── Tool: batch_set_property ───

        private static JToken BatchSetProperty(JObject jparams)
        {
            var componentType = jparams["component_type"]?.ToString();
            var propertyPath = jparams["property"]?.ToString();
            var value = jparams["value"];

            if (string.IsNullOrEmpty(componentType)) return ErrorResult("Provide 'component_type'");
            if (string.IsNullOrEmpty(propertyPath)) return ErrorResult("Provide 'property' path");
            if (value == null) return ErrorResult("Provide 'value'");

            var type = ResolveType(componentType);
            if (type == null) return ErrorResult($"Type '{componentType}' not found");

            var allGOs = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(go => go.scene.isLoaded && !go.hideFlags.HasFlag(HideFlags.HideInHierarchy));

            var modified = 0;
            Undo.IncrementCurrentGroup();

            foreach (var go in allGOs)
            {
                var comp = go.GetComponent(type);
                if (comp == null) continue;

                using var so = new SerializedObject(comp);
                var prop = so.FindProperty(propertyPath);
                if (prop == null) continue;

                Undo.RecordObject(comp, $"Batch set {propertyPath}");
                if (ApplyValue(prop, value))
                {
                    so.ApplyModifiedProperties();
                    modified++;
                }
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            EditorSceneManager.MarkAllScenesDirty();

            return new JObject { ["success"] = true, ["modified_count"] = modified };
        }

        // ─── Tool: unpack_prefab ───

        private static JToken UnpackPrefab(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            if (PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.NotAPrefab)
                return ErrorResult("GameObject is not a prefab instance");

            var mode = jparams["mode"]?.ToString()?.ToLower() == "fully" ? PrefabUnpackMode.OutermostRoot : PrefabUnpackMode.OutermostRoot;

            Undo.IncrementCurrentGroup();
            PrefabUtility.UnpackPrefabInstance(go, mode, InteractionMode.UserAction);
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            return new JObject { ["success"] = true };
        }

        // ─── Tool: revert_prefab_overrides ───

        private static JToken RevertPrefabOverrides(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            if (PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.NotAPrefab)
                return ErrorResult("GameObject is not a prefab instance");

            Undo.IncrementCurrentGroup();
            PrefabUtility.RevertPrefabInstance(go, InteractionMode.UserAction);
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            return new JObject { ["success"] = true };
        }

        // ─── Tool: duplicate_asset ───

        private static JToken DuplicateAsset(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            if (string.IsNullOrEmpty(path)) return ErrorResult("Provide 'path'");

            if (!AssetDatabase.LoadMainAssetAtPath(path))
                return ErrorResult($"Asset not found at '{path}'");

            var newPath = jparams["new_path"]?.ToString();
            if (string.IsNullOrEmpty(newPath))
            {
                var dir = Path.GetDirectoryName(path);
                var name = Path.GetFileNameWithoutExtension(path);
                var ext = Path.GetExtension(path);
                newPath = $"{dir}/{name} (Copy){ext}";
            }

            if (!AssetDatabase.CopyAsset(path, newPath))
                return ErrorResult("Failed to duplicate asset");

            AssetDatabase.Refresh();
            return new JObject
            {
                ["success"] = true,
                ["source"] = path,
                ["destination"] = newPath,
            };
        }

        // ─── Tool: delete_asset ───

        private static JToken DeleteAsset(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            if (string.IsNullOrEmpty(path)) return ErrorResult("Provide 'path'");

            if (!AssetDatabase.LoadMainAssetAtPath(path))
                return ErrorResult($"Asset not found at '{path}'");

            AssetDatabase.MoveAssetToTrash(path);
            AssetDatabase.Refresh();
            return new JObject { ["success"] = true, ["deleted"] = path };
        }

        // ─── Tool: move_asset ───

        private static JToken MoveAsset(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            var newPath = jparams["new_path"]?.ToString();
            if (string.IsNullOrEmpty(path)) return ErrorResult("Provide 'path'");
            if (string.IsNullOrEmpty(newPath)) return ErrorResult("Provide 'new_path'");

            var error = AssetDatabase.MoveAsset(path, newPath);
            if (!string.IsNullOrEmpty(error))
                return ErrorResult($"Failed to move asset: {error}");

            AssetDatabase.Refresh();
            return new JObject { ["success"] = true, ["from"] = path, ["to"] = newPath };
        }

        // ─── Tool: create_ui_element ───

        private static JToken CreateUIElement(JObject jparams)
        {
            var uiType = jparams["type"]?.ToString() ?? "Image";
            var parentPath = jparams["parent_path"]?.ToString();

            var parent = !string.IsNullOrEmpty(parentPath)
                ? GameObject.Find(parentPath)?.transform ?? GameObject.Find("Canvas")?.transform
                : GameObject.Find("Canvas")?.transform;

            if (parent == null)
            {
                var canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                canvasObj.layer = 5;
                parent = canvasObj.transform;
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");

                var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }

            var name = jparams["name"]?.ToString() ?? uiType;

            Undo.IncrementCurrentGroup();
            GameObject element;

            switch (uiType.ToLower())
            {
                case "button":
                    element = new GameObject(name, typeof(UnityEngine.UI.Button));
                    AddUIGraphic(element, "Text", typeof(TextMeshProUGUI));
                    break;
                case "text":
                    element = new GameObject(name, typeof(TextMeshProUGUI));
                    break;
                case "image":
                    element = new GameObject(name, typeof(UnityEngine.UI.Image));
                    break;
                case "slider":
                    element = new GameObject(name, typeof(UnityEngine.UI.Slider));
                    break;
                case "panel":
                    element = new GameObject(name, typeof(UnityEngine.UI.Image));
                    break;
                case "inputfield":
                    element = new GameObject(name, typeof(TMP_InputField));
                    var textArea = AddUIGraphic(element, "Text Area", typeof(RectTransform));
                    AddUIGraphic(textArea, "Text", typeof(TextMeshProUGUI));
                    break;
                case "toggle":
                    element = new GameObject(name, typeof(UnityEngine.UI.Toggle));
                    AddUIGraphic(element, "Label", typeof(TextMeshProUGUI));
                    break;
                default:
                    element = new GameObject(name, typeof(UnityEngine.UI.Image));
                    break;
            }

            element.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(element, $"Create {uiType}");

            if (jparams["text"] != null)
            {
                var tmp = element.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null) tmp.text = jparams["text"].ToString();
            }

            var rect = element.GetComponent<RectTransform>();
            if (rect != null && jparams["size"] is JObject size)
            {
                rect.sizeDelta = new Vector2(
                    size["x"]?.Value<float>() ?? rect.sizeDelta.x,
                    size["y"]?.Value<float>() ?? rect.sizeDelta.y);
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            Selection.activeGameObject = element;

            return SerializeGameObject(element);
        }

        private static GameObject AddUIGraphic(GameObject parent, string childName, params Type[] components)
        {
            var child = new GameObject(childName, components);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        // ─── Tool: create_light ───

        private static JToken CreateLight(JObject jparams)
        {
            var lightType = jparams["light_type"]?.ToString() ?? "directional";

            var type = lightType.ToLower() switch
            {
                "directional" => LightType.Directional,
                "point" => LightType.Point,
                "spot" => LightType.Spot,
                "area" => LightType.Rectangle,
                _ => LightType.Directional
            };

            var name = jparams["name"]?.ToString() ?? $"{lightType} Light";
            var go = new GameObject(name, typeof(Light));
            var light = go.GetComponent<Light>();
            light.type = type;

            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");

            if (jparams["color"] is JObject color)
                light.color = new Color(
                    color["r"]?.Value<float>() ?? 1,
                    color["g"]?.Value<float>() ?? 1,
                    color["b"]?.Value<float>() ?? 1);
            if (jparams["intensity"] != null)
                light.intensity = jparams["intensity"].Value<float>();
            if (jparams["range"] != null)
                light.range = jparams["range"].Value<float>();
            if (jparams["shadow"] != null)
                light.shadows = jparams["shadow"].Value<bool>()
                    ? LightShadows.Soft
                    : LightShadows.None;

            var parentPath = jparams["parent_path"]?.ToString();
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = GameObject.Find(parentPath);
                if (parent != null)
                    go.transform.SetParent(parent.transform);
            }

            Selection.activeGameObject = go;
            return SerializeGameObject(go);
        }

        // ─── Tool: create_camera ───

        private static JToken CreateCamera(JObject jparams)
        {
            var name = jparams["name"]?.ToString() ?? "New Camera";
            var go = new GameObject(name, typeof(Camera), typeof(AudioListener));
            var cam = go.GetComponent<Camera>();

            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");

            if (jparams["clear_flags"] != null)
            {
                cam.clearFlags = jparams["clear_flags"].ToString() switch
                {
                    "solid" or "solidcolor" => CameraClearFlags.SolidColor,
                    "skybox" => CameraClearFlags.Skybox,
                    "depth" => CameraClearFlags.Depth,
                    "nothing" => CameraClearFlags.Nothing,
                    _ => CameraClearFlags.Skybox,
                };
            }

            if (jparams["fov"] != null) cam.fieldOfView = jparams["fov"].Value<float>();
            if (jparams["near"] != null) cam.nearClipPlane = jparams["near"].Value<float>();
            if (jparams["far"] != null) cam.farClipPlane = jparams["far"].Value<float>();
            if (jparams["orthographic"] != null)
            {
                cam.orthographic = jparams["orthographic"].Value<bool>();
                cam.orthographicSize = jparams["orthographic_size"]?.Value<float>() ?? 5;
            }
            if (jparams["culling_mask"] != null)
                cam.cullingMask = LayerMask.GetMask(jparams["culling_mask"].ToString().Split(','));

            var parentPath = jparams["parent_path"]?.ToString();
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = GameObject.Find(parentPath);
                if (parent != null)
                    go.transform.SetParent(parent.transform);
            }

            Selection.activeGameObject = go;
            return SerializeGameObject(go);
        }

        // ─── Tool: set_play_mode ───

        private static JToken SetPlayMode(JObject jparams)
        {
            var enter = jparams["enter"]?.Value<bool>();
            if (enter == null) return ErrorResult("Provide 'enter' (true = enter play mode, false = exit)");

            EditorApplication.isPlaying = enter.Value;
            return new JObject { ["success"] = true, ["play_mode"] = enter.Value };
        }

        // ─── Tool: get_all_tags ───

        private static JToken GetAllTags(JObject jparams)
        {
            var tags = new JArray(UnityEditorInternal.InternalEditorUtility.tags);
            return new JObject { ["tags"] = tags };
        }

        // ─── Tool: get_all_layers ───

        private static JToken GetAllLayers(JObject jparams)
        {
            var layers = new JArray();
            for (var i = 0; i < 32; i++)
            {
                var name = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name))
                    layers.Add(new JObject { ["index"] = i, ["name"] = name });
            }
            return new JObject { ["layers"] = layers };
        }

        // ─── Tool: set_material_property ───

        private static JToken SetMaterialProperty(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            var guid = jparams["guid"]?.ToString();

            if (!string.IsNullOrEmpty(guid))
                path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
                return ErrorResult("Provide 'path' or 'guid' to the material");

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                return ErrorResult($"Material not found at '{path}'");

            Undo.RecordObject(material, $"Modify {material.name}");

            if (jparams["color"] is JObject color)
                material.color = new Color(
                    color["r"]?.Value<float>() ?? 1,
                    color["g"]?.Value<float>() ?? 1,
                    color["b"]?.Value<float>() ?? 1,
                    color["a"]?.Value<float>() ?? 1);

            if (jparams["main_texture"] is JObject tex)
            {
                var texPath = tex["path"]?.ToString();
                var texGuid = tex["guid"]?.ToString();
                var texAssetPath = !string.IsNullOrEmpty(texGuid)
                    ? AssetDatabase.GUIDToAssetPath(texGuid)
                    : texPath;
                if (!string.IsNullOrEmpty(texAssetPath))
                    material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture>(texAssetPath);
            }

            if (jparams["float_properties"] is JObject floats)
            {
                foreach (var kv in floats)
                {
                    var floatVal = kv.Value?.Value<float>();
                    if (floatVal.HasValue)
                        material.SetFloat(kv.Key, floatVal.Value);
                }
            }

            if (jparams["shader"] != null)
            {
                var shader = Shader.Find(jparams["shader"].ToString());
                if (shader != null) material.shader = shader;
            }

            if (jparams["render_queue"] != null)
                material.renderQueue = jparams["render_queue"].Value<int>();

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return new JObject { ["success"] = true, ["path"] = path };
        }

        // ─── Tool: set_renderer_material ───

        private static JToken SetRendererMaterial(JObject jparams)
        {
            var go = ResolveGameObject(jparams);
            if (go == null) return ErrorResult("GameObject not found");

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return ErrorResult("GameObject has no Renderer component");

            var materialPath = jparams["material_path"]?.ToString();
            var materialGuid = jparams["material_guid"]?.ToString();
            var slotIndex = jparams["slot"]?.Value<int>() ?? 0;

            var resolvedPath = !string.IsNullOrEmpty(materialGuid)
                ? AssetDatabase.GUIDToAssetPath(materialGuid)
                : materialPath;

            if (string.IsNullOrEmpty(resolvedPath))
                return ErrorResult("Provide 'material_path' or 'material_guid'");

            var material = AssetDatabase.LoadAssetAtPath<Material>(resolvedPath);
            if (material == null)
                return ErrorResult($"Material not found at '{resolvedPath}'");

            Undo.RecordObject(renderer, "Assign Material");

            var sharedMats = renderer.sharedMaterials;
            if (slotIndex < 0 || slotIndex >= sharedMats.Length)
                return ErrorResult($"Slot index {slotIndex} out of range (0-{sharedMats.Length - 1})");

            var applyToAll = jparams["all_slots"]?.Value<bool>() ?? false;

            if (applyToAll)
            {
                var mats = new Material[sharedMats.Length];
                for (var i = 0; i < mats.Length; i++)
                    mats[i] = material;
                renderer.sharedMaterials = mats;
            }
            else
            {
                sharedMats[slotIndex] = material;
                renderer.sharedMaterials = sharedMats;
            }

            EditorSceneManager.MarkSceneDirty(go.scene);
            return new JObject { ["success"] = true, ["slot"] = slotIndex, ["material"] = material.name };
        }

        // ─── Tool: get_asset_dependencies ───

        private static JToken GetAssetDependencies(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            var guid = jparams["guid"]?.ToString();

            if (!string.IsNullOrEmpty(guid))
                path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
                return ErrorResult("Provide 'path' or 'guid'");

            var deps = AssetDatabase.GetDependencies(path, jparams["recursive"]?.Value<bool>() ?? false);

            return new JObject
            {
                ["path"] = path,
                ["dependencies"] = new JArray(deps
                    .Where(d => d != path)
                    .Select(d => new JObject
                    {
                        ["path"] = d,
                        ["name"] = Path.GetFileNameWithoutExtension(d),
                    })),
                ["dependency_count"] = deps.Length - 1,
            };
        }

        // ─── Tool: create_physics_material ───

        private static JToken CreatePhysicsMaterial(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            if (string.IsNullOrEmpty(path))
                return ErrorResult("Provide 'path' (e.g. Assets/Physics/MyMaterial.physicsMaterial)");

            if (!path.StartsWith("Assets/"))
                return ErrorResult("Path must be within Assets/");

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            var mat = new PhysicsMaterial();
            mat.name = Path.GetFileNameWithoutExtension(path);
            mat.staticFriction = jparams["static_friction"]?.Value<float>() ?? 0.6f;
            mat.dynamicFriction = jparams["dynamic_friction"]?.Value<float>() ?? 0.6f;
            mat.bounciness = jparams["bounciness"]?.Value<float>() ?? 0;

            if (jparams["friction_combine"] != null)
                mat.frictionCombine = Enum.TryParse<PhysicsMaterialCombine>(jparams["friction_combine"].ToString(), true, out var fc)
                    ? fc : PhysicsMaterialCombine.Average;

            if (jparams["bounce_combine"] != null)
                mat.bounceCombine = Enum.TryParse<PhysicsMaterialCombine>(jparams["bounce_combine"].ToString(), true, out var bc)
                    ? bc : PhysicsMaterialCombine.Average;

            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.Refresh();
            return new JObject { ["success"] = true, ["path"] = path };
        }

        // ─── Tool: create_particle_system ───

        private static JToken CreateParticleSystem(JObject jparams)
        {
            var name = jparams["name"]?.ToString() ?? "Particle System";
            var go = new GameObject(name, typeof(ParticleSystem));
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;

            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");

            main.loop = jparams["looping"]?.Value<bool>() ?? true;
            main.duration = jparams["duration"]?.Value<float>() ?? 5;
            main.startSpeed = jparams["start_speed"]?.Value<float>() ?? 5;
            main.startSize = jparams["start_size"]?.Value<float>() ?? 1;
            main.maxParticles = jparams["max_particles"]?.Value<int>() ?? 1000;

            if (jparams["start_color"] is JObject color)
                main.startColor = new Color(
                    color["r"]?.Value<float>() ?? 1,
                    color["g"]?.Value<float>() ?? 1,
                    color["b"]?.Value<float>() ?? 1,
                    color["a"]?.Value<float>() ?? 1);

            if (jparams["rate_over_time"] != null)
            {
                emission.rateOverTime = jparams["rate_over_time"].Value<float>();
            }

            var preset = jparams["preset"]?.ToString()?.ToLower();
            switch (preset)
            {
                case "fire":
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color32(255, 200, 50, 255));
                    main.startSpeed = 2;
                    main.startSize = 0.5f;
                    main.maxParticles = 200;
                    emission.rateOverTime = 30;
                    shape.enabled = true;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 15;
                    shape.radius = 0.2f;
                    break;
                case "smoke":
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color32(100, 100, 100, 100));
                    main.startSpeed = 1;
                    main.startSize = 2;
                    main.maxParticles = 100;
                    main.loop = true;
                    main.duration = 5;
                    emission.rateOverTime = 15;
                    break;
                case "sparks":
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color32(255, 255, 200, 255));
                    main.startSpeed = 8;
                    main.startSize = 0.1f;
                    main.maxParticles = 500;
                    main.loop = false;
                    main.duration = 1;
                    emission.rateOverTime = 100;
                    break;
                case "explosion":
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color32(255, 150, 50, 255));
                    main.startSpeed = 10;
                    main.startSize = 1;
                    main.maxParticles = 200;
                    main.loop = false;
                    main.duration = 0.5f;
                    emission.rateOverTime = 400;
                    break;
            }

            var parentPath = jparams["parent_path"]?.ToString();
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = GameObject.Find(parentPath);
                if (parent != null)
                    go.transform.SetParent(parent.transform);
            }

            Selection.activeGameObject = go;
            return SerializeGameObject(go);
        }

        // ─── Tool: create_audio_source ───

        private static JToken CreateAudioSource(JObject jparams)
        {
            GameObject go;

            var path = jparams["path"]?.ToString();
            var instanceId = jparams["instance_id"]?.Value<int>();

            if (instanceId.HasValue && instanceId.Value != 0)
#pragma warning disable 0618
                go = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
#pragma warning restore 0618
            else if (!string.IsNullOrEmpty(path))
                go = GameObject.Find(path);
            else
                go = new GameObject("Audio Source");

            if (go == null) return ErrorResult("GameObject not found");

            var existing = go.GetComponent<AudioSource>();
            if (existing != null)
                return ErrorResult("GameObject already has an AudioSource");

            Undo.IncrementCurrentGroup();
            var audio = Undo.AddComponent<AudioSource>(go);

            var clipPath = jparams["audio_clip_path"]?.ToString();
            var clipGuid = jparams["audio_clip_guid"]?.ToString();
            if (!string.IsNullOrEmpty(clipGuid))
                clipPath = AssetDatabase.GUIDToAssetPath(clipGuid);
            if (!string.IsNullOrEmpty(clipPath))
                audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);

            audio.spatialBlend = jparams["spatial_blend"]?.Value<float>() ?? 0;
            audio.loop = jparams["loop"]?.Value<bool>() ?? false;
            audio.playOnAwake = jparams["play_on_awake"]?.Value<bool>() ?? true;
            audio.volume = jparams["volume"]?.Value<float>() ?? 1;
            audio.pitch = jparams["pitch"]?.Value<float>() ?? 1;

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            EditorSceneManager.MarkSceneDirty(go.scene);
            return SerializeGameObject(go);
        }

        // ─── Tool: create_animator_controller ───

        private static JToken CreateAnimatorController(JObject jparams)
        {
            var path = jparams["path"]?.ToString();
            if (string.IsNullOrEmpty(path))
                return ErrorResult("Provide 'path' (e.g. Assets/Animations/MyController.controller)");

            if (!path.StartsWith("Assets/"))
                return ErrorResult("Path must be within Assets/");

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            UnityEditor.Animations.AnimatorController controller = null;
            try
            {
                controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
            }
            catch (Exception ex)
            {
                return ErrorResult($"Failed to create controller: {ex.Message}");
            }

            if (controller == null)
                return ErrorResult("Failed to create animator controller");

            var defaultState = jparams["default_state"]?.ToString() ?? "Idle";
            if (controller.layers.Length > 0 && controller.layers[0].stateMachine != null)
            {
                var sm = controller.layers[0].stateMachine;
                if (!sm.states.Any(s => s.state.name == defaultState))
                {
                    var state = sm.AddState(defaultState);
                    sm.defaultState = state;
                }
            }

            AssetDatabase.Refresh();
            return new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["layers"] = controller.layers.Length,
            };
        }

        // ─── Tool: refresh_and_compile ───

        private static JToken RefreshAndCompile(JObject jparams)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            var wait = jparams["wait"]?.Value<bool>() ?? false;

            if (wait)
            {
                var t0 = EditorApplication.timeSinceStartup;
                while (EditorApplication.isCompiling && EditorApplication.timeSinceStartup - t0 < 60)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["compiling"] = EditorApplication.isCompiling,
            };
        }

        // ─── Tool: build_project ───

        private static JToken BuildProject(JObject jparams)
        {
            var targetStr = jparams["target"]?.ToString() ?? "StandaloneWindows64";
            if (!Enum.TryParse<BuildTarget>(targetStr, true, out var target))
                target = BuildTarget.StandaloneWindows64;

            var outputPath = jparams["output_path"]?.ToString() ?? "Builds/";
            var development = jparams["development"]?.Value<bool>() ?? false;
            var clean = jparams["clean"]?.Value<bool>() ?? false;

            if (clean && System.IO.Directory.Exists(outputPath))
                System.IO.Directory.Delete(outputPath, true);

            var scenes = jparams["scenes"] as JArray;
            var scenePaths = scenes != null
                ? scenes.Select(s => s.ToString()).ToArray()
                : EditorBuildSettings.scenes
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray();

            if (scenePaths.Length == 0)
                return ErrorResult("No scenes to build");

            var options = BuildOptions.None;
            if (development)
                options |= BuildOptions.Development;

            var report = BuildPipeline.BuildPlayer(scenePaths, outputPath, target, options);

            return new JObject
            {
                ["success"] = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded,
                ["result"] = report.summary.result.ToString(),
                ["output"] = outputPath,
                ["warnings"] = report.summary.totalWarnings,
                ["errors"] = report.summary.totalErrors,
                ["time_seconds"] = report.summary.totalTime.TotalSeconds,
            };
        }

        // ─── Tool: get_console_logs ───

        private static JToken GetConsoleLogs(JObject jparams)
        {
            var count = jparams["count"]?.Value<int>() ?? 20;
            var mode = jparams["mode"]?.ToString()?.ToLower() ?? "all";

            var entries = new JArray();

            try
            {
                var assembly = typeof(EditorApplication).Assembly;
                var logEntriesType = assembly.GetType("UnityEditor.LogEntries");
                if (logEntriesType == null)
                    return new JObject { ["entries"] = entries, ["error"] = "LogEntries API not available" };

                var getCountMethod = logEntriesType.GetMethod("GetCount");
                var getEntryMethod = logEntriesType.GetMethod("GetEntryInternal");
                var startMethod = logEntriesType.GetMethod("StartGettingEntries");
                var endMethod = logEntriesType.GetMethod("EndGettingEntries");

                if (getCountMethod == null || startMethod == null || endMethod == null)
                    return new JObject { ["entries"] = entries };

                startMethod.Invoke(null, null);
                var totalCount = (int)getCountMethod.Invoke(null, null);

                var entryType = assembly.GetType("UnityEditor.LogEntry");
                if (entryType == null) return new JObject { ["entries"] = entries };

                for (var i = Math.Max(0, totalCount - count); i < totalCount; i++)
                {
                    var entry = Activator.CreateInstance(entryType);
                    var result = (bool)getEntryMethod.Invoke(null, new[] { i, entry });

                    if (!result) continue;

                    var condition = entryType.GetField("condition")?.GetValue(entry)?.ToString() ?? "";
                    var file = entryType.GetField("file")?.GetValue(entry)?.ToString() ?? "";
                    var line = entryType.GetField("line")?.GetValue(entry) is int l ? l : 0;
                    var typeVal = entryType.GetField("type")?.GetValue(entry);
                    var logType = typeVal != null ? (LogType)typeVal : LogType.Log;

                    var skip = mode switch
                    {
                        "error" => logType != LogType.Error && logType != LogType.Exception && logType != LogType.Assert,
                        "warning" => logType != LogType.Warning,
                        "message" => logType != LogType.Log,
                        _ => false
                    };
                    if (skip) continue;

                    entries.Add(new JObject
                    {
                        ["type"] = logType.ToString(),
                        ["message"] = condition.Length > 500 ? condition[..500] + "..." : condition,
                        ["file"] = file,
                        ["line"] = line,
                    });

                    if (entries.Count >= count) break;
                }

                endMethod.Invoke(null, null);
            }
            catch { }

            return new JObject { ["entries"] = entries, ["count"] = entries.Count };
        }

        // ─── Helpers ───

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            return Type.GetType(typeName) ??
                   AppDomain.CurrentDomain.GetAssemblies()
                       .Select(a => a.GetType(typeName))
                       .FirstOrDefault(t => t != null);
        }

        private static Object ResolveAsset(string pathOrGuid)
        {
            if (string.IsNullOrEmpty(pathOrGuid)) return null;
            if (pathOrGuid.StartsWith("Assets/") || pathOrGuid.StartsWith("Packages/"))
                return AssetDatabase.LoadMainAssetAtPath(pathOrGuid);
            if (pathOrGuid.Length == 32 && pathOrGuid.All(c => char.IsLetterOrDigit(c) || c == '-'))
                return AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(pathOrGuid));
            return AssetDatabase.LoadMainAssetAtPath(pathOrGuid);
        }

        private static JToken SuccessResult() => new JObject { ["success"] = true };
        private static JToken ErrorResult(string msg) => new JObject { ["success"] = false, ["error"] = msg };
    }
}
