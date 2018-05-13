// Decompiled with JetBrains decompiler
// Type: XRL.World.Parts.Mutation.IceBreath
// Assembly: Assembly-CSharp, Version=2.0.6684.37340, Culture=neutral, PublicKeyToken=null
// MVID: 5B4FB8C1-2DD4-47AE-B531-7F4329DC0775
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed\Assembly-CSharp.dll
// This code tries to add a Ice Breathing Dragon type attack.  It uses freezing hands as its original code source.

using ConsoleLib.Console;
using System;
using System.Collections.Generic;
using System.Text;
using XRL.Core;
using XRL.Rules;
using XRL.World.Parts;
using XRL.UI;
using XRL.Messages;

namespace XRL.World.Parts.Mutation
{
  [Serializable]
  public class rr_IceBreath : BaseMutation
  {
    public Guid myActivatedAbilityID = Guid.Empty;
    public ActivatedAbilityEntry myActivatedAbility = null;
    public int OldFreeze = -1;
    public int OldBrittle = -1;
    public int VarCooldown = 25;
    public GameObject mySource;

    public rr_IceBreath()
    {
      Name = "rr_IceBreath";
      DisplayName = "Ice Breath";
    }

    public override void Register(GameObject Object)
    {
      Object.RegisterPartEvent(this, "BeginEquip");
      Object.RegisterPartEvent(this, "rr_CommandIceBreath");
      Object.RegisterPartEvent(this, "AIGetOffensiveMutationList");
    }

    public override string GetDescription()
    {
      return "You can breathe ice.";
    }

    public override string GetLevelText(int Level)
    {
      return "Emits a 9-square ray of frost in the direction of your choice\n" +
             "Cooldown: " + VarCooldown + " rounds\n" +
             "Damage: " + Level + "d3+1\n" +
             "You can't wear anything on your face.";
    }

    public void Freeze(Cell C, ScreenBuffer Buffer)
    {
      string Damage = Level + "d3+1";
      int TempChange = -120 - (7 * Level);

      if(C != null)
      {
        foreach(GameObject GO in C.GetObjectsInCell())
        {
          if(GO.PhasedMatches(ParentObject))
          {
            GO.FireEvent(Event.New("TemperatureChange", "Amount", TempChange, "Owner", ParentObject));
            for (int x = 0; x < 5; x++) GO.ParticleText("&C" + (char)(219 + Rules.Stat.Random(0, 4)), 2.9f, 1);
            for (int x = 0; x < 5; x++) GO.ParticleText("&c" + (char)(219 + Rules.Stat.Random(0, 4)), 2.9f, 1);
            for (int x = 0; x < 5; x++) GO.ParticleText("&Y" + (char)(219 + Rules.Stat.Random(0, 4)), 2.9f, 1);
          }
        }

        foreach(GameObject GO in C.GetObjectsWithPart("Combat"))
        {
          if(GO.PhasedMatches(ParentObject))
          {
            Damage Dmg = new Damage(Rules.Stat.Roll(Damage));
            // Filler
            Dmg.AddAttribute("Cold");

            Event eTakeDamage = Event.New("TakeDamage");
            eTakeDamage.AddParameter("Damage", Dmg);
            eTakeDamage.AddParameter("Owner", ParentObject);
            eTakeDamage.AddParameter("Attacker", ParentObject);
            eTakeDamage.AddParameter("Message", "from %o freezing effect!");

            GO.FireEvent(eTakeDamage);
          }
        }
      }

      Buffer.Goto(C.X, C.Y);
      string sColor = "&C";

      int r = Rules.Stat.Random(1, 3);
      if(r == 1) sColor = "&C";
      if(r == 2) sColor = "&B";
      if(r == 3) sColor = "&Y";

      r = Rules.Stat.Random(1, 3);
      if(r == 1) sColor += "^C";
      if(r == 2) sColor += "^B";
      if(r == 3) sColor += "^Y";

      if(C.ParentZone != XRL.Core.XRLCore.Core.Game.ZoneManager.ActiveZone) return;

      r = Rules.Stat.Random(1, 3);
      Buffer.Write(sColor + (char)(219 + Rules.Stat.Random(0, 4)));
      Popup._TextConsole.DrawBuffer(Buffer);
      System.Threading.Thread.Sleep(10);
    }

