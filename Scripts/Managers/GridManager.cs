using Godot;
using Godot.Collections;

namespace Com.IsartDigital.Sokoban;

public partial class GridManager : Node2D
{
    public static GridManager instance;
    public static GridManager GetInstance() => instance;

    public int tileSize;

    [ExportGroup("References")]
    [Export] public TileMap map;
    [Export] private Json jsonLevelDesign;

    [ExportGroup("Level Data")]
    [Export] public TileLevelData[] tileLevelData;
    [Export] public ObjectLevelData[] objectLevelData;

    [ExportGroup("Grid Display")]
    [Export] private float marginPC = 1.1f;
    [Export] private float marginPhone = 1.5f;

    [ExportGroup("Wave Settings")]
    [Export] private float spawnInitialDelay = 0.1f;
    [Export] private float wavePropagationSpeed = 0.04f;
    [Export] private float despawnWaveSpeed = 0.03f;

    [ExportGroup("Animation Timings")]
    [Export] private float animDurFast = 0.05f;
    [Export] private float animDurShort = 0.15f;
    [Export] private float animDurMedium = 0.35f;
    [Export] private float animDurBounce = 0.4f;
    [Export] private float animDurLong = 0.45f;
    [Export] private float animDurFinalRot = 0.2f;
    [Export] private float animDurPlayerDespawnStretch = 0.25f;
    [Export] private float animDurPlayerDespawnPos = 0.3f;

    [ExportGroup("Animation Delays")]
    [Export] private float delayStep = 0.15f;
    [Export] private float delayMedium = 0.3f;
    [Export] private float delayLong = 0.6f;
    [Export] private float delayMax = 0.75f;

    [ExportGroup("Animation Values")]
    [Export] private Vector2 playerSpawnOffset = new Vector2(0, -1000f);

    [ExportSubgroup("Rotations")]
    [Export] private float rotOffsetSpawnStart = -0.3f;
    [Export] private float rotOffsetSpawnStretch = 0.15f;
    [Export] private float rotOffsetSpawnSquash = -0.05f;
    [Export] private float rotOffsetDespawnAnticipation = -0.4f;
    [Export] private float rotOffsetDespawnStretch = 0.5f;

    private const string TILE_LEVEL_NAME = "levelDesign";
    private const string TILE_LEVEL_MAP = "map";
    private const string TILE_LEVEL_ARROW_DIRECTION = "^<v>";

    private const int SOURCE_ID_PLACEHOLDER = 0;
    private const int SOURCE_ID_FINAL = 1;

    private const int TILE_ID_FLOOR_A = 1;
    private const int TILE_ID_FLOOR_B = 2;

    private readonly Vector2 SCALE_ZERO = Vector2.Zero;
    private readonly Vector2 SCALE_ONE = Vector2.One;

    private readonly Vector2 TILE_STRETCH = new Vector2(0.5f, 1.5f);
    private readonly Vector2 TILE_SQUASH = new Vector2(1.2f, 0.8f);

    private readonly Vector2 OBJECT_STRETCH = new Vector2(0.4f, 1.6f);
    private readonly Vector2 OBJECT_SQUASH = new Vector2(1.3f, 0.7f);

    private readonly Vector2 PLAYER_STRETCH_IN = new Vector2(0.4f, 2.0f);
    private readonly Vector2 PLAYER_SQUASH = new Vector2(1.6f, 0.4f);
    private readonly Vector2 PLAYER_REBOUND = new Vector2(0.85f, 1.15f);

    private readonly Vector2 DESPAWN_ANTICIPATION = new Vector2(1.3f, 1.3f);
    private readonly Vector2 DESPAWN_STRETCH = new Vector2(0.1f, 1.6f);
    private readonly Vector2 PLAYER_DESPAWN_SQUASH = new Vector2(1.5f, 0.5f);
    private readonly Vector2 PLAYER_DESPAWN_STRETCH = new Vector2(0.2f, 2.5f);

