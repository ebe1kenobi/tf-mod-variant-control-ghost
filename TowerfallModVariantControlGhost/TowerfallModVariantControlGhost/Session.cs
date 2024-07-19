// Decompiled with JetBrains decompiler
// Type: TowerFall.Session
// Assembly: TowerFall, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6C0D5BCB-5E68-4D7B-89AF-7F8B9D4C227C
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\TowerFall.original\TowerFall.exe

using Microsoft.Xna.Framework;
using Monocle;
using System;

#nullable disable
namespace TowerFall
{
  public class Session
  {
    private const int START_ARROWS = 3;
    public static readonly int[] TeamStartArrows = new int[3]
    {
      3,
      3,
      2
    };
    public long SongTimer;
    public MatchSettings MatchSettings;
    public TowerFall.MatchStats[] MatchStats;
    public int[] Scores;
    public int[] OldScores;
    public DarkWorldSessionState DarkWorldState;
    public Level CurrentLevel;
    public int QuestTestWave;
    public Player.HatStates TestHatState;
    public Allegiance TestTeam;

    public TreasureSpawner TreasureSpawner { get; private set; }

    public RoundLogic RoundLogic { get; private set; }

    public int RoundIndex { get; private set; }

    public ArrowTypes RoundRandomArrowType { get; private set; }

    public Session(MatchSettings settings)
    {
      this.MatchSettings = settings;
      this.MatchStats = new TowerFall.MatchStats[4];
      this.Scores = new int[this.MatchSettings.TeamMode ? 2 : 4];
      this.OldScores = new int[this.MatchSettings.TeamMode ? 2 : 4];
      this.TestHatState = Player.HatStates.Normal;
      this.TestTeam = Allegiance.Neutral;
    }

    public void StartGame()
    {
      if (!this.MatchSettings.SoloMode)
      {
        ++SaveData.Instance.Stats.MatchesPlayed;
        ++SaveData.Instance.Stats.VersusTowerPlays[this.MatchSettings.LevelSystem.ID.X];
        if (this.MatchSettings.RandomVersusTower)
          ++SaveData.Instance.Stats.VersusRandomPlays;
        else
          SaveData.Instance.Stats.RegisterVersusTowerSelection(this.MatchSettings.LevelSystem.ID.X);
        ++SessionStats.MatchesPlayed;
      }
      if (this.MatchSettings.Mode == Modes.DarkWorld)
      {
        this.DarkWorldState = new DarkWorldSessionState(this);
        ++SaveData.Instance.DarkWorld.Towers[this.MatchSettings.LevelSystem.ID.X].Attempts;
      }
      this.TreasureSpawner = this.MatchSettings.LevelSystem.GetTreasureSpawner(this);
      Engine.Instance.Scene = (Scene) new LevelLoaderXML(this);
    }

    public void LevelLoadStart(Level level)
    {
      this.CurrentLevel = level;
      this.RoundLogic = RoundLogic.GetRoundLogic(this);
      if (this.TreasureSpawner == null)
        return;
      this.RoundRandomArrowType = this.TreasureSpawner.GetRandomArrowType(true);
    }

    public void GotoNextRound()
    {
      for (int index = 0; index < this.Scores.Length; ++index)
        this.OldScores[index] = this.Scores[index];
      ++this.RoundIndex;
      Engine.Instance.Scene = (Scene) new LevelLoaderXML(this);
    }

    public void EndlessContinue()
    {
      this.DarkWorldState.OnContinue();
      Engine.Instance.Scene = (Scene) new LevelLoaderXML(this);
    }

    public void StartRound()
    {
      if (this.RoundLogic != null)
        this.RoundLogic.OnRoundStart();
      foreach (Player player in this.CurrentLevel.Players)
      {
        player.StopFlashing();
        player.Unfreeze();
      }
      if (this.MatchSettings.SoloMode)
        return;
      if ((bool) this.MatchSettings.Variants.AlwaysDark)
        this.CurrentLevel.OrbLogic.DoDarkOrb();
      if ((bool) this.MatchSettings.Variants.SlowTime)
        this.CurrentLevel.OrbLogic.DoTimeOrb(true);
      if ((bool) this.MatchSettings.Variants.AlwaysLava)
        this.CurrentLevel.OrbLogic.DoLavaVariant();
      if (!(bool) this.MatchSettings.Variants.AlwaysScrolling)
        return;
      this.CurrentLevel.OrbLogic.StartScroll();
    }

