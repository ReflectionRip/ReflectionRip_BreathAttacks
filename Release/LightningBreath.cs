// Decompiled with JetBrains decompiler
// Type: XRL.World.Parts.Mutation.LightningBreath
// This code tries to add a Lightning Breathing Dragon type attack.

using ConsoleLib.Console;
using System;
using XRL.Core;
using XRL.Messages;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class rr_LightningBreath : rr_BaseBreath
    {
        public int FullCooldown = 25;
        private int AdjustedCooldown = 25;
        public int nCharges = 3;
        public int nTurnCounter;
        public int OldConductivity;

        // My attempt of preparing for multi-language support.
        // Wanted to use a resource file, but couldn't get it to work correctly.
        // Also wanted to seperate the strings from the source code, to allow for better reuse.
        protected override string CommandName { get { return "Breathe Lightning"; } }
        protected override string FaceItem { get { return "Sparking Breath"; } }
        protected override string Description { get { return "You can breathe lightning."; } }
        protected override string EquipError { get { return "Your lightning breath prevents you from equipping {0}!"; } }
        protected override string LevelText { get {
                return "Emits a 9-square ray of lightning in the direction of your choice.\n" +
                       "Accrue an additional charge every {0} rounds up to the maximum of {1} charges.\n" +
                       "Damage per charge: {2}\n" +
                       "Electricity will arc to adjacent targets dealing reduced damage.\n" +
                       "You can't wear anything on your face."; } }
        protected override string DamageMessage { get { return "from %o lightning!"; } }

        // A small cealing function to round up.
        protected int Ceiling(int n, int d)
        {
            int div = n / d;
            if (n % d != 0) div++;
            return div;
        }

        public rr_LightningBreath()
        {
            Name = nameof(rr_LightningBreath);
            DisplayName = "Lightning Breath";
            VarCooldown = Ceiling(25 , 3);
            DamageDie = "d4";
            Colors = new char[] { 'C', 'W', 'Y' };
            Particles = new char[] { '\u00B3', '\u00C4', '\u00C5', '\u00B4', '\u00C1', '\u00C2', '\u00C3' };
        }

        public override void Register(GameObject GO)
        {
            GO.RegisterPartEvent(this, "EndTurn");
            base.Register(GO);
        }

        public override string GetLevelText(int Level)
        {
            return String.Format(LevelText, VarCooldown, (2 + Level), '1' + DamageDie);
        }

        // Update the ability string to show the number of charges.
        public void UpdateAbility()
        {
            if (myActivatedAbility == null) return;
            myActivatedAbility.DisplayName = CommandName + " [" + nCharges + " charges]";
        }

        public override void Spawn(Cell C, ScreenBuffer Buffer, int Distance = 0)
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
                    string str1 = "&" + Color + Particle;

                    XRLCore.ParticleManager.Add(str1, C.X, C.Y, 0.0f, 0.0f, 10 + 2 * Distance + (6 - 2 * Fade));
                }
            }
        }

        public override bool Breathe()
        {
            base.Breathe();

            // Clear the number of charges.
            nCharges = 0;
            AdjustedCooldown = (myActivatedAbility.Cooldown / 10) - 1;
            nTurnCounter = AdjustedCooldown;
            UpdateAbility();

            return true;
        }

        public override bool FireEvent(Event E)
        {
            // Check the turn counter at the end of turn, and update the number of charges if needed.
            if(E.ID == "EndTurn")
            {
                if (nCharges >= (2 + Level)) return true;

                if (nTurnCounter <= 0)
                {
                    nTurnCounter = AdjustedCooldown;
                    nCharges++;
                }
                else
                {
                    nTurnCounter--;
                }

                UpdateAbility();
                return true;
            }
            return base.FireEvent(E);
        }

        public override bool ChangeLevel(int NewLevel)
        {
            VarCooldown = Ceiling(FullCooldown, (2 + NewLevel));
            UpdateAbility();

            return base.ChangeLevel(NewLevel);
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            base.Mutate(GO, Level);

            // Disable conductivity of the Creature/Player.
            Physics pPhysics = GO.GetPart("Physics") as Physics;
            if (pPhysics != null)
            {
                OldConductivity = pPhysics.Conductivity;
                pPhysics.Conductivity = 0;
            }

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

            return base.Unmutate(GO);
        }
    }
}