    private AStarGrid2D pathfinding = new AStarGrid2D();

    private int currentWidth;
    private int currentHeight;
    private int currentLevelIndex = -1;

    private Node2D animContainer;

    #region Logic

    public override void _Ready()
    {
        if (instance == null)
            instance = this;
        else
        {
            QueueFree();
            return;
        }

        tileSize = map.TileSet.TileSize.X;
        GetViewport().SizeChanged += OnViewportResized;

        GameManager.OnVisualModeChanged += UpdateTilesetVisuals;
    }

    private int GetCurrentSourceId() => GameManager.IsPlaceholderMode ? SOURCE_ID_PLACEHOLDER : SOURCE_ID_FINAL;

    private void UpdateTilesetVisuals(bool pIsPlaceholder)
    {
        int lTargetSourceId = pIsPlaceholder ? SOURCE_ID_PLACEHOLDER : SOURCE_ID_FINAL;

        for (int l = 0; l < map.GetLayersCount(); l++)
        {
            foreach (Vector2I lCell in map.GetUsedCells(l))
            {
                Vector2I lAtlasCoords = map.GetCellAtlasCoords(l, lCell);
                int lAltTile = map.GetCellAlternativeTile(l, lCell);

                map.SetCell(l, lCell, lTargetSourceId, lAtlasCoords, lAltTile);
            }
        }
    }

    public void ResetLevel()
    {
        if (currentLevelIndex != -1)
            LoadLevel(currentLevelIndex);
    }

    public void ClearGrid()
    {
        if (animContainer != null)
        {
            animContainer.QueueFree();
            animContainer = null;
        }

        for (int i = 0; i < map.GetLayersCount(); i++)
            map.SetLayerModulate(i, new Color(1, 1, 1, 1));

        foreach (Node lChild in map.GetChildren())
            lChild.QueueFree();

        map.Clear();
        Hud.GetInstance().Steps = 0;

        Movable.list.Clear();
    }

    public void DestroyManager()
    {
        ClearGrid();
        QueueFree();
    }

    public void LoadLevel(int pLevel)
    {
        Dictionary lData = (Dictionary)jsonLevelDesign.Data;
        Array lLevelDesign = lData[TILE_LEVEL_NAME].AsGodotArray();

        if (pLevel < 0 || pLevel >= lLevelDesign.Count)
            return;

        currentLevelIndex = pLevel;
        GameManager.GetInstance().ChangeGameState(GameState.MENU);

        if (map.GetChildCount() > 0 || map.GetUsedCells(0).Count > 0)
        {
            Tween lTween = CreateTween();
            PlayDespawnAnimation(lTween);
            lTween.Finished += ClearAndGenerate;
        }
        else
            ClearAndGenerate();
    }

    public void DespawnToMenu()
    {
        GameManager.GetInstance().ChangeGameState(GameState.MENU);

        if (map.GetChildCount() > 0 || map.GetUsedCells(0).Count > 0)
        {
            Tween lTween = CreateTween();
            PlayDespawnAnimation(lTween);
            lTween.Finished += OnDespawnToMenuFinished;
        }
        else
        {
            OnDespawnToMenuFinished();
        }
    }

    private void OnDespawnToMenuFinished()
    {
        ClearGrid();
        MenuManager.GetInstance().ChangeMenu(Hud.GetInstance(), (int)MenuList.SELECTOR);
    }

    private void ClearAndGenerate()
    {
        ClearGrid();

        Dictionary lData = (Dictionary)jsonLevelDesign.Data;
        Array lLevelDesign = lData[TILE_LEVEL_NAME].AsGodotArray();

        GenerateLevel(lLevelDesign[currentLevelIndex].AsGodotDictionary());
    }

    private void GenerateLevel(Dictionary pLevelData)
    {
        Array lMapLines = pLevelData[TILE_LEVEL_MAP].AsGodotArray();

        CalculateMap(lMapLines);
        InitPathfinding();
        BuildGrid(lMapLines);
        CenterGrid(currentWidth, currentHeight);

        PlaySpawnAnimation();
    }