    public void EndRound()
    {
      int highestScore = this.GetHighestScore();
      for (int playerIndex = 0; playerIndex < 4; ++playerIndex)
      {
        if (TFGame.Players[playerIndex])
        {
          int scoreIndex = this.GetScoreIndex(playerIndex);
          this.MatchStats[playerIndex].FurthestBehind = (uint) Math.Max((long) this.MatchStats[playerIndex].FurthestBehind, (long) (highestScore - this.Scores[scoreIndex]));
          this.MatchStats[playerIndex].PointsFromGoal = this.Scores[scoreIndex] - this.MatchSettings.GoalScore;
          int num = TFGame.PlayerAmount - this.MatchSettings.GetPlayerTeamSize(playerIndex);
          if (this.RoundLogic.Kills[playerIndex] == num)
            ++this.MatchStats[playerIndex].RoundSweeps;
          if (this.RoundLogic.Kills[playerIndex] == 0 && this.CurrentLevel.GetPlayer(playerIndex) != null)
            ++this.MatchStats[playerIndex].SurvivedWithNoKills;
        }
      }
      if (this.CurrentLevel.ReplayRecorder != null)
      {
        this.CurrentLevel.ReplayRecorder.End();
        this.CurrentLevel.ReplayViewer.Watch(this.CurrentLevel.ReplayRecorder, ReplayViewer.ReplayType.Rewind, new Action(this.CreateResults));
      }
      else
        this.CreateResults();
    }

    protected void CreateResults()
    {
      this.CurrentLevel.Add<HUDFade>(new HUDFade());
      VersusRoundResults roundResults;
      this.CurrentLevel.Add<VersusRoundResults>(roundResults = new VersusRoundResults(this, this.RoundLogic.Events));
      if (this.GetWinner() == -1)
        return;
      VersusMatchResults entity = new VersusMatchResults(this, roundResults);
      roundResults.MatchResults = entity;
      this.CurrentLevel.Add<VersusMatchResults>(entity);
    }

    public void OnUpdate()
    {
      this.SongTimer += Engine.DeltaTicks;
      this.RoundLogic.OnUpdate();
    }

    public void OnLevelLoadFinish() => this.RoundLogic.OnLevelLoadFinish();

    public void OnPlayerDeath(
      Player player,
      PlayerCorpse corpse,
      int playerIndex,
      DeathCause deathType,
      Vector2 position,
      int killerIndex)
    {
      this.RoundLogic.OnPlayerDeath(player, corpse, playerIndex, deathType, position, killerIndex);
    }

    public bool CanHurtPlayer(int ownerIndex, int hurtingIndex)
    {
      if (!(bool) this.MatchSettings.Variants.NoFriendlyFire)
        return true;
      if (!this.MatchSettings.TeamMode && !this.MatchSettings.SoloMode)
        return ownerIndex != hurtingIndex;
      return ownerIndex == -1 || hurtingIndex == -1 || this.MatchSettings.Teams[ownerIndex] != this.MatchSettings.Teams[hurtingIndex];
    }

    public bool ShouldSpawn(int playerIndex)
    {
      if (!TFGame.Players[playerIndex])
        return false;
      return !this.IsInOvertime || this.GetScoreLead(playerIndex) == 0;
    }

    public PlayerInventory GetPlayerInventory(int playerIndex)
    {
      return new PlayerInventory(this.GetSpawnShield(playerIndex), this.GetSpawnWings(playerIndex), this.GetSpawnSpeedBoots(playerIndex), this.GetSpawnInvisible(playerIndex), this.GetSpawnArrows(playerIndex));
    }

    private ArrowList GetSpawnArrows(int playerIndex)
    {
      bool flag = this.GetSpawnHatState(playerIndex) == Player.HatStates.Crown;
      int startArrows;
      if (this.MatchSettings.Variants.NoArrows[playerIndex])
        startArrows = 0;
      else if (this.MatchSettings.Variants.SingleArrow[playerIndex])
        startArrows = 1;
      else if (this.MatchSettings.Variants.MaxArrows[playerIndex])
      {
        startArrows = !flag || (bool) this.MatchSettings.Variants.NoAutobalance ? 6 : 4;
      }
      else
      {
        startArrows = !this.MatchSettings.TeamMode ? 3 : Session.TeamStartArrows[this.MatchSettings.GetPlayerTeamSize(playerIndex) - 1];
        if (!(bool) this.MatchSettings.Variants.NoAutobalance & flag)
          startArrows = Math.Max(2, startArrows - 1);
      }
      return new ArrowList(startArrows, this.MatchSettings.Variants.GetStartArrowType(playerIndex, this.RoundRandomArrowType));
    }

    public ArrowTypes GetRegenArrow(int playerIndex)
    {
      return this.MatchSettings.Variants.GetStartArrowType(playerIndex, this.RoundRandomArrowType);
    }

    public bool GetSpawnShield(int playerIndex)
    {
      if (this.MatchSettings.Variants.StartWithShields[playerIndex])
        return true;
      return !this.IsInOvertime && !(bool) this.MatchSettings.Variants.NoAutobalance && !(bool) this.MatchSettings.Variants.WeakAutobalance && this.GetScoreLead(playerIndex) <= -this.MatchSettings.AutobalanceLosingAmount;
    }

    private bool GetSpawnWings(int playerIndex)
    {
      return this.MatchSettings.Variants.StartWithWings[playerIndex];
    }

    private bool GetSpawnSpeedBoots(int playerIndex)
    {
      return this.MatchSettings.Variants.StartWithSpeedBoots[playerIndex];
    }

