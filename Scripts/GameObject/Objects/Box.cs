using Com.IsartDigital.Shmup.NManager;
using Godot;

namespace Com.IsartDigital.Sokoban;

public partial class Box : Movable
{
    public enum MoveType
    {
        Classic,
        Bounce
    }

    [Export] public Label text;
    [Export] public Sprite2D sprite;
    [Export] private Texture2D placeholderMoving;
    [Export] private Texture2D finalMoving;

    private static readonly Vector2 MOVE_TWEEN = new(0.8f, 0.8f);
    private static readonly Vector2 BOUNCE_TWEEN = new(1.2f, 0.4f);

    private bool wasOnTarget = false;

    private GridManager grid;
    private GameManager game;

    public int moveCount;
    public int counter;

    public Vector2I direction;

    public override void _Ready()
    {
        base._Ready();

        grid = GridManager.GetInstance();
        game = GameManager.GetInstance();

        counter = moveCount;
        UpdateCounter();

        if (sprite != null)
            Effects.PlayIdleLoop(sprite, idleTweenStrength, 0.6f, 0.3f);
    }

    protected override void UpdateVisualMode(bool pIsPlaceholder)
    {
        base.UpdateVisualMode(pIsPlaceholder);

        if (sprite == null) return;

        if (isMoving)
        {
            sprite.Texture = pIsPlaceholder ? placeholderMoving : finalMoving;
        }
        else
        {
            sprite.Texture = pIsPlaceholder ? placeholder : final;
        }
    }

    private void UpdateCounter()
    {
        text.Text = counter.ToString();
    }

    public bool Push(Vector2I pDirection)
    {
        Arrow lArrow = grid.GetAt<Arrow>(currentGridPos);
        direction = lArrow != null ? (Vector2I)lArrow.direction : pDirection;

        if (IsBlocked(direction))
            return false;

        Vector2I next = currentGridPos + direction;

        Box nextBox = grid.GetAt<Box>(next);

        if (nextBox != null && nextBox.IsBlocked(direction))
        {
            Bounce(direction);
            return false;
        }

        InitMove(direction, MOVE_TWEEN, MoveType.Classic);
        return true;
    }

    public void InitMove(Vector2I pDirection, Vector2 tweenSize, MoveType type)
    {
        game.ChangeGameState(GameState.BOX_MOVE);
        isMoving = true;

        if (sprite != null)
        {
            sprite.Texture = GameManager.IsPlaceholderMode ? placeholderMoving : finalMoving;
        }

        if (grid.GetAt<River>(currentGridPos) == null)
            grid.SetCellSolid(currentGridPos, false);

        Vector2I targetGrid = currentGridPos + direction;

        if (pDirection.X != 0)
            tweenSize = new Vector2(tweenSize.Y, tweenSize.X);

        Move(grid.GetGridPosition(targetGrid), tweenSize, type);

        AudioManager.GetInstance().PlayRandomPitch(AudioManager.AudioType.Hit);
    }

    private void Move(Vector2 target, Vector2 scale, MoveType pType)
    {
        moveTween?.Kill();
        moveTween = CreateTween();

        switch (pType)
        {
            case MoveType.Classic:
                moveTween.TweenProperty(this, (string)Node2D.PropertyName.Position, target, moveDuration)
                 .SetTrans(Tween.TransitionType.Sine);

                Effects.PlayScalePulse(this, scale, moveDuration);
                break;

            case MoveType.Bounce:
                Vector2 lOffset = direction.X != 0 ? new Vector2(20, 0) : new Vector2(0, 20);
                if (direction.X > 0 || direction.Y > 0) lOffset *= -1;

                moveTween.TweenProperty(this, (string)Node2D.PropertyName.Position, Position + lOffset, moveDuration / 1.65f)
                 .SetTrans(Tween.TransitionType.Sine);

                moveTween.TweenProperty(this, (string)Node2D.PropertyName.Position, target, moveDuration / 1.3f)
                 .SetTrans(Tween.TransitionType.Sine);

                IsBlockedByPlayer(direction);

                Effects.PlayBouncePulse(this, scale, moveDuration / 1.65f);
                break;
        }

        moveTween.Finished += OnMoveFinished;
    }

