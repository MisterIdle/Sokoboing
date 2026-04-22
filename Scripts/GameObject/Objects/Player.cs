using Com.IsartDigital.Shmup.NManager;
using Godot;
using Godot.Collections;

namespace Com.IsartDigital.Sokoban;

public partial class Player : Movable
{
    private static Player instance;
    public static Player GetInstance() => instance;

    [Export] private Node2D playerSprite;

    [Export] private float minSwipeDistance = 50f;
    private Vector2 touchStartPosition;

    public override void _Ready()
    {
        base._Ready();

        if (instance == null)
            instance = this;
        else
        {
            QueueFree();
            return;
        }

        if (playerSprite != null)
            Effects.PlayIdleLoop(playerSprite, idleTweenStrength);
    }

    public override void _Input(InputEvent pEvent)
    {
        if (GameManager.GetInstance().GetGameState() != GameState.PLAYER_MOVE)
            return;

        if (HandleTouchInput(pEvent))
            return;

        if (pEvent is InputEventKey)
            HandleKeyboardInput(pEvent);
    }

    protected override void StartMove(Vector2 pTargetPosition, float pTweenedX)
    {
        if (isMoving)
            return;

        UpdateSpriteDirection(pTargetPosition);

        isMoving = true;

        Effects.PlayWalkHop(playerSprite, moveTweenStrength);

        AudioManager.GetInstance().PlayRandomPitch(AudioManager.AudioType.FootStep);
        Effects.PlayCameraShake(GameManager.GetInstance().camera, 0.3f, false);

        base.StartMove(pTargetPosition, pTweenedX);
    }

    private void UpdateSpriteDirection(Vector2 pTargetPosition)
    {
        if (playerSprite == null)
            return;

        Vector2I targetGrid = GridManager.GetInstance().GetGridPosition(pTargetPosition);
        Vector2I lDir = targetGrid - currentGridPos;

        if (lDir.X != 0)
        {
            float lSignX = lDir.X > 0 ? -1f : 1f;
            playerSprite.Scale = new Vector2(lSignX * Mathf.Abs(playerSprite.Scale.X), playerSprite.Scale.Y);
        }
    }

    private bool HandleTouchInput(InputEvent pEvent)
    {
        if (pEvent.IsActionPressed(Input.INTERACT))
        {
            touchStartPosition = GetViewport().GetMousePosition();
            return true;
        }

        if (pEvent.IsActionReleased(Input.INTERACT))
        {
            Vector2 lEndPos = GetViewport().GetMousePosition();

            if (touchStartPosition.DistanceTo(lEndPos) < minSwipeDistance)
                HandleTap(lEndPos);
            else
                HandleSwipe(lEndPos);

            return true;
        }

        return false;
    }

    private void HandleKeyboardInput(InputEvent pEvent)
    {
        Vector2I lDirection = Vector2I.Zero;

        CancelPathfinding();

        if (pEvent.IsActionPressed(Input.UP))
            lDirection = Vector2I.Up;

        else if (pEvent.IsActionPressed(Input.DOWN))
            lDirection = Vector2I.Down;

        else if (pEvent.IsActionPressed(Input.LEFT))
            lDirection = Vector2I.Left;

        else if (pEvent.IsActionPressed(Input.RIGHT))
            lDirection = Vector2I.Right;

        if (lDirection != Vector2I.Zero)
            TryMove(lDirection);
    }

    private void TryMove(Vector2I pDir)
    {
        if (GameManager.GetInstance().gameFinished)
            return;

        Vector2I lTargetGridPos = currentGridPos + pDir;
        Box lBox = GridManager.GetInstance().GetAt<Box>(lTargetGridPos);

        if (lBox != null)
        {
            if (lBox.Push(pDir))
                StartMove(GridManager.GetInstance().GetGridPosition(currentGridPos), moveTweenStrength);

            Effects.PlayPushEffect(lBox, pDir);
            return;
        }

        if (GridManager.GetInstance().IsWallWithPathfinding(lTargetGridPos))
        {
            Node2D targetToAnimate = playerSprite != null ? playerSprite : this;
            Effects.PlayPushEffect(targetToAnimate, pDir);
            return;
        }

        StartMove(GridManager.GetInstance().GetGridPosition(lTargetGridPos), moveTweenStrength);
    }