    private bool GetSpawnInvisible(int playerIndex)
    {
      return this.MatchSettings.Variants.StartInvisible[playerIndex];
    }

    public Player.HatStates GetSpawnHatState(int playerIndex)
    {
      return (this.Scores[this.GetScoreIndex(playerIndex)] != this.GetHighestScore() ? 0 : (this.Scores[this.GetScoreIndex(playerIndex)] > this.GetLowestScore() ? 1 : 0)) == 0 ? Player.HatStates.Normal : Player.HatStates.Crown;
    }

    public int GetOldScore(int scoreIndex) => this.OldScores[scoreIndex];

    public bool TeamHasCrown(int teamIndex)
    {
      int score = this.Scores[teamIndex];
      bool flag = false;
      for (int index = 0; index < this.Scores.Length; ++index)
      {
        if (this.MatchSettings.TeamMode || TFGame.Players[index])
        {
          if (this.Scores[index] < score)
            flag = true;
          else if (this.Scores[index] > score)
            return false;
        }
      }
      return flag;
    }

    public bool TeamHadCrown(int teamIndex)
    {
      int oldScore = this.GetOldScore(teamIndex);
      bool flag = false;
      for (int scoreIndex = 0; scoreIndex < this.Scores.Length; ++scoreIndex)
      {
        if (this.MatchSettings.TeamMode || TFGame.Players[scoreIndex])
        {
          if (this.GetOldScore(scoreIndex) < oldScore)
            flag = true;
          else if (this.GetOldScore(scoreIndex) > oldScore)
            return false;
        }
      }
      return flag;
    }

    public int GetScoreIndex(int playerIndex)
    {
      if (playerIndex == -1)
        return -1;
      return this.MatchSettings.TeamMode ? (int) this.MatchSettings.GetPlayerAllegiance(playerIndex) : playerIndex;
    }

    public bool WasWinningAtStartOfRound(int playerIndex)
    {
      bool flag = false;
      int scoreIndex1 = this.GetScoreIndex(playerIndex);
      for (int scoreIndex2 = 0; scoreIndex2 < this.Scores.Length; ++scoreIndex2)
      {
        if (scoreIndex2 != scoreIndex1 && TFGame.Players[scoreIndex2])
        {
          if (this.GetOldScore(scoreIndex2) > this.GetOldScore(scoreIndex1))
            return false;
          if (this.GetOldScore(scoreIndex2) < this.GetOldScore(scoreIndex1))
            flag = true;
        }
      }
      return flag;
    }

    public int GetScoreLead(int playerIndex)
    {
      int scoreIndex = this.GetScoreIndex(playerIndex);
      int num = 0;
      for (int index = 0; index < this.Scores.Length; ++index)
      {
        if (index != scoreIndex && this.Scores[index] > num)
          num = this.Scores[index];
      }
      return this.Scores[scoreIndex] - num;
    }

    public bool IsWinningNotAllTied(int playerIndex)
    {
      int score = this.Scores[this.GetScoreIndex(playerIndex)];
      return score == this.GetHighestScore() && score > this.GetLowestScore();
    }

    public bool IsWinning(int playerIndex)
    {
      return this.Scores[this.GetScoreIndex(playerIndex)] == this.GetHighestScore();
    }

    public bool IsLosing(int playerIndex)
    {
      return this.Scores[this.GetScoreIndex(playerIndex)] == this.GetLowestScore();
    }

    public int GetWinner()
    {
      int winner = -1;
      int num = 0;
      for (int index = 0; index < this.Scores.Length; ++index)
      {
        if (this.Scores[index] == num)
          winner = -1;
        else if (this.Scores[index] > num && this.Scores[index] >= this.MatchSettings.GoalScore)
        {
          num = this.Scores[index];
          winner = index;
        }
      }
      return winner;
    }

    public int GetHighestScore()
    {
      int val1 = 0;
      for (int index = 0; index < this.Scores.Length; ++index)
        val1 = Math.Max(val1, this.Scores[index]);
      return val1;
    }

    public int GetLowestScore()
    {
      int val1 = int.MaxValue;
      for (int index = 0; index < this.Scores.Length; ++index)
      {
        if (TFGame.Players[index] || this.MatchSettings.TeamMode)
          val1 = Math.Min(val1, this.Scores[index]);
      }
      return val1;
    }

    public bool IsInOvertime
    {
      get
      {
        if (this.RoundIndex >= 0 && this.MatchSettings.Mode == Modes.HeadHunters)
        {
          for (int index = 0; index < this.Scores.Length; ++index)
          {
            if (this.Scores[index] >= this.MatchSettings.GoalScore)
              return true;
          }
        }
        return false;
      }
    }

    public bool WasInOvertime
    {
      get
      {
        if (this.RoundIndex >= 0 && this.MatchSettings.Mode == Modes.HeadHunters)
        {
          for (int scoreIndex = 0; scoreIndex < this.Scores.Length; ++scoreIndex)
          {
            if (this.GetOldScore(scoreIndex) >= this.MatchSettings.GoalScore)
              return true;
          }
        }
        return false;
      }
    }
  }
}
