using System;
using System.Collections.Generic;
using System.Text;

using XRL.Rules;
using XRL.World.Parts;
using XRL.Messages;
using ConsoleLib.Console;

namespace XRL.World.Parts.Mutation
{
  [Serializable]
  class FireBreath : BaseMutation
  {
    public Guid FireBreathActivatedAbilityID = Guid.Empty;
    public int OldFlame = -1;
    public int OldVapor = -1;
    public int VarCooldown = 25;
    public ActivatedAbilityEntry FireBreathActivatedAbility = null;
    public GameObject FlamesObject;

    public FireBreath()
    {
      Name = "FireBreath";
      DisplayName = "Fire Breath";
    }

    public override void Register(GameObject Object)
    {
      Object.RegisterPartEvent(this, "BeginEquip");
      Object.RegisterPartEvent(this, "rr_CommandFireBreath");
      Object.RegisterPartEvent(this, "AIGetOffensiveMutationList");
    }

    public override string GetDescription()
    {
      return "You can breath fire.";
    }

    public override string GetLevelText(int Level)
    {
      return "Emits a 9-square ray of flame in the direction of your choice\n" +
             "Cooldown: " + VarCooldown + " rounds\n" +
             "Damage: " + Level + "d4+1\n" +
             "You can't wear anything on your face.";
    }

    public override bool FireEvent(Event E)
    {
      //if (E.ID == "rr_CommandFireBreath") return true
      return base.FireEvent(E);
    }

    public override bool ChangeLevel(int NewLevel)
    {
      TemperatureOnHit pTemp = FlamesObject.GetPart("TemperatureOnHit") as TemperatureOnHit;
      pTemp.Amount = (Level * 2) + "d8";

      return base.ChangeLevel(NewLevel);
    }

    public override bool Mutate(GameObject GO, int Level)
    {
      Unmutate(GO);

      Physics pPhysics = GO.GetPart("Physics") as Physics;

      if (pPhysics != null)
      {
          OldFlame = pPhysics.FlameTemperature;
          OldVapor = pPhysics.VaporTemperature;
      }

      Body pBody = GO.GetPart("Body") as Body;
      if (pBody != null)
      {

      }

      ActivatedAbilities pAA = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;

      FireBreathActivatedAbilityID = pAA.AddAbility("Breath Fire", "rr_CommandFireBreath", "Physical Mutation");
      FireBreathActivatedAbility = pAA.AbilityByGuid[FireBreathActivatedAbilityID];
      return true;
    }

    public override bool Unmutate(GameObject GO)
    {
      Physics pPhysics = GO.GetPart("Physics") as Physics;

      if (pPhysics != null)
      {
        if (OldFlame != -1) pPhysics.FlameTemperature = OldFlame;
        if (OldVapor != -1) pPhysics.BrittleTemperature = OldVapor;
        OldFlame = -1;
        OldVapor = -1;

        pPhysics.Temperature = 25;
      }

      Body pBody = GO.GetPart("Body") as Body;
      if (pBody != null)
      {

      }


      if (FireBreathActivatedAbilityID != Guid.Empty)
      {
        ActivatedAbilities pAA = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;
        pAA.RemoveAbility(FireBreathActivatedAbilityID);
        FireBreathActivatedAbilityID = Guid.Empty;
      }

      return true;
    }
  }
}