    private void HandleSwipe(Vector2 pPos)
    {
        Vector2 lDiff = pPos - touchStartPosition;

        Vector2I lDir = Mathf.Abs(lDiff.X) > Mathf.Abs(lDiff.Y)
            ? (lDiff.X > 0 ? Vector2I.Right : Vector2I.Left)
            : (lDiff.Y > 0 ? Vector2I.Down : Vector2I.Up);

        TryMove(lDir);
    }

    private void HandleTap(Vector2 pScreenPos)
    {
        Vector2 lLocalPos = GridManager.GetInstance().map.ToLocal(pScreenPos);
        Vector2I lTarget = GridManager.GetInstance().GetGridPosition(lLocalPos);
        Vector2I lStart = currentGridPos;

        int lDistance = 1;

        if (lTarget == lStart)
            return;

        Vector2I lDiff = lTarget - lStart;

        if (lDiff.LengthSquared() == lDistance && IsSolid(lTarget))
        {
            TryMove(lDiff);
            return;
        }

        if (IsSolid(lTarget))
            lTarget = GetClosestFreeNeighbor(lStart, lTarget);

        ExecutePathfinding(lStart, lTarget);
    }

    private void ExecutePathfinding(Vector2I pStart, Vector2I pTarget)
    {
        Array<Vector2I> lPath = GridManager.GetInstance().Pathfind(pStart, pTarget);

        if (lPath == null || lPath.Count <= 1)
            return;

        pathList.Clear();

        for (int i = 1; i < lPath.Count; i++)
            pathList.Add(GridManager.GetInstance().GetGridPosition(lPath[i]));

        Vector2 lNext = pathList[0];
        pathList.RemoveAt(0);

        StartMove(lNext, moveTweenStrength);
    }

    private void CancelPathfinding()
    {
        pathList.Clear();
    }

    private Vector2I GetClosestFreeNeighbor(Vector2I pSource, Vector2I pTarget)
    {
        Vector2I[] lDirections = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };
        Vector2I lBestCell = pSource;

        int lMinDistance = int.MaxValue;

        foreach (Vector2I lDir in lDirections)
        {
            Vector2I lNeighbor = pTarget + lDir;

            if (!IsSolid(lNeighbor))
            {
                int lDistance = Mathf.Abs(pSource.X - lNeighbor.X) + Mathf.Abs(pSource.Y - lNeighbor.Y);

                if (lDistance < lMinDistance)
                {
                    lMinDistance = lDistance;
                    lBestCell = lNeighbor;
                }
            }
        }

        return lBestCell;
    }

    public bool PushedByBox(Vector2I pDir)
    {
        Vector2I lNextGridPos = currentGridPos + pDir;

        if (IsSolid(lNextGridPos))
        {
            CancelPathfinding();
            RevertToCurrentTurnStart();

            GameManager.GetInstance().ChangeGameState(GameState.PLAYER_MOVE);
            return false;
        }

        StartMove(GridManager.GetInstance().GetGridPosition(lNextGridPos), -moveTweenStrength);
        return true;
    }

    private bool IsSolid(Vector2I pGridPos)
    {
        return GridManager.GetInstance().IsWallWithPathfinding(pGridPos) ||
               GridManager.GetInstance().GetAt<Box>(pGridPos) != null;
    }

    protected override void OnMoveFinished()
    {
        base.OnMoveFinished();
        Hud.GetInstance().Steps++;

        Effects.PlayIdleLoop(playerSprite, idleTweenStrength);
        Effects.PlayWalkHop(playerSprite, moveTweenStrength);
    }

    protected override void UpdateVisualMode(bool pIsPlaceholder)
    {
        base.UpdateVisualMode(pIsPlaceholder);

        if (playerSprite is Sprite2D lSprite)
        {
            lSprite.Texture = pIsPlaceholder ? placeholder : final;
        }
    }

    protected override void Dispose(bool pDisposing)
    {
        instance = null;
        base.Dispose(pDisposing);
    }
}