    private void CalculateMap(Array pMapLines)
    {
        currentHeight = pMapLines.Count;
        currentWidth = 0;

        foreach (Variant lLine in pMapLines)
            currentWidth = Mathf.Max(currentWidth, lLine.ToString().Length);
    }

    private void InitPathfinding()
    {
        pathfinding.Region = new Rect2I(0, 0, currentWidth, currentHeight);
        pathfinding.CellSize = new Vector2(tileSize, tileSize);
        pathfinding.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Manhattan;
        pathfinding.DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never;
        pathfinding.Update();
    }

    private void BuildGrid(Array pMapLines)
    {
        for (int y = 0; y < pMapLines.Count; y++)
        {
            string lLine = pMapLines[y].ToString();

            for (int x = 0; x < lLine.Length; x++)
            {
                Vector2I lGridPos = new Vector2I(x, y);
                string lChar = lLine[x].ToString();

                TrySpawnTile(lChar, lGridPos);
                TrySpawnObject(lChar, lGridPos);
            }
        }
    }

    private void TrySpawnTile(string pChar, Vector2I pGridPos)
    {
        if (tileLevelData == null) return;

        foreach (TileLevelData lTileData in tileLevelData)
        {
            if (lTileData != null && lTileData.character == pChar)
            {
                Vector2I lTargetAtlasCoords = lTileData.tileMapIndex;

                if (!lTileData.blockPath)
                {
                    bool isEvenTile = (pGridPos.X + pGridPos.Y) % 2 == 0;
                    lTargetAtlasCoords = isEvenTile ? new Vector2I(TILE_ID_FLOOR_A, 0) : new Vector2I(TILE_ID_FLOOR_B, 0);
                }

                map.SetCell(lTileData.layer, pGridPos, GetCurrentSourceId(), lTargetAtlasCoords);

                if (lTileData.blockPath)
                    pathfinding.SetPointSolid(pGridPos, true);
            }
        }
    }

    private void TrySpawnObject(string pChar, Vector2I pGridPos)
    {
        if (objectLevelData == null || string.IsNullOrEmpty(pChar)) return;

        char lC = pChar[0];

        foreach (ObjectLevelData lObjectData in objectLevelData)
        {
            if (lObjectData == null) continue;

            if (IsMatchingObject(lObjectData, pChar, lC))
            {
                SetupObject(lObjectData, pChar, lC, pGridPos);
                break;
            }
        }
    }

    private bool IsMatchingObject(ObjectLevelData pData, string pChar, char pC)
    {
        bool lIsBoxNumber = char.IsDigit(pC) && pData.tileType == TileType.Box;
        bool lIsArrowDirection = TILE_LEVEL_ARROW_DIRECTION.Contains(pC) && pData.tileType == TileType.Arrow;

        return pData.character == pChar || lIsBoxNumber || lIsArrowDirection;
    }

    private void SetupObject(ObjectLevelData pData, string pChar, char pC, Vector2I pGridPos)
    {
        if (pData.scene == null) return;

        GameObject lObject = (GameObject)pData.scene.Instantiate();

        if (lObject is Box lBox)
            SetupBoxObject(lBox, pData, pChar, pGridPos);

        if (lObject is Arrow lArrow)
            lArrow.IniatiateArrowBehavior(pC);

        if (pData.blockPath)
            SetCellSolid(pGridPos, true);

        if (lObject is Movable lMovable)
        {
            Movable.list.Add(lMovable);
            lMovable.undoList.Add(GetGridPosition(pGridPos));
            lMovable.spawningPos = GetGridPosition(pGridPos);
        }

        map.AddChild(lObject);
        lObject.Position = GetGridPosition(pGridPos);

        bool isEvenTile = (pGridPos.X + pGridPos.Y) % 2 == 0;
        Vector2I lTargetAtlasCoords = isEvenTile ? new Vector2I(TILE_ID_FLOOR_A, 0) : new Vector2I(TILE_ID_FLOOR_B, 0);
        map.SetCell(pData.layer, pGridPos, GetCurrentSourceId(), lTargetAtlasCoords);

        lObject.Name = $"{pData.tileType}";
    }
    private void SetupBoxObject(Box pBox, ObjectLevelData pData, string pChar, Vector2I pGridPos)
    {
        bool lIsBoxNumber = char.IsDigit(pChar[0]);
        pBox.text.Text = lIsBoxNumber ? pChar : pData.character;
        pBox.moveCount = pChar.ToInt();

    }

