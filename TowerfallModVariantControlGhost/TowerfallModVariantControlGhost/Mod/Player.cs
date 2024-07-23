using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using Patcher;
using TowerFall;

namespace ModVariantControlGhost
{
  [Patch]
  public class ModPlayer : Player
  {
    public static Monocle.Collider[] wasColliders = new Monocle.Collider[4];

    // Ghost mode var
    public bool isGhostCooldown = false;
    private Counter ghostEndCounter;
    private Counter ghostBufferCounter;
    
    private int ghostReleaseCount = 0;
    
    private Vector2 ghostTargetPosition;
    private Vector2 ghostOriginalPosition;

    const int ghostCounterMaxValue = 40;
    const int ghostDistanceValue = 70;
    const int ghostDistanceMinValue = 5;
    const int ghostCheckStepValue = 5;
    const int ghostCoolDownDelay = 50;
    const int ghostDistanceMaxRandom = 300;
    const int ghostBufferCounterMaxValue = 10; // must be < ghostCounterMaxValue

    static List<GameTags> ghostListCollider = new List<GameTags>();

    private int kamikazeReleaseCount = 0;
    private Counter kamikazeBufferCounter;
    const int kamikazeBufferCounterMaxValue = 40; // must be < ghostCoolDownDelay
    // End Ghost mode var

    public void GhostCooldown() => this.isGhostCooldown = false;

    public ModPlayer(
      int playerIndex,
      Vector2 position,
      Allegiance allegiance,
      Allegiance teamColor,
      PlayerInventory inventory,
      Player.HatStates hatState,
      bool frozen,
      bool flash,
      bool indicator)
      : base(playerIndex, position, allegiance, teamColor, inventory, hatState, frozen, flash, indicator)
    {
      ghostListCollider.Add(GameTags.Player);
      ghostListCollider.Add(GameTags.Solid);
      ghostListCollider.Add(GameTags.JumpThru);
      ghostListCollider.Add(GameTags.PlayerCollider);
      ghostListCollider.Add(GameTags.PlayerOnlySolid);
      ghostListCollider.Add(GameTags.Granite);

      this.ghostEndCounter = new Counter();
      this.ghostBufferCounter = new Counter();
      this.kamikazeBufferCounter = new Counter();

      this.state.SetCallbacks(6, new Func<int>(this.GhostUpdate), new Action(this.EnterGhost), new Action(this.LeaveGhost));
    }

    public override void Update()
    {
      base.Update();
      if (isGhostCooldown && (bool)this.kamikazeBufferCounter)
      {
        this.kamikazeBufferCounter.Update();
      }
    }

  public int GhostUpdate()
    {
      Vector2 newPosition;

      if (!((MyMatchVariants)Level.Session.MatchSettings.Variants).Ghost[this.PlayerIndex]
          && !((MyMatchVariants)Level.Session.MatchSettings.Variants).GhostRandom[this.PlayerIndex])
      {
        return State == PlayerStates.Ducking ? 2 : 0;
      }

      if ((bool)this.kamikazeBufferCounter && this.input.ShoulderReleased && isGhostCooldown)
      {
        kamikazeReleaseCount++;
      }
      //Surprise mother fucker
      if ((bool)this.kamikazeBufferCounter && kamikazeReleaseCount == 4)
      {
        Explosion.SpawnSuper(this.Level, this.Position, this.PlayerIndex, true);
        kamikazeReleaseCount = 0;
        this.kamikazeBufferCounter.Set(0);
      }

      bool isRandom = ((MyMatchVariants)Level.Session.MatchSettings.Variants).GhostRandom[this.PlayerIndex];
      bool hyperGhost = false;

      // if not dodging 
      if (isGhostCooldown)
      {
        return State == PlayerStates.Ducking ? 2 : 0;
      }
      //if no Ghosting on going and not key pressed
      if (0 == ((int)this.ghostEndCounter.Value) && !this.input.ShoulderPressed)
      {
        return State == PlayerStates.Ducking ? 2 : 0;
      }

      // First action after EnterGhost
      if ((int)this.ghostEndCounter == ghostCounterMaxValue)
      {
        this.DisableSolids(); 
      }

      bool pass = false; //TODO code smell 
      if ((int)this.ghostEndCounter > 0) { 
        this.ghostEndCounter.Update();
        pass = true;
        if ((bool)this.ghostBufferCounter)
        {
          this.ghostBufferCounter.Update();
        }
        //if ((bool)this.kamikazeBufferCounter)
        //{
        //  this.kamikazeBufferCounter.Update();
        //}
        if (this.input.ShoulderReleased)
        {
          ghostReleaseCount++;
        }
      }

      //Is there still time for a hyperjump and release once
      if ((bool)this.ghostBufferCounter && this.input.ShoulderCheck && ghostReleaseCount == 1)
      {
        hyperGhost = true;
      }
      // move the player step by step to the final position after Ghost start
      if (!(bool)this.ghostBufferCounter && ((int)this.ghostEndCounter > 0 || pass))
      {
        newPosition = this.Position;

        if (this.Position.X != ghostTargetPosition.X)
        {
          newPosition.X = ghostOriginalPosition.X + ((ghostTargetPosition.X - ghostOriginalPosition.X) / ghostCounterMaxValue) * (ghostCounterMaxValue - (int)this.ghostEndCounter.Value);
        }
        if (this.Position.Y != ghostTargetPosition.Y) 
        {
          newPosition.Y = ghostOriginalPosition.Y + ((ghostTargetPosition.Y - ghostOriginalPosition.Y) / ghostCounterMaxValue) * (ghostCounterMaxValue - (int)this.ghostEndCounter.Value);
        }
        this.Position = newPosition;

        if (0 == ((int)this.ghostEndCounter.Value))
        {
          return 0;
        }
        return 6;
      }

      // period grace before starting to move, wait for a hyper Ghost
      if ((int)this.ghostEndCounter > 0 && !hyperGhost)
      {
        return 6;
      }

      if (hyperGhost)
      {
        this.ghostBufferCounter.Set(0);
      }

      // Find the final position
      Vector2 oldPosition = this.Position;

      int distanceX;
      int distanceY;
      int choice = 0;
      if (isRandom) {
        Random ghostDistanceRandom = new Random();
        Random choiseRnd = new Random();
        choice = choiseRnd.Next(1, 4);
        distanceX = ghostDistanceRandom.Next(ghostDistanceValue, ghostDistanceMaxRandom);
        distanceY = ghostDistanceRandom.Next(ghostDistanceValue, ghostDistanceMaxRandom);
      } else {
        distanceX = (ghostDistanceValue * (hyperGhost ? 2 : 1));
        distanceY = (ghostDistanceValue * (hyperGhost ? 2 : 1));
      }

      while (distanceX > ghostDistanceMinValue)
      {
        if (isRandom) {
          newPosition = getNewRandomPosition(distanceX, distanceY, choice);
        }
        else {
          newPosition = getNewPosition(distanceX, distanceY);
        }
        if (newPosition.Equals(oldPosition))
        {
          return State == PlayerStates.Ducking ? 2 : 0;
        }
        this.Position = newPosition;

        if (this.CollideAll(ghostListCollider.ToArray()).Count == 0)
        {
          ghostTargetPosition = newPosition;
          // We can Ghost in the new Position
          this.Position = oldPosition;
          ghostOriginalPosition = oldPosition;
          return 6;
        } 

        distanceX -= ghostCheckStepValue;
        distanceY -= ghostCheckStepValue;
        this.Position = oldPosition;
      }
      this.Position = oldPosition;
      this.ghostEndCounter.Set(0);
      return State == PlayerStates.Ducking ? 2 : 0;
    }


