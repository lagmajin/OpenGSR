using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace OpenGS.EditorTools
{
    public sealed class SpriteSheetAutoSliceWindow : EditorWindow
    {
        private const string ReportDir = "Assets/Reports";
        private const string ReportPath = "Assets/Reports/SpriteSheetAutoSliceReport.md";

        private enum SliceMode
        {
            TransparencyIslands = 0,
            GridDivide = 1,
            AutoGridDivide = 2,
        }

        [MenuItem("OpenGSR/Tools/Sprite Sheet Auto Slice")]
        private static void Open()
        {
            var window = GetWindow<SpriteSheetAutoSliceWindow>();
            window.titleContent = new GUIContent("Sprite Slice");
            window.minSize = new Vector2(620, 320);
            window.Show();
        }

        private int _alphaThreshold = 1;
        private int _padding = 2;
        private int _minComponentArea = 16;
        private SliceMode _sliceMode = SliceMode.AutoGridDivide;
        private int _gridColumns = 4;
        private int _gridRows = 1;
        private bool _trimTransparentBorders = true;
        private int _separatorCoveragePercent = 2;
        private bool _onlyProcessNonMultiple = true;
        private bool _keepReadableDuringProcess = true;
        private bool _writeReport = true;
        private Vector2 _scroll;
        private string _lastSummary = "Not processed yet.";
        private string _lastReportPath;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Sprite Sheet Auto Slice", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select textures or folders in the Project window, then auto-slice them into Multiple sprites. Use Grid Divide for evenly spaced button/effect sheets, or Transparency Islands for irregular layouts.",
                MessageType.Info);

            EditorGUILayout.LabelField("Selection", GetSelectionSummary());
            _sliceMode = (SliceMode)EditorGUILayout.EnumPopup("Slice Mode", _sliceMode);
            _alphaThreshold = EditorGUILayout.IntSlider("Alpha Threshold", _alphaThreshold, 0, 254);
            _padding = EditorGUILayout.IntSlider("Padding", _padding, 0, 16);
            _minComponentArea = EditorGUILayout.IntSlider("Min Component Area", _minComponentArea, 1, 128);
            if (_sliceMode == SliceMode.GridDivide)
            {
                _gridColumns = EditorGUILayout.IntSlider("Columns", _gridColumns, 1, 32);
                _gridRows = EditorGUILayout.IntSlider("Rows", _gridRows, 1, 32);
                _trimTransparentBorders = EditorGUILayout.ToggleLeft("Trim transparent borders inside each cell", _trimTransparentBorders);
            }
            if (_sliceMode == SliceMode.AutoGridDivide)
            {
                _separatorCoveragePercent = EditorGUILayout.IntSlider("Separator Coverage %", _separatorCoveragePercent, 0, 25);
                _trimTransparentBorders = EditorGUILayout.ToggleLeft("Trim transparent borders inside each cell", _trimTransparentBorders);
            }
            _onlyProcessNonMultiple = EditorGUILayout.ToggleLeft("Only process textures that are not already Multiple", _onlyProcessNonMultiple);
            _keepReadableDuringProcess = EditorGUILayout.ToggleLeft("Keep texture readable after import", _keepReadableDuringProcess);
            _writeReport = EditorGUILayout.ToggleLeft("Write markdown report", _writeReport);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview", GUILayout.Height(26)))
            {
                Run(applyChanges: false);
            }

            if (GUILayout.Button("Apply", GUILayout.Height(26)))
            {
                Run(applyChanges: true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Output", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.SelectableLabel(_lastSummary, GUILayout.Height(160));
            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(_lastReportPath))
            {
                EditorGUILayout.LabelField("Markdown", _lastReportPath);
            }
        }

        private string GetSelectionSummary()
        {
            return $"{Selection.objects.Length} selected asset(s) or folder(s)";
        }

        private void Run(bool applyChanges)
        {
            if (!Directory.Exists(ReportDir))
            {
                Directory.CreateDirectory(ReportDir);
            }

            var paths = GetSelectedTexturePaths();
            if (paths.Count == 0)
            {
                _lastSummary = "No texture assets or folders selected.";
                return;
            }

            var rows = new List<SliceReportRow>();
            foreach (var path in paths)
            {
                rows.Add(ProcessTexture(path, applyChanges));
            }

            if (_writeReport)
            {
                WriteReport(rows, applyChanges);
                _lastReportPath = ReportPath;
            }

            var processed = rows.Count;
            var totalSlices = rows.Sum(r => r.SliceCount);
            var changed = rows.Count(r => r.Changed);
            var readableChanged = rows.Count(r => r.ReadableChanged);

            _lastSummary =
                $"Textures processed: {processed}\n" +
                $"Total slices: {totalSlices}\n" +
                $"Changed importers: {changed}\n" +
                $"Readable toggled: {readableChanged}\n" +
                $"Mode: {(applyChanges ? "apply" : "preview")}\n" +
                $"Markdown: {(string.IsNullOrEmpty(_lastReportPath) ? "(disabled)" : _lastReportPath)}";
        }

        private SliceReportRow ProcessTexture(string path, bool applyChanges)
        {
            var row = new SliceReportRow
            {
                Path = path,
                Name = Path.GetFileName(path),
                Mode = applyChanges ? "apply" : "preview"
            };

            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                row.Notes = "Not a texture importer.";
                return row;
            }

            var originalReadable = importer.isReadable;
            var originalType = importer.textureType;
            var originalSpriteMode = importer.spriteImportMode;

            if (_onlyProcessNonMultiple && importer.spriteImportMode == SpriteImportMode.Multiple)
            {
                row.Notes = "Already Multiple, skipped.";
                return row;
            }

            var needsReadableToggle = !importer.isReadable;
            var samplingAdjusted = false;
            var applied = false;
            try
            {
                if (needsReadableToggle)
                {
                    importer.isReadable = true;
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                    samplingAdjusted = true;
                    row.ReadableChanged = true;
                }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null)
                {
                    row.Notes = "Texture could not be loaded.";
                    return row;
                }

                var build = BuildSlices(texture, path);
                row.SliceCount = build.Slices.Count;
                row.Slices = build.Slices;
                row.DetectedRows = build.DetectedRows;
                row.DetectedColumns = build.DetectedColumns;

                if (!applyChanges)
                {
                    row.Notes = "Preview only.";
                    return row;
                }

                importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    row.Notes = "Texture importer disappeared during processing.";
                    return row;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                ApplySpriteRects(importer, BuildSpriteRects(build.Slices, path), originalReadable);

                row.Changed = true;
                applied = true;
                row.Notes = _sliceMode == SliceMode.AutoGridDivide
                    ? $"Applied {row.SliceCount} slice(s) from {row.DetectedRows} row(s) x {row.DetectedColumns} column(s)."
                    : $"Applied {row.SliceCount} slice(s).";
            }
            catch (Exception ex)
            {
                row.Notes = ex.Message;
            }
            finally
            {
                if (samplingAdjusted && (!applyChanges || !applied))
                {
                    importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null && importer.isReadable != originalReadable)
                    {
                        importer.isReadable = originalReadable;
                        importer.textureType = originalType;
                        importer.spriteImportMode = originalSpriteMode;
                        importer.SaveAndReimport();
                    }
                }
            }

            return row;
        }

        private List<string> GetSelectedTexturePaths()
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var selection = Selection.objects;

            foreach (var obj in selection)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (AssetDatabase.IsValidFolder(path))
                {
                    var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
                    foreach (var guid in guids)
                    {
                        var texturePath = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(texturePath))
                        {
                            results.Add(texturePath);
                        }
                    }
                    continue;
                }

                if (obj is Texture2D)
                {
                    results.Add(path);
                }
            }

            return results.OrderBy(path => path).ToList();
        }

        private SliceBuildResult BuildSlices(Texture2D texture, string path)
        {
            var width = texture.width;
            var height = texture.height;
            List<RectInt> regions;

            if (_sliceMode == SliceMode.GridDivide)
            {
                regions = BuildGridRegions(texture, width, height, _gridColumns, _gridRows);
            }
            else if (_sliceMode == SliceMode.AutoGridDivide)
            {
                regions = BuildAutoGridRegions(texture, width, height, out var detectedRows, out var detectedColumns);

                var autoSlices = BuildSliceResults(regions, path, width, height);
                return new SliceBuildResult
                {
                    Slices = autoSlices,
                    DetectedRows = detectedRows,
                    DetectedColumns = detectedColumns
                };
            }
            else
            {
                regions = BuildTransparencyRegions(texture, width, height);
            }

            var slices = BuildSliceResults(regions, path, width, height);
            return new SliceBuildResult
            {
                Slices = slices,
                DetectedRows = _sliceMode == SliceMode.GridDivide ? _gridRows : 0,
                DetectedColumns = _sliceMode == SliceMode.GridDivide ? _gridColumns : 0
            };
        }

        private List<SliceResult> BuildSliceResults(List<RectInt> regions, string path, int width, int height)
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            var slices = new List<SliceResult>();
            for (var i = 0; i < regions.Count; i++)
            {
                var rect = ExpandAndClamp(regions[i], width, height, _padding);
                var meta = new SpriteMetaData
                {
                    name = regions.Count == 1 ? stem : $"{stem}_{i + 1:00}",
                    rect = new Rect(rect.x, rect.y, rect.width, rect.height),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero
                };

                slices.Add(new SliceResult
                {
                    Meta = meta,
                    Region = rect
                });
            }

            return slices;
        }

        private static SpriteRect[] BuildSpriteRects(List<SliceResult> slices, string path)
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            var rects = new SpriteRect[slices.Count];

            for (var i = 0; i < slices.Count; i++)
            {
                var meta = slices[i].Meta;
                rects[i] = new SpriteRect
                {
                    name = string.IsNullOrWhiteSpace(meta.name) ? $"{stem}_{i + 1:00}" : meta.name,
                    rect = meta.rect,
                    alignment = (SpriteAlignment)meta.alignment,
                    pivot = meta.pivot,
                    border = meta.border,
                    spriteID = GUID.Generate()
                };
            }

            return rects;
        }

        private void ApplySpriteRects(TextureImporter importer, SpriteRect[] spriteRects, bool originalReadable)
        {
            if (importer == null)
            {
                return;
            }

            var factory = new SpriteDataProviderFactories();
            factory.Init();

            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
            {
                throw new InvalidOperationException("Failed to get SpriteEditorDataProvider from TextureImporter.");
            }

            dataProvider.InitSpriteEditorDataProvider();
            dataProvider.SetSpriteRects(spriteRects);

            var spriteNameFileIdDataProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (spriteNameFileIdDataProvider != null)
            {
                var nameFileIdPairs = spriteRects.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)).ToArray();
                spriteNameFileIdDataProvider.SetNameFileIdPairs(nameFileIdPairs);
            }

            dataProvider.Apply();
            importer.alphaIsTransparency = true;
            importer.isReadable = _keepReadableDuringProcess ? true : originalReadable;
            importer.SaveAndReimport();
        }

        private List<RectInt> BuildGridRegions(Texture2D texture, int width, int height, int columns, int rows)
        {
            var regions = new List<RectInt>();
            var columnEdges = BuildEdges(width, columns);
            var rowEdges = BuildEdges(height, rows);
            var pixels = _trimTransparentBorders ? texture.GetPixels32() : null;

            for (var row = rows - 1; row >= 0; row--)
            {
                for (var col = 0; col < columns; col++)
                {
                    var xMin = columnEdges[col];
                    var xMax = columnEdges[col + 1];
                    var yMin = rowEdges[row];
                    var yMax = rowEdges[row + 1];
                    var rect = new RectInt(xMin, yMin, Mathf.Max(1, xMax - xMin), Mathf.Max(1, yMax - yMin));

                    if (_trimTransparentBorders && pixels != null)
                    {
                        rect = TrimTransparentBorder(rect, width, height, pixels);
                    }

                    regions.Add(rect);
                }
            }

            return regions;
        }

        private List<RectInt> BuildAutoGridRegions(Texture2D texture, int width, int height, out int detectedRows, out int detectedColumns)
        {
            var pixels = texture.GetPixels32();
            var separatorThresholdForColumns = Mathf.Max(0, Mathf.RoundToInt(height * (_separatorCoveragePercent / 100f)));
            var separatorThresholdForRows = Mathf.Max(0, Mathf.RoundToInt(width * (_separatorCoveragePercent / 100f)));

            var verticalSeparators = DetectSeparatorRunsForColumns(pixels, width, height, separatorThresholdForColumns, (byte)_alphaThreshold);
            var horizontalSeparators = DetectSeparatorRunsForRows(pixels, width, height, separatorThresholdForRows, (byte)_alphaThreshold);

            var columns = BuildSegmentsFromSeparators(width, verticalSeparators);
            var rows = BuildSegmentsFromSeparators(height, horizontalSeparators);

            detectedColumns = columns.Count;
            detectedRows = rows.Count;

            if (!LooksLikeSheet(columns, rows))
            {
                detectedColumns = 1;
                detectedRows = 1;
                return new List<RectInt>
                {
                    new RectInt(0, 0, width, height)
                };
            }

            var regions = new List<RectInt>();
            for (var rowIndex = rows.Count - 1; rowIndex >= 0; rowIndex--)
            {
                for (var colIndex = 0; colIndex < columns.Count; colIndex++)
                {
                    regions.Add(new RectInt(
                        columns[colIndex].xMin,
                        rows[rowIndex].xMin,
                        Mathf.Max(1, columns[colIndex].width),
                        Mathf.Max(1, rows[rowIndex].width)));
                }
            }

            if (regions.Count == 0)
            {
                regions.Add(new RectInt(0, 0, width, height));
                detectedColumns = 1;
                detectedRows = 1;
            }

            if (_trimTransparentBorders)
            {
                for (var i = 0; i < regions.Count; i++)
                {
                    regions[i] = TrimTransparentBorder(regions[i], width, height, pixels);
                }
            }

            return regions;
        }

        private static bool LooksLikeSheet(List<RectInt> columns, List<RectInt> rows)
        {
            if (columns == null || rows == null || columns.Count == 0 || rows.Count == 0)
            {
                return false;
            }

            if (!IsUniformSegments(columns) || !IsUniformSegments(rows))
            {
                return false;
            }

            return true;
        }

        private static bool IsUniformSegments(List<RectInt> segments)
        {
            if (segments.Count <= 1)
            {
                return false;
            }

            var min = int.MaxValue;
            var max = int.MinValue;
            foreach (var segment in segments)
            {
                var size = segment.width;
                if (size < min) min = size;
                if (size > max) max = size;
            }

            if (min <= 0)
            {
                return false;
            }

            return max <= Mathf.CeilToInt(min * 1.4f);
        }

        private List<RectInt> BuildTransparencyRegions(Texture2D texture, int width, int height)
        {
            var pixels = texture.GetPixels32();
            var regions = FindOpaqueRegions(pixels, width, height, (byte)_alphaThreshold, _minComponentArea);

            if (regions.Count == 0)
            {
                regions.Add(new RectInt(0, 0, width, height));
            }

            regions = regions
                .OrderByDescending(r => r.yMax)
                .ThenBy(r => r.xMin)
                .ToList();

            return regions;
        }

        private static int[] BuildEdges(int size, int divisions)
        {
            var edges = new int[divisions + 1];
            for (var i = 0; i <= divisions; i++)
            {
                edges[i] = Mathf.RoundToInt(size * (i / (float)divisions));
            }

            edges[0] = 0;
            edges[divisions] = size;
            for (var i = 1; i < edges.Length; i++)
            {
                if (edges[i] < edges[i - 1])
                {
                    edges[i] = edges[i - 1];
                }
            }

            return edges;
        }

        private static List<RectInt> BuildSegmentsFromSeparators(int size, List<IndexRange> separators)
        {
            var segments = new List<RectInt>();
            var cursor = 0;

            foreach (var separator in separators)
            {
                var end = separator.Start - 1;
                if (end >= cursor)
                {
                    segments.Add(new RectInt(cursor, 0, end - cursor + 1, 1));
                }

                cursor = separator.End + 1;
            }

            if (cursor <= size - 1)
            {
                segments.Add(new RectInt(cursor, 0, size - cursor, 1));
            }

            if (segments.Count == 0)
            {
                segments.Add(new RectInt(0, 0, size, 1));
            }

            return segments;
        }

        private static List<IndexRange> DetectSeparatorRunsForColumns(Color32[] pixels, int width, int height, int maxOpaquePixels, byte alphaThreshold)
        {
            var flags = new bool[width];
            for (var x = 0; x < width; x++)
            {
                var opaqueCount = 0;
                for (var y = 0; y < height; y++)
                {
                    if (pixels[y * width + x].a > alphaThreshold)
                    {
                        opaqueCount++;
                        if (opaqueCount > maxOpaquePixels)
                        {
                            break;
                        }
                    }
                }

                flags[x] = opaqueCount <= maxOpaquePixels;
            }

            return BuildRuns(flags);
        }

        private static List<IndexRange> DetectSeparatorRunsForRows(Color32[] pixels, int width, int height, int maxOpaquePixels, byte alphaThreshold)
        {
            var flags = new bool[height];
            for (var y = 0; y < height; y++)
            {
                var opaqueCount = 0;
                for (var x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a > alphaThreshold)
                    {
                        opaqueCount++;
                        if (opaqueCount > maxOpaquePixels)
                        {
                            break;
                        }
                    }
                }

                flags[y] = opaqueCount <= maxOpaquePixels;
            }

            return BuildRuns(flags);
        }

        private static List<IndexRange> BuildRuns(bool[] flags)
        {
            var runs = new List<IndexRange>();
            var start = -1;

            for (var i = 0; i < flags.Length; i++)
            {
                if (flags[i])
                {
                    if (start < 0)
                    {
                        start = i;
                    }
                    continue;
                }

                if (start >= 0)
                {
                    runs.Add(new IndexRange(start, i - 1));
                    start = -1;
                }
            }

            if (start >= 0)
            {
                runs.Add(new IndexRange(start, flags.Length - 1));
            }

            return runs;
        }

        private RectInt TrimTransparentBorder(RectInt rect, int width, int height, Color32[] pixels)
        {
            var left = rect.xMin;
            var right = rect.xMax - 1;
            var bottom = rect.yMin;
            var top = rect.yMax - 1;
            var threshold = (byte)_alphaThreshold;

            while (left <= right && IsColumnTransparent(pixels, width, left, bottom, top, threshold))
            {
                left++;
            }

            while (right >= left && IsColumnTransparent(pixels, width, right, bottom, top, threshold))
            {
                right--;
            }

            while (bottom <= top && IsRowTransparent(pixels, width, bottom, left, right, threshold))
            {
                bottom++;
            }

            while (top >= bottom && IsRowTransparent(pixels, width, top, left, right, threshold))
            {
                top--;
            }

            if (left > right || bottom > top)
            {
                return rect;
            }

            return new RectInt(
                Mathf.Clamp(left, 0, width),
                Mathf.Clamp(bottom, 0, height),
                Mathf.Max(1, right - left + 1),
                Mathf.Max(1, top - bottom + 1));
        }

        private static bool IsColumnTransparent(Color32[] pixels, int width, int x, int yMin, int yMax, byte threshold)
        {
            for (var y = yMin; y <= yMax; y++)
            {
                if (pixels[y * width + x].a > threshold)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsRowTransparent(Color32[] pixels, int width, int y, int xMin, int xMax, byte threshold)
        {
            for (var x = xMin; x <= xMax; x++)
            {
                if (pixels[y * width + x].a > threshold)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<RectInt> FindOpaqueRegions(Color32[] pixels, int width, int height, byte alphaThreshold, int minComponentArea)
        {
            var visited = new bool[pixels.Length];
            var regions = new List<RectInt>();
            var stack = new Stack<int>();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    if (visited[index] || pixels[index].a <= alphaThreshold)
                    {
                        continue;
                    }

                    visited[index] = true;
                    stack.Push(index);

                    var minX = x;
                    var maxX = x;
                    var minY = y;
                    var maxY = y;
                    var area = 0;

                    while (stack.Count > 0)
                    {
                        var current = stack.Pop();
                        var cx = current % width;
                        var cy = current / width;
                        area++;

                        if (cx < minX) minX = cx;
                        if (cx > maxX) maxX = cx;
                        if (cy < minY) minY = cy;
                        if (cy > maxY) maxY = cy;

                        for (var ny = cy - 1; ny <= cy + 1; ny++)
                        {
                            if (ny < 0 || ny >= height)
                            {
                                continue;
                            }

                            for (var nx = cx - 1; nx <= cx + 1; nx++)
                            {
                                if (nx < 0 || nx >= width)
                                {
                                    continue;
                                }

                                if (nx == cx && ny == cy)
                                {
                                    continue;
                                }

                                var nIndex = ny * width + nx;
                                if (visited[nIndex] || pixels[nIndex].a <= alphaThreshold)
                                {
                                    continue;
                                }

                                visited[nIndex] = true;
                                stack.Push(nIndex);
                            }
                        }
                    }

                    if (area >= minComponentArea)
                    {
                        regions.Add(new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1));
                    }
                }
            }

            return regions;
        }

        private static RectInt ExpandAndClamp(RectInt rect, int width, int height, int padding)
        {
            var xMin = Mathf.Clamp(rect.xMin - padding, 0, width);
            var yMin = Mathf.Clamp(rect.yMin - padding, 0, height);
            var xMax = Mathf.Clamp(rect.xMax + padding, 0, width);
            var yMax = Mathf.Clamp(rect.yMax + padding, 0, height);
            return new RectInt(xMin, yMin, Mathf.Max(1, xMax - xMin), Mathf.Max(1, yMax - yMin));
        }

        private void WriteReport(List<SliceReportRow> rows, bool applyChanges)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Sprite Sheet Auto Slice Report");
            sb.AppendLine();
            sb.AppendLine($"- Mode: {(applyChanges ? "Apply" : "Preview")}");
            sb.AppendLine($"- Alpha threshold: {_alphaThreshold}");
            sb.AppendLine($"- Padding: {_padding}");
            sb.AppendLine($"- Min component area: {_minComponentArea}");
            sb.AppendLine();
            sb.AppendLine("| Texture | Slices | Changed | Readable toggled | Notes |");
            sb.AppendLine("| --- | ---: | --- | --- | --- |");

            foreach (var row in rows)
            {
                sb.AppendLine($"| `{row.Path}` | {row.SliceCount} | {(row.Changed ? "yes" : "no")} | {(row.ReadableChanged ? "yes" : "no")} | {EscapeTable(row.Notes)} |");
                if (row.Slices.Count == 0)
                {
                    continue;
                }

                sb.AppendLine();
                sb.AppendLine($"### {row.Name}");
                sb.AppendLine($"- Path: `{row.Path}`");
                if (row.DetectedRows > 0 && row.DetectedColumns > 0)
                {
                    sb.AppendLine($"- Detected grid: {row.DetectedRows} row(s) x {row.DetectedColumns} column(s)");
                }
                sb.AppendLine("| Sprite name | Rect |");
                sb.AppendLine("| --- | --- |");
                foreach (var slice in row.Slices)
                {
                    sb.AppendLine($"| `{slice.Meta.name}` | `{slice.Region.x}, {slice.Region.y}, {slice.Region.width}, {slice.Region.height}` |");
                }
                sb.AppendLine();
            }

            File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
        }

        private static string EscapeTable(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("|", "\\|").Replace("\n", " ").Replace("\r", " ");
        }

        [Serializable]
        private sealed class SliceReportRow
        {
            public string Path;
            public string Name;
            public string Mode;
            public int SliceCount;
            public int DetectedRows;
            public int DetectedColumns;
            public bool Changed;
            public bool ReadableChanged;
            public string Notes;
            public List<SliceResult> Slices = new();
        }

        private readonly struct IndexRange
        {
            public IndexRange(int start, int end)
            {
                Start = start;
                End = end;
            }

            public int Start { get; }
            public int End { get; }
        }

        [Serializable]
        private sealed class SliceResult
        {
            public SpriteMetaData Meta;
            public RectInt Region;
        }

        [Serializable]
        private sealed class SliceBuildResult
        {
            public List<SliceResult> Slices;
            public int DetectedRows;
            public int DetectedColumns;
        }
    }
}