    public Vector2 GetGridPosition(Vector2I pGridPos) => map.MapToLocal(pGridPos);
    public Vector2I GetGridPosition(Vector2 pLocalPos) => map.LocalToMap(pLocalPos);

    public bool IsWallWithPathfinding(Vector2I pGridPos)
    {
        if (!pathfinding.Region.HasPoint(pGridPos)) return true;
        return pathfinding.IsPointSolid(pGridPos);
    }

    public Array<Vector2I> Pathfind(Vector2I pStart, Vector2I pEnd)
    {
        if (!pathfinding.Region.HasPoint(pEnd) || pathfinding.IsPointSolid(pEnd))
            return new Array<Vector2I>();

        return pathfinding.GetIdPath(pStart, pEnd);
    }

    public Type GetAt<Type>(Vector2I pGridPos)
    {
        foreach (Node lChild in map.GetChildren())
        {
            if (lChild is Type lResult && lChild is GameObject lGameObject)
            {
                if (GetGridPosition(lGameObject.Position) == pGridPos)
                    return lResult;
            }
        }
        return default;
    }

    public void SetCellSolid(Vector2I pGridPos, bool pIsSolid)
    {
        if (pathfinding.Region.HasPoint(pGridPos))
            pathfinding.SetPointSolid(pGridPos, pIsSolid);
    }

    private void CenterGrid(int pWidth, int pHeight)
    {
        Vector2 lLevelSize = new Vector2(pWidth * tileSize, pHeight * tileSize);
        Vector2 lScreenSize = GetViewportRect().Size;

        bool lIsPortrait = lScreenSize.Y > lScreenSize.X;
        float lCurrentMargin = lIsPortrait ? marginPhone : marginPC;

        float lScaleX = (lScreenSize.X * lCurrentMargin) / lLevelSize.X;
        float lScaleY = (lScreenSize.Y * lCurrentMargin) / lLevelSize.Y;
        float lFinalScale = Mathf.Min(lScaleX, lScaleY);

        Scale = new Vector2(lFinalScale, lFinalScale);
        Position = (lScreenSize / 2) - (lLevelSize * lFinalScale / 2);
    }

    private void OnViewportResized() => CenterGrid(currentWidth, currentHeight);

    protected override void Dispose(bool pDisposing)
    {
        GameManager.OnVisualModeChanged -= UpdateTilesetVisuals;
        instance = null;
        base.Dispose(pDisposing);
    }

    #endregion

    #region Animation

    private void SetupAnimationContainer()
    {
        Color lColor = new Color(1, 1, 1, 0);

        animContainer = new Node2D();

        AddChild(animContainer);

        animContainer.ZIndex = map.ZIndex - 1;

        for (int i = 0; i < map.GetLayersCount(); i++)
            map.SetLayerModulate(i, lColor);
    }

    private void PlaySpawnAnimation()
    {
        Tween lTween = CreateTween();
        SetupAnimationContainer();

        float lMaxTileDelay = AnimateTilesSpawn(lTween);
        AnimateObjectsSpawn(lTween, lMaxTileDelay);

        lTween.Finished += OnSpawnAnimationFinished;
    }

