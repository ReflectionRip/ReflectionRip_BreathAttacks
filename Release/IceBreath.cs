// Decompiled with JetBrains decompiler
// Type: XRL.World.Parts.Mutation.IceBreath
// This code tries to add a Ice Breathing Dragon type attack.  It uses freezing hands as its original code source.

using ConsoleLib.Console;
using System;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class rr_IceBreath : rr_BaseBreath
    {
        public int OldFreeze = -1;
        public int OldBrittle = -1;

        // Bright Cyan, Bright Blue, White
        protected override string CommandName { get { return "Breathe Ice"; } }
        protected override string FaceItem { get { return "Icy Breath"; } }
        protected override string Description { get { return "You can breathe ice."; } }
        protected override string EquipError { get { return "Your ice breath prevents you from equipping {0}!"; } }
        protected override string LevelText { get {
                return "Emits a 9-square ray of ice in the direction of your choice\n" +
                       "Cooldown: {0} rounds\n" +
                       "Damage: {1}\n" +
                       "You can't wear anything on your face."; } }
        protected override string DamageMessage { get { return "from %o freezing effect!"; } }

        public rr_IceBreath()
        {
            Name = nameof(rr_IceBreath);
            DisplayName = "Ice Breath";
            VarCooldown = 25;
            DamageDie = "d3+1";
            Colors = new char[] { 'C', 'B', 'Y' };
            Particles = new char[] { '\u00DB', '\u00DC', '\u00DD', '\u00DE', '\u00DF' };
            Attributes = new string[] { "Cold" };
        }

        public override void Spawn(Cell C, ScreenBuffer Buffer, int Distance = 0)
        {
            if (C == null) return;

            string Damage = Level + DamageDie;
            int TempChange = -120 - (7 * Level);

            // Change the temperature of all objects in the cell.
            foreach (GameObject GO in C.GetObjectsInCell())
            {
                if(GO.PhasedMatches(ParentObject))
                {
                    GO.FireEvent(Event.New("TemperatureChange", "Amount", TempChange, "Owner", ParentObject));

                    /* Removed; Doesn't seem to have a noticable effect. Could be old legacy code?
                    for (int x = 0; x < 5; x++) GO.ParticleText("&C" + (char)(219 + Rules.Stat.Random(0, 4)), 2.9f, 1);
                    for (int x = 0; x < 5; x++) GO.ParticleText("&c" + (char)(219 + Rules.Stat.Random(0, 4)), 2.9f, 1);
                    for (int x = 0; x < 5; x++) GO.ParticleText("&Y" + (char)(219 + Rules.Stat.Random(0, 4)), 2.9f, 1);
                    */
                }
            }

            base.Spawn(C, Buffer, Distance);
        }

        public override bool ChangeLevel(int NewLevel)
        {
            // Adjust when the player can freeze (I think)
            Physics pPhysics = ParentObject.GetPart("Physics") as Physics;
            pPhysics.BrittleTemperature = -300 * Level - 600;

            return base.ChangeLevel(NewLevel);
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            base.Mutate(GO, Level);

            // Enable temperature changing.
            Physics pPhysics = GO.GetPart("Physics") as Physics;
            if(pPhysics != null)
            {
                OldFreeze = pPhysics.FreezeTemperature;
                OldBrittle = pPhysics.BrittleTemperature;
            }

            return true;
        }

        public override bool Unmutate(GameObject GO)
        {
            // Restore temperature changes to the Creature/Player.
            Physics pPhysics = GO.GetPart("Physics") as Physics;
            if(pPhysics != null)
            {
                if(OldFreeze != -1) pPhysics.FreezeTemperature = OldFreeze;
                if(OldBrittle != -1) pPhysics.BrittleTemperature = OldBrittle;
                OldFreeze = -1;
                OldBrittle = -1;

                pPhysics.Temperature = 25;
            }

            return base.Unmutate(GO);
        }
    }
}
