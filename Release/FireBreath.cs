// Decompiled with JetBrains decompiler
// Type: XRL.World.Parts.Mutation.FireBreath
// Assembly: Assembly-CSharp, Version=2.0.6684.37340, Culture=neutral, PublicKeyToken=null
// MVID: 5B4FB8C1-2DD4-47AE-B531-7F4329DC0775
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed\Assembly-CSharp.dll
// This code tries to add a Fire Breathing Dragon type attack.  It uses flaming hands as its original code source.

using ConsoleLib.Console;
using System;
using System.Collections.Generic;
using XRL.Core;
using XRL.Rules;
using XRL.UI;
using XRL.World.AI.GoalHandlers;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class rr_FireBreath : BaseMutation
    {
        public Guid myActivatedAbilityID = Guid.Empty;
        public ActivatedAbilityEntry myActivatedAbility = null;
        public int OldFlame = -1;
        public int OldVapor = -1;
        public int VarCooldown = 25;
        public GameObject mySource;

        // Internal variables for simplifying code; Locate strings at top for easy change.
        private string[] pC = { "&r", "&R", "&W", "^r", "^R", "^W" };
        private string equipString = "Your fire breath prevents you from equipping {0}!";

        public rr_FireBreath()
        {
            Name = nameof(rr_FireBreath);
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
            return "You can breathe fire.";
        }

        public override string GetLevelText(int Level)
        {
            return "Emits a 9-square ray of flame in the direction of your choice\n" +
                   "Cooldown: " + VarCooldown + " rounds\n" +
                   "Damage: " + Level + "d4+1\n" +
                   "You can't wear anything on your face.";
        }

        public void Flame(Cell C, ScreenBuffer Buffer)
        {
            string Damage = Level + "d4+1";
            int TempChange = 310 + (25 * Level);

            if(C != null)
            {
                foreach(GameObject GO in C.GetObjectsInCell())
                {
                    if(GO.PhasedMatches(ParentObject))
                    {
                        GO.FireEvent(Event.New("TemperatureChange", "Amount", TempChange, "Owner", ParentObject));
                        for (int x = 0; x < 5; x++) GO.ParticleText(pC[0] + (char)(219 + Stat.Random(0, 4)), 2.9f, 1);
                        for (int x = 0; x < 5; x++) GO.ParticleText(pC[1] + (char)(219 + Stat.Random(0, 4)), 2.9f, 1);
                        for (int x = 0; x < 5; x++) GO.ParticleText(pC[2] + (char)(219 + Stat.Random(0, 4)), 2.9f, 1);
                    }
                }

                foreach(GameObject GO in C.GetObjectsWithPart("Combat"))
                {
                    if(GO.PhasedMatches(ParentObject))
                    {
                        Damage Dmg = new Damage(Stat.Roll(Damage));
                        Dmg.AddAttribute("Fire");
                        Dmg.AddAttribute("Heat");

                        Event eTakeDamage = Event.New("TakeDamage");
                        eTakeDamage.AddParameter("Damage", Dmg);
                        eTakeDamage.AddParameter("Owner", ParentObject);
                        eTakeDamage.AddParameter("Attacker", ParentObject);
                        eTakeDamage.AddParameter("Message", "from %o flames!");

                        GO.FireEvent(eTakeDamage);
                    }
                }
            }

            Buffer.Goto(C.X, C.Y);

            if (C.ParentZone != XRLCore.Core.Game.ZoneManager.ActiveZone) return;
            
            // More Particle Effects?
            string sColor = pC[Stat.Random(0, 2)];
            sColor += pC[Stat.Random(3, 5)];

            Buffer.Write(sColor + (char)(219 + Stat.Random(0, 4)));
            Popup._TextConsole.DrawBuffer(Buffer);
            System.Threading.Thread.Sleep(10);
        }

        public static bool Cast(rr_FireBreath mutation = null, string level = "5-6")
        {
            if(mutation == null)
            {
                mutation = new rr_FireBreath();
                mutation.Level = Stat.Roll(level);
                mutation.ParentObject = XRLCore.Core.Game.Player.Body;
            }
            ScreenBuffer Buffer = new ScreenBuffer(80, 25);
            XRLCore.Core.RenderMapToBuffer(Buffer);
            List<Cell> TargetCell = mutation.PickLine(9, AllowVis.Any, null);
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
                    mutation.Flame(TargetCell[index], Buffer);
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
                List<AICommandList> CommandList = (List<AICommandList>)E.GetParameter("List");

                if(myActivatedAbility != null &&
                    myActivatedAbility.Cooldown <= 0 &&
                    Distance <= 9 &&
                    ParentObject.HasLOSTo(Target))
                {
                    CommandList.Add(new AICommandList("rr_CommandFireBreath", 1));
                }
                return true;
            }

            if(E.ID == "rr_CommandFireBreath")
            {
                return rr_FireBreath.Cast(this, "5-6");
            }

            if(E.ID == "BeginEquip")
            {
                GameObject Equipment = E.GetParameter("Object") as GameObject;
                string BodyPartName = E.GetParameter("BodyPartName") as string;

                if(BodyPartName == "Face")
                {
                    if(IsPlayer())
                    {
                        Popup.Show(String.Format(equipString, Equipment.DisplayName));
                    }
                    return false;
                }
            }
            return base.FireEvent(E);
        }

        public override bool ChangeLevel(int NewLevel)
        {
            //
            //

            TemperatureOnHit pTemp = mySource.GetPart("TemperatureOnHit") as TemperatureOnHit;
            pTemp.Amount = (Level * 2) + "d8";

            return base.ChangeLevel(NewLevel);
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            Unmutate(GO);

            Physics pPhysics = GO.GetPart("Physics") as Physics;
            if(pPhysics != null)
            {
                OldFlame = pPhysics.FlameTemperature;
                OldVapor = pPhysics.VaporTemperature;
            }

            Body pBody = GO.GetPart("Body") as Body;
            if(pBody != null)
            {
                GO.FireEvent(Event.New("CommandForceUnequipObject", "BodyPartName", "Face"));
                mySource = GameObjectFactory.Factory.CreateObject("Smokey Breath");
                Event eCommandEquipObject = Event.New("CommandEquipObject");
                eCommandEquipObject.AddParameter("Object", mySource);
                eCommandEquipObject.AddParameter("BodyPartName", "Face");
                GO.FireEvent(eCommandEquipObject);
            }

            ActivatedAbilities pAA = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;

            myActivatedAbilityID = pAA.AddAbility("Breathe Fire", "rr_CommandFireBreath", "Physical Mutation");
            myActivatedAbility = pAA.AbilityByGuid[myActivatedAbilityID];
            return true;
        }

        public override bool Unmutate(GameObject GO)
        {
            Physics pPhysics = GO.GetPart("Physics") as Physics;
            if(pPhysics != null)
            {
                if(OldFlame != -1) pPhysics.FlameTemperature = OldFlame;
                if(OldVapor != -1) pPhysics.BrittleTemperature = OldVapor;
                OldFlame = -1;
                OldVapor = -1;

                pPhysics.Temperature = 25;
            }

            Body pBody = GO.GetPart("Body") as Body;
            if(pBody != null)
            {
                BodyPart pMainBody = pBody.GetPartByName("Face");
                if(pMainBody != null && pMainBody.Equipped != null &&
                    pMainBody.Equipped.Blueprint == "Smokey Breath")
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
