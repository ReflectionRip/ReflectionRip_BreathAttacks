// Decompiled with JetBrains decompiler
// Type: XRL.World.Parts.Mutation.LightningBreath
// This code tries to add a Lightning Breathing Dragon type attack.

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
    public class rr_LightningBreath : BaseMutation
    {
        public Guid myActivatedAbilityID = Guid.Empty;
        public ActivatedAbilityEntry myActivatedAbility = null;
        public GameObject mySource;

        public int VarCooldown = 25;
        public string DamageDie = "d4";

        public int nCharges = 3;
        public int nTurnCounter;
        public int OldConductivity;

        // My attempt of preparing for multi-language support.
        // Wanted to use a resource file, but couldn't get it to work correctly.
        // Also wanted to seperate the strings from the source code, to allow for better reuse.
        private class Resources
        {
            private static char[] Colors = { 'C', 'W', 'Y' }; // Brright Cyan, Yellow, White
            // 7 of the single line characters; not including the 4 corner characters.
            private static char[] Particles = { '\u00B3', '\u00C4', '\u00C5', '\u00B4', '\u00C1', '\u00C2', '\u00C3' }; 
            internal static string displayName { get {
                    return "Lightning Breath"; } }
            internal static string commandName { get {
                    return "Breathe Lightning [{0} charges]"; } }
            internal static string faceItem { get {
                    return "Sparking Breath"; } }
            internal static string description { get {
                    return "You can breathe lightning."; } }
            internal static string targetSelf { get {
                    return "Are you sure you want to target yourself?"; } }
            internal static string equipError { get {
                    return "Your lightning breath prevents you from equipping {0}!"; } }
            internal static string levelText { get {
                    return "Emits a 9-square ray of lightning in the direction of your choice.\n" +
                           "Accrue an additional charge every {0} rounds up to the maximum of {1} charges.\n" +
                           "Damage per charge: {2}\n" +
                           "Electricity will arc to adjacent targets dealing reduced damage.\n" +
                           "You can't wear anything on your face."; } }
            internal static string damageMessage { get {
                    return "from %o lightning!"; } }
            internal static char Color { get {
                    Stat.Random(1, 3);
                    return Colors[Stat.Random(0, Colors.Length - 1)]; } }
            internal static char Particle { get {
                    Stat.Random(1, 3);
                    return Particles[Stat.Random(0, Particles.Length - 1)]; } }
        }

        public rr_LightningBreath()
        {
            Name = nameof(rr_LightningBreath);
            DisplayName = Resources.displayName;
        }

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, "BeginEquip");
            Object.RegisterPartEvent(this, "EndTurn");
            Object.RegisterPartEvent(this, "rr_CommandBreath");
            Object.RegisterPartEvent(this, "AIGetOffensiveMutationList");
        }

        public override string GetDescription()
        {
            return Resources.description;
        }

        public override string GetLevelText(int Level)
        {
            return String.Format(Resources.levelText, Math.Ceiling((Decimal)(VarCooldown / (2 + Level))), (2 + Level), '1' + DamageDie);
        }

        // Update the ability string to show the number of charges to be used.
        public void UpdateAbility()
        {
            if (myActivatedAbility == null) return;
            myActivatedAbility.DisplayName = string.Format(Resources.commandName,nCharges);
            if (nCharges == 0) myActivatedAbility.Enabled = false;
            else               myActivatedAbility.Enabled = true;
        }

        public void Spawn(Cell C, ScreenBuffer Buffer, int Distance = 0)
        {
            if (C == null) return;

            // Apply discarge to the cell, all objects that can take electricity damage will.
            Event eDischarge = Event.New("Discharge");
            eDischarge.AddParameter("Owner", ParentObject);
            eDischarge.AddParameter("TargetCell", C);
            eDischarge.AddParameter("Voltage", nCharges);
            eDischarge.AddParameter("Damage", nCharges.ToString() + DamageDie);
            ParentObject.FireEvent(eDischarge);

            // Add particle effects, but only in visible active zones.
            if (C.ParentZone.IsActive() && C.IsVisible())
            {
                for (int Fade = 0; Fade < 3; ++Fade)
                {
                    // Pick a Forground color, and particle.
                    string str1 = "&" + Resources.Color + Resources.Particle;

                    XRLCore.ParticleManager.Add(str1, C.X, C.Y, 0.0f, 0.0f, 10 + 2 * Distance + (6 - 2 * Fade));
                }
            }
        }

        public bool Breathe()
        {
            // Pick the target.
            ScreenBuffer Buffer = new ScreenBuffer(80, 25);
            XRLCore.Core.RenderMapToBuffer(Buffer);
            List<Cell> TargetCells = PickLine(9, AllowVis.Any, (Func<GameObject, bool>) null);
            if (TargetCells == null || TargetCells.Count <= 1) return false;

            // Shoot out the breath line.
            for (int Distance = 0; Distance < 9 && Distance < TargetCells.Count; ++Distance)
            {
                if(TargetCells.Count == 1 || TargetCells[Distance] != ParentObject.pPhysics.CurrentCell)
                {
                    Spawn(TargetCells[Distance], Buffer, Distance);
                }
                foreach(GameObject gameObject in TargetCells[Distance].GetObjectsWithPart("Physics"))
                {
                    // Stop if a solid object or a missle blocker is hit.
                    if(gameObject.pPhysics.Solid && gameObject.GetIntProperty("AllowMissiles", 0) == 0)
                    {
                        Distance = 999;
                        break;
                    }
                }
            }

            // Apply 1 turn of energy usage to the player.
            ParentObject.FireEvent(Event.New("UseEnergy", "Amount", 1000, "Type", "Physical Mutation"));

            // Clear the number of charges.
            nCharges = 0;
            UpdateAbility();

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
                    CommandList.Add(new AICommandList("rr_CommandBreath", 1));
                }
                return true;
            }

            if(E.ID == "rr_CommandBreath")
            {
                if (nCharges == 0) return false;

                return Breathe();
            }

            if(E.ID == "BeginEquip")
            {
                GameObject Equipment = E.GetParameter("Object") as GameObject;
                string BodyPartName = E.GetParameter("BodyPartName") as string;

                if(BodyPartName == "Face")
                {
                    if(IsPlayer())
                    {
                        Popup.Show(String.Format(Resources.equipError, Equipment.DisplayName));
                    }
                    return false;
                }
            }
            return base.FireEvent(E);
        }

        public override bool ChangeLevel(int NewLevel)
        {
            // Refill the charges on level up.  Isn't that nice of us?
            nCharges = 2 + Level;
            UpdateAbility();

            return base.ChangeLevel(NewLevel);
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            Unmutate(GO);

            // Disable conductivity of the Creature/Player.
            Physics pPhysics = GO.GetPart("Physics") as Physics;
            if (pPhysics != null)
            {
                OldConductivity = pPhysics.Conductivity;
                pPhysics.Conductivity = 0;
            }

            // Add the breath to the face of the Creature/Player.
            Body pBody = GO.GetPart("Body") as Body;
            if(pBody != null)
            {
                GO.FireEvent(Event.New("CommandForceUnequipObject", "BodyPartName", "Face"));
                mySource = GameObjectFactory.Factory.CreateObject(Resources.faceItem);
                Event eCommandEquipObject = Event.New("CommandEquipObject");
                eCommandEquipObject.AddParameter("Object", mySource);
                eCommandEquipObject.AddParameter("BodyPartName", "Face");
                GO.FireEvent(eCommandEquipObject);
            }

            // Add the ability.
            ActivatedAbilities AA = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;
            myActivatedAbilityID = AA.AddAbility("Breathe Lightning", "rr_CommandBreath", "Physical Mutation");
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

            // Remove the breath from the Face of the Creature/Player.
            Body pBody = GO.GetPart("Body") as Body;
            if(pBody != null)
            {
                BodyPart pFace = pBody.GetPartByName("Face");
                if(pFace != null && pFace.Equipped != null && pFace.Equipped.Blueprint == Resources.faceItem)
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
