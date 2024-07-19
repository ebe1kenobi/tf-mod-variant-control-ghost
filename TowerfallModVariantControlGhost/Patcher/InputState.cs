using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Patcher
{
  public static class MyInputState
  {
    public static void PatchModule(ModuleDefinition baseModule)
    {
      // find struct instruction and replace by class
      var type = baseModule.AllNestedTypes().Single(t => t.FullName == "TowerFall.InputState");
      type.Fields.Add(new FieldDefinition("ShoulderCheck", Mono.Cecil.FieldAttributes.Public, baseModule.ImportReference(typeof(Boolean))));
      type.Fields.Add(new FieldDefinition("ShoulderPressed", Mono.Cecil.FieldAttributes.Public, baseModule.ImportReference(typeof(Boolean))));
      type.Fields.Add(new FieldDefinition("ShoulderReleased", Mono.Cecil.FieldAttributes.Public, baseModule.ImportReference(typeof(Boolean))));
      type.Fields.Add(new FieldDefinition("unused", Mono.Cecil.FieldAttributes.Public, baseModule.ImportReference(typeof(Boolean))));
    }
  }
}
