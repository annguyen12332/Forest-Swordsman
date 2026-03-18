using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class MapPainterWindow : EditorWindow
{
    // --- Grid data ---
    private int gridWidth = 10;
    private int gridHeight = 10;
    private int[,] grid;

    // --- Palette ---
    [System.Serializable]
    private class PaletteEntry
    {
        public int id;
        public string label;
        public Color color;
    }

    private List<PaletteEntry> palette = new List<PaletteEntry>();
    private int selectedPaletteIndex = 0;

    // --- Scroll ---
    private Vector2 gridScroll;
    private Vector2 paletteScroll;

    // --- Cell size ---
    private float cellPixelSize = 28f;

    // --- References ---
    private MapGenerator mapGenerator;

    // --- New palette entry input ---
    private int newId = 1;
    private string newLabel = "Grass";
    private Color newColor = Color.green;

    // --- Brush ---
    private bool isPainting = false;

    [MenuItem("Tools/Map Painter")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<MapPainterWindow>("Map Painter");
        wnd.minSize = new Vector2(420, 500);
    }

    private void OnEnable()
    {
        if (grid == null)
            InitGrid();
        if (palette.Count == 0)
            SetupDefaultPalette();
    }

    private void InitGrid()
    {
        grid = new int[gridHeight, gridWidth];
    }

    private void SetupDefaultPalette()
    {
        palette.Clear();
        palette.Add(new PaletteEntry { id = 0, label = "Empty",  color = new Color(0.2f, 0.2f, 0.2f) });
        palette.Add(new PaletteEntry { id = 1, label = "Grass",  color = new Color(0.3f, 0.8f, 0.3f) });
        palette.Add(new PaletteEntry { id = 2, label = "Water",  color = new Color(0.2f, 0.5f, 1f) });
        palette.Add(new PaletteEntry { id = 3, label = "Tree",   color = new Color(0.1f, 0.5f, 0.1f) });
        palette.Add(new PaletteEntry { id = 4, label = "Enemy",  color = new Color(1f, 0.2f, 0.2f) });
        palette.Add(new PaletteEntry { id = 5, label = "Rock",   color = new Color(0.6f, 0.6f, 0.6f) });
    }

    private void OnGUI()
    {
        // ===== HEADER =====
        EditorGUILayout.Space(4);
        GUILayout.Label("Map Painter", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        DrawGridSizeSection();
        EditorGUILayout.Space(4);
        DrawPaletteSection();
        EditorGUILayout.Space(6);
        DrawGridCanvas();
        EditorGUILayout.Space(6);
        DrawActionsSection();
    }

    // ===== GRID SIZE =====
    private void DrawGridSizeSection()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Grid Size", GUILayout.Width(60));
        int newW = EditorGUILayout.IntField(gridWidth, GUILayout.Width(40));
        EditorGUILayout.LabelField("x", GUILayout.Width(12));
        int newH = EditorGUILayout.IntField(gridHeight, GUILayout.Width(40));
        if (GUILayout.Button("Resize", GUILayout.Width(55)))
        {
            ResizeGrid(Mathf.Clamp(newW, 1, 100), Mathf.Clamp(newH, 1, 100));
        }
        GUILayout.FlexibleSpace();
        cellPixelSize = EditorGUILayout.Slider("Cell", cellPixelSize, 16, 48, GUILayout.Width(180));
        EditorGUILayout.EndHorizontal();
    }

    // ===== PALETTE =====
    private void DrawPaletteSection()
    {
        EditorGUILayout.LabelField("Palette (Click to Select Brush)", EditorStyles.boldLabel);
        paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll, GUILayout.Height(80));
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < palette.Count; i++)
        {
            var entry = palette[i];
            bool isSelected = (i == selectedPaletteIndex);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.normal.textColor = Color.white;
            btnStyle.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = isSelected ? entry.color : entry.color * 0.7f;

            string btnText = entry.id + "\n" + entry.label;
            if (GUILayout.Button(btnText, btnStyle, GUILayout.Width(60), GUILayout.Height(50)))
            {
                selectedPaletteIndex = i;
            }
            GUI.backgroundColor = prevBg;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        // --- Add / Remove palette entry ---
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Add:", GUILayout.Width(30));
        newId = EditorGUILayout.IntField(newId, GUILayout.Width(30));
        newLabel = EditorGUILayout.TextField(newLabel, GUILayout.Width(60));
        newColor = EditorGUILayout.ColorField(newColor, GUILayout.Width(50));
        if (GUILayout.Button("+", GUILayout.Width(24)))
        {
            palette.Add(new PaletteEntry { id = newId, label = newLabel, color = newColor });
        }
        if (GUILayout.Button("-", GUILayout.Width(24)))
        {
            if (selectedPaletteIndex > 0 && selectedPaletteIndex < palette.Count)
            {
                palette.RemoveAt(selectedPaletteIndex);
                selectedPaletteIndex = Mathf.Clamp(selectedPaletteIndex, 0, palette.Count - 1);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // ===== GRID CANVAS =====
    private void DrawGridCanvas()
    {
        EditorGUILayout.LabelField("Grid Canvas (Click or Drag to Paint)", EditorStyles.boldLabel);
        gridScroll = EditorGUILayout.BeginScrollView(gridScroll, GUILayout.ExpandHeight(true));

        if (grid != null)
        {
            Event evt = Event.current;

            for (int y = 0; y < gridHeight; y++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < gridWidth; x++)
                {
                    int cellId = grid[y, x];
                    Color cellColor = GetColorForId(cellId);

                    Rect cellRect = GUILayoutUtility.GetRect(cellPixelSize, cellPixelSize, GUILayout.Width(cellPixelSize), GUILayout.Height(cellPixelSize));

                    EditorGUI.DrawRect(cellRect, cellColor);

                    // Draw border
                    Handles.color = new Color(0, 0, 0, 0.3f);
                    Handles.DrawLine(new Vector3(cellRect.xMin, cellRect.yMin), new Vector3(cellRect.xMax, cellRect.yMin));
                    Handles.DrawLine(new Vector3(cellRect.xMin, cellRect.yMin), new Vector3(cellRect.xMin, cellRect.yMax));

                    // Draw ID number
                    if (cellPixelSize >= 20)
                    {
                        GUIStyle numStyle = new GUIStyle(EditorStyles.miniLabel);
                        numStyle.alignment = TextAnchor.MiddleCenter;
                        numStyle.normal.textColor = Color.white;
                        numStyle.fontSize = Mathf.Clamp((int)(cellPixelSize * 0.35f), 7, 14);
                        GUI.Label(cellRect, cellId.ToString(), numStyle);
                    }

                    // Handle mouse paint
                    if (cellRect.Contains(evt.mousePosition))
                    {
                        if (evt.type == EventType.MouseDown && evt.button == 0)
                        {
                            isPainting = true;
                            PaintCell(x, y);
                            evt.Use();
                        }
                        else if (evt.type == EventType.MouseDrag && isPainting)
                        {
                            PaintCell(x, y);
                            evt.Use();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            if (evt.type == EventType.MouseUp)
            {
                isPainting = false;
            }
        }

        EditorGUILayout.EndScrollView();
    }

    // ===== ACTIONS =====
    private void DrawActionsSection()
    {
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        mapGenerator = (MapGenerator)EditorGUILayout.ObjectField("Map Generator", mapGenerator, typeof(MapGenerator), true);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Generate Map", GUILayout.Height(30)))
        {
            if (mapGenerator != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(mapGenerator.gameObject, "Generate Map");
                mapGenerator.GenerateFromMatrix(grid);
            }
            else
            {
                EditorUtility.DisplayDialog("Missing Reference", "Hay keo tha MapGenerator (tren MapManager GameObject) vao o 'Map Generator' phia tren!", "OK");
            }
        }

        if (GUILayout.Button("Clear Map", GUILayout.Height(30)))
        {
            if (mapGenerator != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(mapGenerator.gameObject, "Clear Map");
                mapGenerator.ClearMap();
            }
        }

        if (GUILayout.Button("Clear Grid", GUILayout.Height(30)))
        {
            InitGrid();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Export .txt", GUILayout.Height(28)))
        {
            ExportToTxt();
        }

        if (GUILayout.Button("Import .txt", GUILayout.Height(28)))
        {
            ImportFromTxt();
        }

        if (GUILayout.Button("Fill Random", GUILayout.Height(28)))
        {
            FillRandom();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void PaintCell(int x, int y)
    {
        if (selectedPaletteIndex >= 0 && selectedPaletteIndex < palette.Count)
        {
            grid[y, x] = palette[selectedPaletteIndex].id;
            Repaint();
        }
    }

    private Color GetColorForId(int id)
    {
        foreach (var entry in palette)
        {
            if (entry.id == id) return entry.color;
        }
        return new Color(0.15f, 0.15f, 0.15f);
    }

    private void ResizeGrid(int newWidth, int newHeight)
    {
        int[,] newGrid = new int[newHeight, newWidth];
        int copyW = Mathf.Min(gridWidth, newWidth);
        int copyH = Mathf.Min(gridHeight, newHeight);
        for (int y = 0; y < copyH; y++)
        {
            for (int x = 0; x < copyW; x++)
            {
                newGrid[y, x] = grid[y, x];
            }
        }
        grid = newGrid;
        gridWidth = newWidth;
        gridHeight = newHeight;
    }

    private void ExportToTxt()
    {
        string path = EditorUtility.SaveFilePanel("Export Map", Application.dataPath, "map_data", "txt");
        if (string.IsNullOrEmpty(path)) return;

        string content = MapGenerator.MatrixToString(grid);
        File.WriteAllText(path, content);
        AssetDatabase.Refresh();
        Debug.Log("Map exported to: " + path);
    }

    private void ImportFromTxt()
    {
        string path = EditorUtility.OpenFilePanel("Import Map", Application.dataPath, "txt");
        if (string.IsNullOrEmpty(path)) return;

        string[] lines = File.ReadAllLines(path);
        if (lines.Length == 0) return;

        string[] firstCols = lines[0].Trim().Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        int w = firstCols.Length;
        int h = 0;
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line)) h++;
        }

        gridWidth = w;
        gridHeight = h;
        grid = new int[h, w];

        int row = 0;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] cols = line.Trim().Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int x = 0; x < Mathf.Min(cols.Length, w); x++)
            {
                int.TryParse(cols[x], out grid[row, x]);
            }
            row++;
        }

        Debug.Log("Map imported from: " + path + " (" + w + "x" + h + ")");
        Repaint();
    }

    private void FillRandom()
    {
        if (palette.Count <= 1) return;

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                int idx = Random.Range(0, palette.Count);
                grid[y, x] = palette[idx].id;
            }
        }
        Repaint();
    }
}