    private void EnterGhost()
    {
      this.ghostEndCounter.Set(ghostCounterMaxValue); 
      this.ghostBufferCounter.Set(ghostBufferCounterMaxValue);
      //this.kamikazeBufferCounter.Set(kamikazeBufferCounterMaxValue);
      this.ghostReleaseCount = 0;
      this.Speed = Vector2.Zero;
      this.InvisOpacity = 0; 
    }

    private void LeaveGhost()
    {
      this.ghostBufferCounter.Set(0);

      this.ghostEndCounter.Set(0);
      this.EnableSolids(); 
      if (!this.Invisible)
      {
        this.InvisOpacity = 1;
      }
      this.isGhostCooldown = true;
      //kamikaze option only in ghostcooldown
      this.kamikazeBufferCounter.Set(kamikazeBufferCounterMaxValue);
      //this.kamikazeBufferCounter.Set(0);
      this.kamikazeReleaseCount = 0;
      this.scheduler.ScheduleAction(new Action(this.GhostCooldown), ghostCoolDownDelay);
    }

    public Vector2 getNewRandomPosition(int distanceX, int distanceY, int choice)
    {

      Vector2 newPosition = new Vector2(this.Position.X, this.Position.Y);

      switch (choice) {
        case 1:
          newPosition.X += distanceX;
          newPosition.Y += distanceY;
        break;
        case 2:
          newPosition.X += distanceX;
          newPosition.Y -= distanceY;
          break;
        case 3:
          newPosition.X -= distanceX;
          newPosition.Y += distanceY;
          break;
        case 4:
          newPosition.X -= distanceX;
          newPosition.Y -= distanceY;
          break;
      }

      return newPosition;

    }
    public Vector2 getNewPosition(int distanceX, int distanceY)
    {
      Vector2 newPosition = new Vector2(this.Position.X, this.Position.Y);

      if (this.input.MoveY > 0 && this.input.MoveX == 0)
      {
        newPosition.Y += distanceY;
      }
      else if (this.input.MoveY < 0 && this.input.MoveX == 0)
      {
        newPosition.Y -= distanceY;
      }
      else if ((this.input.MoveX > 0 || this.Facing == Facing.Right) && this.input.MoveY == 0)
      {
        newPosition.X += distanceX;
      }
      else if ((this.input.MoveX < 0 || this.Facing == Facing.Left) && this.input.MoveY == 0)
      {
        newPosition.X -= distanceX;
      }
      else if (this.input.MoveY > 0 && this.input.MoveX > 0)
      {
        newPosition.X += distanceX;
        newPosition.Y += distanceY;
      }
      else if (this.input.MoveY > 0 && this.input.MoveX < 0)
      {
        newPosition.X -= distanceX;
        newPosition.Y += distanceY;
      }
      else if (this.input.MoveY < 0 && this.input.MoveX > 0)
      {
        newPosition.X += distanceX;
        newPosition.Y -= distanceY;
      }
      else if (this.input.MoveY < 0 && this.input.MoveX < 0)
      {
        newPosition.X -= distanceX;
        newPosition.Y -= distanceY;
      }
      return newPosition;
    }
    public override int NormalUpdate()
    {
      int result = base.NormalUpdate();
      if (result != 0)
      {
        return result;
      }
      return GhostUpdate();
    }
  }
}
