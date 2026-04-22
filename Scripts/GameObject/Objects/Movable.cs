using Godot;
using System;
using System.Collections.Generic;

namespace Com.IsartDigital.Sokoban;

public partial class Movable : GameObject
{
    static public List<Movable> list = new List<Movable>();

    [Export] protected float moveDuration = 0.20f;
    [Export] protected float moveTweenStrength = 0.2f;
    [Export] protected float idleTweenStrength = 0.05f;

    protected bool isMoving = false;

    protected Vector2 lastPos;
    protected Tween moveTween;
    protected int numberOfMovement = 0;
    public Vector2 spawningPos;

    protected List<Vector2> pathList = new List<Vector2>();

    public List<Vector2> undoList = new List<Vector2>();

    public Vector2I currentGridPos => GridManager.GetInstance().GetGridPosition(Position);

    public override void _Ready()
    {
        base._Ready();
        lastPos = Position;
    }

    protected virtual void StartMove(Vector2 pTargetPosition, float pTweenedX)
    {
        if (moveTween != null && moveTween.IsValid())
        {
            moveTween.Kill();
            moveTween.Dispose();
        }

        moveTween = CreateTween();

        moveTween.TweenProperty(this, (string)Node2D.PropertyName.Position, pTargetPosition, moveDuration)
                 .SetTrans(Tween.TransitionType.Sine)
                 .Dispose();

        Vector2 lTargetScale = new Vector2(1 + pTweenedX, 1 - pTweenedX);
        Effects.PlayScalePulse(this, lTargetScale, moveDuration);

        moveTween.Finished += OnMoveFinished;
    }

    protected void UptadeListForMovable()
    {
        if (undoList.Count != numberOfMovement + 1)
        {
            for (int i = undoList.Count - 1; i > numberOfMovement; i--)
            {
                foreach (Movable lMovable in list)
                {
                    lMovable.undoList.RemoveAt(i);
                }
            }
        }

        foreach (Movable lMovable in list)
        {
            lMovable.undoList.Add(lMovable.Position);
            lMovable.numberOfMovement++;
        }
    }

    protected virtual void OnMoveFinished()
    {
        lastPos = Position;
        isMoving = false;

        if (pathList.Count > 0)
        {
            Vector2 lNextPos = pathList[0];
            pathList.RemoveAt(0);
            StartMove(lNextPos, moveTweenStrength);
        }
        else
        {
            Effects.PlayIdleLoop(this, idleTweenStrength);
        }

        foreach (Movable lMovable in list)
        {
            if (lMovable.isMoving)
                return;
        }

        UptadeListForMovable();
    }

    protected void AnimateUndoRedo(Vector2 pTargetPosition)
    {
        FreeCurrentCell();

        moveTween?.Kill();

        isMoving = true;
        moveTween = CreateTween();

        moveTween.TweenProperty(this, (string)Node2D.PropertyName.Position, pTargetPosition, moveDuration)
                 .SetTrans(Tween.TransitionType.Sine);

        Effects.PlayScalePulse(this, new Vector2(0.85f, 1.15f), moveDuration);

        moveTween.Finished += OnUndoRedoFinished;
    }

    protected virtual void OnUndoRedoFinished()
    {
        lastPos = Position;
        isMoving = false;
        Effects.PlayIdleLoop(this, idleTweenStrength);
    }

    static public void RevertToCurrentTurnStart()
    {
        foreach (Movable lMovable in list)
        {
            if (lMovable.undoList.Count > 0)
            {
                lMovable.AnimateUndoRedo(lMovable.undoList[lMovable.numberOfMovement]);
            }
        }
    }

    static public void Redo()
    {
        if (list.Count == 0) return;

        if (list[0].undoList.Count - 1 > list[0].numberOfMovement && list.Count != 1)
        {
            Hud.GetInstance().Steps++;

            foreach (Movable lMovable in list)
            {
                lMovable.FindNextPosForRedo();
            }
        }
    }

    protected virtual void FindNextPosForRedo()
    {
        if (undoList.Count - 1 > numberOfMovement && list.Count != 1)
        {
            numberOfMovement += 1;
            AnimateUndoRedo(undoList[numberOfMovement]);
        }
    }

    static public void Undo()
    {
        if (list.Count == 0) return;

        if (list[0].numberOfMovement > 0)
        {
            if (Hud.GetInstance().Steps > 0)
            {
                Hud.GetInstance().Steps--;
            }

            foreach (Movable lMovable in list)
            {
                lMovable.FindNextPosForUndo();
            }
        }
    }

    protected virtual void FindNextPosForUndo()
    {
        if (numberOfMovement > 0)
        {
            numberOfMovement -= 1;
            AnimateUndoRedo(undoList[numberOfMovement]);
        }
    }

    public virtual void FreeCurrentCell() { }

    static public void Restart()
    {
        Hud.GetInstance().Steps = 0;

        foreach (Movable lMovable in list)
        {
            lMovable.FreeCurrentCell();
        }

        foreach (Movable lMovable in list)
        {
            lMovable.ExecuteRestart();
        }
    }

    protected virtual void ExecuteRestart()
    {
        Position = spawningPos;
        
        undoList.Clear();
        undoList.Add(spawningPos);
        numberOfMovement = 0;

        AnimateUndoRedo(spawningPos);
    }
}