    private float AnimateTilesSpawn(Tween pTween)
    {
        float lMaxTileDelay = 0f;

        for (int l = 0; l < map.GetLayersCount(); l++)
        {
            foreach (Vector2I lCell in map.GetUsedCells(l))
            {
                Vector2I lAtlasCoords = map.GetCellAtlasCoords(l, lCell);
                float lDelay = spawnInitialDelay + ((lCell.X + lCell.Y) * wavePropagationSpeed);

                if (lDelay > lMaxTileDelay)
                    lMaxTileDelay = lDelay;

                Node2D lFakeTile = CreateFakeTile(l, lAtlasCoords, lCell);
                animContainer.AddChild(lFakeTile);

                lFakeTile.Scale = SCALE_ZERO;

                pTween.Parallel().TweenProperty(lFakeTile, (string)Node2D.PropertyName.Scale, TILE_STRETCH, animDurShort)
                  .SetDelay(lDelay)
                  .SetTrans(Tween.TransitionType.Quad)
                  .SetEase(Tween.EaseType.Out)
                  .Dispose();

                pTween.Parallel().TweenProperty(lFakeTile, (string)Node2D.PropertyName.Scale, TILE_SQUASH, animDurShort)
                  .SetDelay(lDelay + animDurShort)
                  .SetTrans(Tween.TransitionType.Quad)
                  .SetEase(Tween.EaseType.Out)
                  .Dispose();

                pTween.Parallel().TweenProperty(lFakeTile, (string)Node2D.PropertyName.Scale, SCALE_ONE, animDurMedium)
                  .SetDelay(lDelay + delayMedium)
                  .SetTrans(Tween.TransitionType.Bounce)
                  .SetEase(Tween.EaseType.Out)
                  .Dispose();
            }
        }

        return lMaxTileDelay;
    }

    private void AnimateObjectsSpawn(Tween pTween, float pBaseDelay)
    {
        float lObjectBaseDelay = pBaseDelay + delayStep;
        float lMaxObjectDelay = lObjectBaseDelay;

        Node2D lPlayer = null;
        Vector2 lPlayerTargetPos = Vector2.Zero;

        foreach (Node lChild in map.GetChildren())
        {
            if (lChild is Node2D lNode)
            {
                if (lNode is Player)
                {
                    lPlayer = lNode;
                    lPlayerTargetPos = lNode.Position;
                    lNode.Scale = SCALE_ZERO;
                    continue;
                }

                Vector2I lGridPos = GetGridPosition(lNode.Position);
                float lDelay = lObjectBaseDelay + ((lGridPos.X + lGridPos.Y) * wavePropagationSpeed);

                if (lDelay > lMaxObjectDelay)
                    lMaxObjectDelay = lDelay;

                float lTargetRot = lNode.Rotation;

                lNode.Scale = SCALE_ZERO;
                lNode.Rotation = lTargetRot + rotOffsetSpawnStart;

                pTween.Parallel().TweenProperty(lNode, (string)Node2D.PropertyName.Scale, OBJECT_STRETCH, animDurShort)
                  .SetDelay(lDelay)
                  .SetTrans(Tween.TransitionType.Quad)
                  .SetEase(Tween.EaseType.Out)
                  .Dispose();

                pTween.Parallel().TweenProperty(lNode, (string)Node2D.PropertyName.Rotation, lTargetRot + rotOffsetSpawnStretch, animDurShort)
                  .SetDelay(lDelay)
                  .Dispose();

                pTween.Parallel().TweenProperty(lNode, (string)Node2D.PropertyName.Scale, OBJECT_SQUASH, animDurShort)
                  .SetDelay(lDelay + animDurShort)
                  .SetTrans(Tween.TransitionType.Quad)
                  .SetEase(Tween.EaseType.Out)
                  .Dispose();

                pTween.Parallel().TweenProperty(lNode, (string)Node2D.PropertyName.Rotation, lTargetRot + rotOffsetSpawnSquash, animDurShort)
                  .SetDelay(lDelay + animDurShort)
                  .Dispose();

                pTween.Parallel().TweenProperty(lNode, (string)Node2D.PropertyName.Scale, SCALE_ONE, animDurBounce)
                  .SetDelay(lDelay + delayMedium)
                  .SetTrans(Tween.TransitionType.Bounce)
                  .SetEase(Tween.EaseType.Out)
                  .Dispose();

                pTween.Parallel().TweenProperty(lNode, (string)Node2D.PropertyName.Rotation, lTargetRot, animDurFinalRot)
                  .SetDelay(lDelay + delayMedium)
                  .Dispose();
            }
        }

        if (lPlayer != null)
            AnimatePlayerSpawn(pTween, lPlayer, lPlayerTargetPos, lMaxObjectDelay + delayStep);
    }