    private void Bounce(Vector2I pDirection)
    {
        direction = -pDirection;
        InitMove(pDirection, BOUNCE_TWEEN, MoveType.Bounce);
    }

    private bool IsBlocked(Vector2I lDir)
    {
        Effects.PlayCameraShake(GameManager.GetInstance().camera, 1f, true);
        AudioManager.GetInstance().PlayRandomPitch(AudioManager.AudioType.Hit, 2f);

        return IsBlockedByWall(lDir) || IsBlockedByBox(lDir) || IsBlockedByPlayer(lDir);
    }

    private bool IsBlockedByWall(Vector2I lDir)
    {
        TileData lData = grid.map.GetCellTileData(0, currentGridPos + lDir)
         ?? grid.map.GetCellTileData(1, currentGridPos + lDir);

        if (lData == null)
        {
            direction = lDir;
            return false;
        }

        if ((bool)lData.GetCustomData("Wall"))
        {
            if (counter == 0 || !isMoving)
                return true;

            direction = lDir;
            Bounce(lDir);

            return false;
        }

        direction = lDir;
        return false;
    }

    private bool IsBlockedByBox(Vector2I lDir)
    {
        Box lOther = grid.GetAt<Box>(currentGridPos + lDir);

        if (lOther == null)
            return false;

        direction = lDir;

        if (!isMoving)
            return true;

        bool lPushed = lOther.Push(lDir);

        if (lPushed)
        {
            FinishMove();
            return true;
        }
        else
        {
            Bounce(lDir);
            return true;
        }
    }

    private bool IsBlockedByPlayer(Vector2I lDir)
    {
        Player player = Player.GetInstance();

        if (player == null || player.currentGridPos != currentGridPos + lDir)
            return false;

        if (!isMoving)
            return true;

        return !player.PushedByBox(lDir);
    }

    protected override void OnMoveFinished()
    {
        if (counter > 1)
        {
            counter--;
            UpdateCounter();
            Push(direction);
            return;
        }

        FinishMove();
    }

    protected override void OnUndoRedoFinished()
    {
        base.OnUndoRedoFinished();

        counter = moveCount;
        UpdateCounter();

        RefreshTexture();

        if (sprite != null)
            Effects.PlayIdleLoop(sprite, idleTweenStrength, 0.6f, 0.3f);

        grid.SetCellSolid(currentGridPos, true);
        CheckTargetStatus();
    }

    private void FinishMove()
    {
        moveTween?.Kill();
        isMoving = false;

        if (sprite != null)
        {
            sprite.Texture = GameManager.IsPlaceholderMode ? placeholder : final;
            Effects.PlayIdleLoop(sprite, idleTweenStrength, 0.6f, 0.3f);
        }

        counter = moveCount;
        UpdateCounter();

        Position = grid.GetGridPosition(currentGridPos);
        grid.SetCellSolid(currentGridPos, true);

        CheckTargetStatus();

        game.ChangeGameState(GameState.PLAYER_MOVE);
    }

    private void CheckTargetStatus()
    {
        bool lIsOnTarget = grid.GetAt<Target>(currentGridPos) != null;

        if (lIsOnTarget && !wasOnTarget)
        {
            game.NbTargetActive++;
            wasOnTarget = true;
        }
        else if (!lIsOnTarget && wasOnTarget)
        {
            game.NbTargetActive--;
            wasOnTarget = false;
        }
    }

    protected override void ExecuteRestart()
    {
        base.ExecuteRestart();

        isMoving = false;

        RefreshTexture();

        counter = moveCount;
        UpdateCounter();

        if (sprite != null)
            Effects.PlayIdleLoop(sprite, idleTweenStrength, 0.6f, 0.3f);

        grid.SetCellSolid(GridManager.GetInstance().GetGridPosition(Position), true);
    }

    public override void FreeCurrentCell()
    {
        if (grid.GetAt<River>(currentGridPos) == null)
            grid.SetCellSolid(currentGridPos, false);
    }

    private void RefreshTexture()
    {
        if (sprite == null) return;

        if (isMoving)
            sprite.Texture = GameManager.IsPlaceholderMode ? placeholderMoving : finalMoving;
        else
            sprite.Texture = GameManager.IsPlaceholderMode ? placeholder : final;
    }
}
