// Decompiled with JetBrains decompiler
// Type: XRL.World.Parts.Mutation.LightningBreath
// Assembly: Assembly-CSharp, Version=2.0.6684.37340, Culture=neutral, PublicKeyToken=null
// MVID: 5B4FB8C1-2DD4-47AE-B531-7F4329DC0775
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed\Assembly-CSharp.dll
// This code tries to add a Lightning Breathing Dragon type attack.

using ConsoleLib.Console;
using System;
using System.Collections.Generic;
using System.Text;
using XRL.Core;
using XRL.Rules;
using XRL.World.Parts;
using XRL.UI;
using XRL.Messages;
using XRL.World.AI.GoalHandlers;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class rr_LightningBreath : BaseMutation
    {
        public int nCharges = 3;
        public int nTurnCounter;
        public int OldConductivity;
        public Guid myActivatedAbilityID = Guid.Empty;
        public ActivatedAbilityEntry myActivatedAbility = null;
        public int VarCooldown = 25;
        public GameObject mySource;
        private string[] pC = { "&W", "&Y", "&y" };

        public rr_LightningBreath()
        {
            Name = nameof(rr_LightningBreath);
            DisplayName = "Lightning Breath";
        }

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, "BeginEquip");
            Object.RegisterPartEvent(this, "EndTurn");
            Object.RegisterPartEvent(this, "rr_CommandLightningBreath");
            Object.RegisterPartEvent(this, "AIGetOffensiveMutationList");
        }

        public override string GetDescription()
        {
            return "You can breathe lightning.";
        }

        public override string GetLevelText(int Level)
        {
            return "Emits a 9-square ray of lightning in the direction of your choice.\n" +
                   "Accrue an additional charge every " + Math.Ceiling((Decimal)(VarCooldown / (2+Level))) +
                   " rounds up to the maximum of " + (2+Level) + " charges.\n" +
                   "Damage per charge: 1d4.\n" +
                   "Electricity will arc to adjacent targets dealing reduced damage.\n" +
                   "You can't wear anything on your face.";
        }

        // Update the ability string to show the number of charges to be used.
        public void UpdateAbility()
        {
            if (myActivatedAbility == null) return;
            StringBuilder stringBuilder = Event.NewStringBuilder((string)null);
            stringBuilder.Append("Breathe Lightning [").Append(nCharges).Append(" charges]");
            myActivatedAbility.DisplayName = stringBuilder.ToString();
            if (nCharges == 0) myActivatedAbility.Enabled = false;
            else               myActivatedAbility.Enabled = true;
        }

        public void Spawn(Cell C)
        {
            Event eDischarge = Event.New("Discharge");
            eDischarge.AddParameter("Owner", ParentObject);
            eDischarge.AddParameter("TargetCell", C);
            eDischarge.AddParameter("Voltage", nCharges);
            eDischarge.AddParameter("Damage", nCharges.ToString() + "d4");
            ParentObject.FireEvent(eDischarge);
        }

        public void Breathe(Cell C, ScreenBuffer Buffer)
        {
            string Damage = nCharges + "d4";

            if(C != null)
            {
                foreach(GameObject GO in C.GetObjectsInCell())
                {
                    if(GO.PhasedMatches(ParentObject))
                    {
                        for (int x = 0; x < 5; x++) GO.ParticleText(pC[0] + (char)(219 + Stat.Random(0, 4)), 2.9f, 1);
                        for (int x = 0; x < 5; x++) GO.ParticleText(pC[1] + (char)(219 + Stat.Random(0, 4)), 2.9f, 1);
                        for (int x = 0; x < 5; x++) GO.ParticleText(pC[2] + (char)(219 + Stat.Random(0, 4)), 2.9f, 1);
                    }
                }

                Spawn(C);
            }

            Buffer.Goto(C.X, C.Y);
            string sColor = "&C";

            int r = Stat.Random(1, 3);
            if(r == 1) sColor = "&W";
            if(r == 2) sColor = "&Y";
            if(r == 3) sColor = "&y";

            r = Stat.Random(1, 3);
            if(r == 1) sColor += "^W";
            if(r == 2) sColor += "^Y";
            if(r == 3) sColor += "^y";

            if(C.ParentZone != XRLCore.Core.Game.ZoneManager.ActiveZone) return;

            r = Stat.Random(1, 3);
            Buffer.Write(sColor + (char)(219 + Stat.Random(0, 4)));
            Popup._TextConsole.DrawBuffer(Buffer);
            System.Threading.Thread.Sleep(10);
        }

        public static bool Cast(rr_LightningBreath mutation = null, string level = "5-6")
        {
            // Make a random mutation if one doesn't exist on the source.
            if(mutation == null)
            {
                mutation = new rr_LightningBreath();
                mutation.Level = Stat.Roll(level);
                mutation.ParentObject = XRLCore.Core.Game.Player.Body;
            }

            // Pick the target.
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

            // Apply 1 turn of energy usage to the player.
            mutation.ParentObject.FireEvent(Event.New("UseEnergy", "Amount", 1000, "Type", "Physical Mutation"));

            // Shoot out the line of lightning.
            for (int index = 0; index < 9 && index < TargetCell.Count; ++index)
            {
                if(TargetCell.Count == 1 || TargetCell[index] != mutation.ParentObject.pPhysics.CurrentCell)
                {
                    mutation.Breathe(TargetCell[index], Buffer);
                }
                foreach(GameObject gameObject in TargetCell[index].GetObjectsWithPart("Physics"))
                {
                    // Stop if a solid object or a missle blocker is hit.
                    if(gameObject.pPhysics.Solid && gameObject.GetIntProperty("AllowMissiles", 0) == 0)
                    {
                        index = 999;
                        break;
                    }
                }
            }

            // Clear the number of charges.
            mutation.nCharges = 0;
            mutation.UpdateAbility();

            return true;
        }

        public override bool FireEvent(Event E)
        {
            // Check the turn counter at the end of turn, and update the number of charges if needed.
            if(E.ID == "EndTurn")
            {
                if (myActivatedAbility == null) return true;
                ++nTurnCounter;
                if (nTurnCounter >= Math.Ceiling((Decimal)(VarCooldown / (2+Level))))
                {
                    nTurnCounter = 0;
                    if (nCharges < (2 + Level)) ++nCharges;
                }
                UpdateAbility();
                return true;
            }
            // When should the AI use this ability?
            if(E.ID == "AIGetOffensiveMutationList")
            {
                // Stop of the AI doesn't have the ability, or it has no charges left.
                if (myActivatedAbility == null) return true;
                if (nCharges <= 0) return true;

                int Distance = (int)E.GetParameter("Distance");
                GameObject Target = E.GetParameter("Target") as GameObject;
                List<AICommandList> CommandList = (List<AICommandList>)E.GetParameter("List");

                // Get the Distance and Line of Sight to the Target and use the ability if able.
                if(Distance <= 9 && ParentObject.HasLOSTo(Target))
                {
                    CommandList.Add(new AICommandList("rr_CommandLightningBreath", 1));
                }
                return true;
            }

            if(E.ID == "rr_CommandLightningBreath")
            {
                if (nCharges == 0) return false;

                return Cast(this, "5-6");
            }

            if(E.ID == "BeginEquip")
            {
                GameObject Equipment = E.GetParameter("Object") as GameObject;
                string BodyPartName = E.GetParameter("BodyPartName") as string;

                if(BodyPartName == "Face")
                {
                    if(IsPlayer())
                    {
                        Popup.Show("Your lightning breath prevents you from equipping " + Equipment.DisplayName + "!");
                    }
                    return false;
                }
            }
            return base.FireEvent(E);
        }

        public override bool ChangeLevel(int NewLevel)
        {
            nCharges = 2 + Level;
            UpdateAbility();

            return base.ChangeLevel(NewLevel);
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            Unmutate(GO);

            // Disable conductivity to the Creature/Player.
            Physics pPhysics = GO.GetPart("Physics") as Physics;
            if(pPhysics != null)
            {
                OldConductivity = pPhysics.Conductivity;
                pPhysics.Conductivity = 0;
            }

            // Add sparking breath to the Face of the Creature/Player.
            Body pBody = GO.GetPart("Body") as Body;
            if(pBody != null)
            {
                GO.FireEvent(Event.New("CommandForceUnequipObject", "BodyPartName", "Face"));
                mySource = GameObjectFactory.Factory.CreateObject("Sparking Breath");
                Event eCommandEquipObject = Event.New("CommandEquipObject");
                eCommandEquipObject.AddParameter("Object", mySource);
                eCommandEquipObject.AddParameter("BodyPartName", "Face");
                GO.FireEvent(eCommandEquipObject);
            }

            // Add the ability.
            ActivatedAbilities AA = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;
            myActivatedAbilityID = AA.AddAbility("Breathe Lightning", "rr_CommandLightningBreath", "Physical Mutation");
            myActivatedAbility = AA.AbilityByGuid[myActivatedAbilityID];
            return true;
        }

        public override bool Unmutate(GameObject GO)
        {
            // Restore conductivity to the Creature/Player.
            Physics pPhysics = GO.GetPart("Physics") as Physics;
            if(pPhysics != null)
            {
                pPhysics.Conductivity = OldConductivity;
                OldConductivity = 0;
            }

            // Remove sparking breath from the Face of the Creature/Player.
            Body pBody = GO.GetPart("Body") as Body;
            if(pBody != null)
            {
                BodyPart pFace = pBody.GetPartByName("Face");
                if(pFace != null && pFace.Equipped != null &&
                pFace.Equipped.Blueprint == "Sparking Breath")
                {
                    pFace.Equipped.FireEvent(Event.New("Unequipped", "UnequippingObject", ParentObject, "BodyPart", pFace));
                    pFace.Unequip();
                }
            }

            // Remove the ability.
            if(myActivatedAbilityID != Guid.Empty)
            {
                ActivatedAbilities AA = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;
                AA.RemoveAbility(myActivatedAbilityID);
                myActivatedAbilityID = Guid.Empty;
            }

            return true;
        }
    }
}