    public static bool Cast(rr_IceBreath mutation = null, string level = "5-6")
    {
      if(mutation == null)
      {
        mutation = new rr_IceBreath();
        mutation.Level = Stat.Roll(level);
        mutation.ParentObject = XRLCore.Core.Game.Player.Body;
      }
      ScreenBuffer Buffer = new ScreenBuffer(80, 25);
      XRLCore.Core.RenderMapToBuffer(Buffer);
      List<Cell> TargetCell = mutation.PickLine(9, AllowVis.Any, (Func<GameObject, bool>) null);
      if(TargetCell != null || TargetCell.Count > 0)
      {
        if(TargetCell.Count == 1 && mutation.ParentObject.IsPlayer())
        {
          if(Popup.ShowYesNoCancel("Are you sure you want to target yourself?") != DialogResult.Yes)
          {
            return true;
          }
        }
      }
      if(mutation.myActivatedAbility != null)
      {
        mutation.myActivatedAbility.Cooldown = (mutation.VarCooldown + 1) * 10;
      }
      mutation.ParentObject.FireEvent(Event.New("UseEnergy", "Amount", 1000, "Type", "Physical Mutation"));
      for(int index = 0; index < 9 && index < TargetCell.Count; ++index)
      {
        if(TargetCell.Count == 1 || TargetCell[index] != mutation.ParentObject.pPhysics.CurrentCell)
        {
          mutation.Freeze(TargetCell[index], Buffer);
        }
        foreach(GameObject gameObject in TargetCell[index].GetObjectsWithPart("Physics"))
        {
          if(gameObject.pPhysics.Solid && gameObject.GetIntProperty("AllowMissiles", 0) == 0)
          {
            index = 999;
            break;
          }
        }
      }
      return true;
    }

    public override bool FireEvent(Event E)
    {
      // When should the AI use this ability?
      if(E.ID == "AIGetOffensiveMutationList")
      {
        int Distance = (int)E.GetParameter("Distance");
        GameObject Target = E.GetParameter("Target") as GameObject;
        List<XRL.World.AI.GoalHandlers.AICommandList> CommandList = (List<XRL.World.AI.GoalHandlers.AICommandList>)E.GetParameter("List");

        if(myActivatedAbility != null &&
           myActivatedAbility.Cooldown <= 0 &&
           Distance <= 9 &&
           ParentObject.HasLOSTo(Target))
        {
          CommandList.Add(new XRL.World.AI.GoalHandlers.AICommandList("rr_CommandIceBreath", 1));
        }
        return true;
      }

      if(E.ID == "rr_CommandIceBreath")
      {
        return rr_IceBreath.Cast(this, "5-6");
      }

      if(E.ID == "BeginEquip")
      {
        GameObject Equipment = E.GetParameter("Object") as GameObject;
        string BodyPartName = E.GetParameter("BodyPartName") as string;

        if(BodyPartName == "Face")
        {
          if(IsPlayer())
          {
            UI.Popup.Show("Your ice breath prevents you from equipping " + Equipment.DisplayName + "!");
          }
          return false;
        }
      }
      return base.FireEvent(E);
    }

    public override bool ChangeLevel(int NewLevel)
    {
      Physics pPhysics = ParentObject.GetPart("Physics") as Physics;
      pPhysics.BrittleTemperature = -300 * Level - 600;

      TemperatureOnHit pTemp = mySource.GetPart("TemperatureOnHit") as TemperatureOnHit;
      pTemp.Amount = "-" + Level + "d4";

      return base.ChangeLevel(NewLevel);
    }

    public override bool Mutate(GameObject GO, int Level)
    {
      Unmutate(GO);

      Physics pPhysics = GO.GetPart("Physics") as Physics;
      if(pPhysics != null)
      {
        OldFreeze = pPhysics.FreezeTemperature;
        OldBrittle = pPhysics.BrittleTemperature;
      }

      Body pBody = GO.GetPart("Body") as Body;
      if(pBody != null)
      {
        GO.FireEvent(Event.New("CommandForceUnequipObject", "BodyPartName", "Face"));
        mySource = GameObjectFactory.Factory.CreateObject("Icy Breath");
        Event eCommandEquipObject = Event.New("CommandEquipObject");
        eCommandEquipObject.AddParameter("Object", mySource);
        eCommandEquipObject.AddParameter("BodyPartName", "Face");
        GO.FireEvent(eCommandEquipObject);
      }

      ActivatedAbilities pAA = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;

      myActivatedAbilityID = pAA.AddAbility("Breathe Ice", "rr_CommandIceBreath", "Physical Mutation");
      myActivatedAbility = pAA.AbilityByGuid[myActivatedAbilityID];
      return true;
    }

    public override bool Unmutate(GameObject GO)
    {
      Physics pPhysics = GO.GetPart("Physics") as Physics;
      if(pPhysics != null)
      {
        if(OldFreeze != -1) pPhysics.FreezeTemperature = OldFreeze;
        if(OldBrittle != -1) pPhysics.BrittleTemperature = OldBrittle;
        OldFreeze = -1;
        OldBrittle = -1;

        pPhysics.Temperature = 25;
      }

      Body pBody = GO.GetPart("Body") as Body;
      if(pBody != null)
      {
        BodyPart pMainBody = pBody.GetPartByName("Face");
        if(pMainBody != null && pMainBody.Equipped != null &&
           pMainBody.Equipped.Blueprint == "Icy Breath")
        {
          pMainBody.Equipped.FireEvent(Event.New("Unequipped", "UnequippingObject", ParentObject, "BodyPart", pMainBody));
          pMainBody.Unequip();
        }
      }

      if(myActivatedAbilityID != Guid.Empty)
      {
        ActivatedAbilities pAA = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;
        pAA.RemoveAbility(myActivatedAbilityID);
        myActivatedAbilityID = Guid.Empty;
      }

      return true;
    }
  }
}