    private void AnimatePlayerSpawn(Tween pTween, Node2D pPlayer, Vector2 pTargetPos, float pDelay)
    {
        pPlayer.Position = pTargetPos + playerSpawnOffset;
        pPlayer.Scale = PLAYER_STRETCH_IN;

        pTween.Parallel().TweenProperty(pPlayer, (string)Node2D.PropertyName.Position, pTargetPos, animDurLong)
          .SetDelay(pDelay)
          .SetTrans(Tween.TransitionType.Quad)
          .SetEase(Tween.EaseType.In)
          .Dispose();

        pTween.Parallel().TweenProperty(pPlayer, (string)Node2D.PropertyName.Scale, PLAYER_SQUASH, animDurShort)
          .SetDelay(pDelay + animDurLong)
          .SetTrans(Tween.TransitionType.Quad)
          .SetEase(Tween.EaseType.Out)
          .Dispose();

        pTween.Parallel().TweenProperty(pPlayer, (string)Node2D.PropertyName.Scale, PLAYER_REBOUND, animDurShort)
          .SetDelay(pDelay + delayLong)
          .SetTrans(Tween.TransitionType.Quad)
          .SetEase(Tween.EaseType.Out)
          .Dispose();

        pTween.Parallel().TweenProperty(pPlayer, (string)Node2D.PropertyName.Scale, SCALE_ONE, animDurMedium)
          .SetDelay(pDelay + delayMax)
          .SetTrans(Tween.TransitionType.Bounce)
          .SetEase(Tween.EaseType.Out)
          .Dispose();
    }

    private void PlayDespawnAnimation(Tween pTween)
    {
        SetupAnimationContainer();
        float lDelay = 0.0f;

        AnimateObjectsDespawn(pTween, lDelay);
        AnimateTilesDespawn(pTween, lDelay);
    }

