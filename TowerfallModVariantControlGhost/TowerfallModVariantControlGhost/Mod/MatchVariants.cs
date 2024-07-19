using Patcher;
using System.Reflection;
using TowerFall;

namespace ModVariantControlGhost { 
  [Patch]
  public class MyMatchVariants : MatchVariants
  {
    [Header("ARCHERS")]
    [PerPlayer]
    [Description("Use Left and Right Bumper (LB/LR and not LT/RT) to use Ghost mode in the select direction")]
    public Variant Ghost;
    [PerPlayer]
    [Description("Use Left and Right Bumper (LB/LR and not LT/RT) to use Ghost mode to a Random position")]
    public Variant GhostRandom;

    public MyMatchVariants(bool noPerPlayer = false) : base(noPerPlayer)
    {
      this.CreateLinks(Ghost, GhostRandom);
    }

    private static string GetVariantTitle(FieldInfo field)
    {
      string str = MatchVariants.GetVariantTitle(field);
      return str.Replace("_", " ");
    }
  }
}