    private void AnimateObjectsDespawn(Tween pTween, float pDelay)
    {
        foreach (Node lChild in map.GetChildren())
        {
            if (lChild is Node2D lNode)
            {
                if (lNode is Player)
                {
                    pTween.Parallel().TweenProperty(lNode, (string)Node2D.PropertyName.Scale, PLAYER_DESPAWN_SQUASH, animDurShort)
                      .SetDelay(pDelay)
                      .SetTrans(Tween.TransitionType.Quad)
                      .SetEase(Tween.EaseType.Out)
                      .Dispose();

                    pTween.Parallel().TweenProperty(lNode, (string)Node2D.PropertyName.Scale, PLAYER_DESPAWN_STRETCH, animDurPlayerDespawnStretch)
                      .SetDelay(pDelay + animDurShort)
                      .SetTrans(Tween.TransitionType.Quad)
                      .SetEase(Tween.EaseType.In)
                      .Dispose();

                    pTween.Parallel().TweenProperty(lNode, (string)Node2D.PropertyName.Position, lNode.Position + playerSpawnOffset, animDurPlayerDespawnPos)
                      .SetDelay(pDelay + animDurShort)
                      .SetTrans(Tween.TransitionType.Quad)
                      .SetEase(Tween.EaseType.In)
                      .Dispose();
                }
                else
                {
                    float lTargetRot = lNode.Rotation;

                    pTween.Parallel().TweenProperty(lNode, (string)Node2D.PropertyName.Scale, DESPAWN_ANTICIPATION, animDurShort)
                      .SetDelay(pDelay)
                      .SetTrans(Tween.TransitionType.Back)
                      .SetEase(Tween.EaseType.Out)
                      .Dispose();

                    pTween.Parallel().TweenProperty(lNode, (string)Node2D.PropertyName.Rotation, lTargetRot + rotOffsetDespawnAnticipation, animDurShort)
                      .SetDelay(pDelay)
                      .SetTrans(Tween.TransitionType.Back)
                      .SetEase(Tween.EaseType.Out)
                      .Dispose();

                    pTween.Parallel().TweenProperty(lNode, (string)Node2D.PropertyName.Scale, DESPAWN_STRETCH, animDurShort)
                      .SetDelay(pDelay + animDurShort)
                      .SetTrans(Tween.TransitionType.Cubic)
                      .SetEase(Tween.EaseType.In)
                      .Dispose();

                    pTween.Parallel().TweenProperty(lNode, (string)Node2D.PropertyName.Rotation, lTargetRot + rotOffsetDespawnStretch, animDurShort)
                      .SetDelay(pDelay + animDurShort)
                      .SetTrans(Tween.TransitionType.Cubic)
                      .SetEase(Tween.EaseType.In)
                      .Dispose();

                    pTween.Parallel().TweenProperty(lNode, (string)Node2D.PropertyName.Scale, SCALE_ZERO, animDurFast)
                      .SetDelay(pDelay + delayMedium)
                      .Dispose();
                }
            }
        }
    }

    private void AnimateTilesDespawn(Tween pTween, float pDelay)
    {
        for (int l = 0; l < map.GetLayersCount(); l++)
        {
            foreach (Vector2I lCell in map.GetUsedCells(l))
            {
                Vector2I lAtlasCoords = map.GetCellAtlasCoords(l, lCell);
                Node2D lFakeTile = CreateFakeTile(l, lAtlasCoords, lCell);
                animContainer.AddChild(lFakeTile);

                pTween.Parallel().TweenProperty(lFakeTile, (string)Node2D.PropertyName.Scale, DESPAWN_ANTICIPATION, animDurShort)
                  .SetDelay(pDelay)
                  .SetTrans(Tween.TransitionType.Back)
                  .SetEase(Tween.EaseType.Out)
                  .Dispose();

                pTween.Parallel().TweenProperty(lFakeTile, (string)Node2D.PropertyName.Scale, DESPAWN_STRETCH, animDurShort)
                  .SetDelay(pDelay + animDurShort)
                  .SetTrans(Tween.TransitionType.Cubic)
                  .SetEase(Tween.EaseType.In)
                  .Dispose();

                pTween.Parallel().TweenProperty(lFakeTile, (string)Node2D.PropertyName.Scale, SCALE_ZERO, animDurFast)
                  .SetDelay(pDelay + delayMedium)
                  .Dispose();
            }
        }
    }

    private Node2D CreateFakeTile(int pLayer, Vector2I pAtlasCoords, Vector2I pGridPos)
    {
        Node2D lPivot = new Node2D();
        TileMap lFakeMap = new TileMap();

        lFakeMap.TileSet = map.TileSet;
        lFakeMap.SetCell(0, Vector2I.Zero, GetCurrentSourceId(), pAtlasCoords);

        lFakeMap.Position = -GetGridPosition(Vector2I.Zero);

        lPivot.AddChild(lFakeMap);

        lPivot.Position = GetGridPosition(pGridPos);

        return lPivot;
    }

    private void OnSpawnAnimationFinished()
    {
        Color lColor = new Color(1, 1, 1, 1);

        if (animContainer != null)
        {
            animContainer.QueueFree();
            animContainer = null;
        }

        for (int i = 0; i < map.GetLayersCount(); i++)
            map.SetLayerModulate(i, lColor);

        GameManager.GetInstance().ChangeGameState(GameState.PLAYER_MOVE);
    }

    #endregion
}
