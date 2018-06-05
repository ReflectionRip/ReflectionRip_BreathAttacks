// Decompiled with JetBrains decompiler
// Type: XRL.World.Parts.Mutation.FireBreath
// This code tries to add a Fire Breathing Dragon type attack.  It uses flaming hands as its original code source.

using ConsoleLib.Console;
using System;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class rr_FireBreath : rr_BaseBreath
    {
        public int OldFlame = -1;
        public int OldVapor = -1;

        // My attempt of preparing for multi-language support.
        // Wanted to use a resource file, but couldn't get it to work correctly.
        // Also wanted to seperate the strings from the source code, to allow for better reuse.
        protected override string CommandName { get { return "Breathe Fire"; } }
        protected override string FaceItem { get { return "Smokey Breath"; } }
        protected override string Description { get { return "You can breathe fire."; } }
        protected override string EquipError { get { return "Your fire breath prevents you from equipping {0}!"; } }
        protected override string LevelText { get {
                return "Emits a 9-square ray of flame in the direction of your choice\n" +
                       "Cooldown: {0} rounds\n" +
                       "Damage: {1}\n" +
                       "You can't wear anything on your face."; } }
        protected override string DamageMessage { get { return "from %o flames!"; } }

        public rr_FireBreath()
        {
            Name = nameof(rr_FireBreath);
            DisplayName = "Fire Breath";
            VarCooldown = 25;
            DamageDie = "d4+1";
            Colors = new char[] { 'r', 'R', 'W' };
            Particles = new char[] { '\u00DB', '\u00DC', '\u00DD', '\u00DE', '\u00DF' };
            Attributes = new string[] { "Fire", "Heat" };
        }

        public override void Spawn(Cell C, ScreenBuffer Buffer, int Distance = 0)
        {
            if (C == null) return;

            int TempChange = 310 + (25 * Level);

            // Change the temperature of all objects in the cell.
            foreach (GameObject GO in C.GetObjectsInCell())
            {
                if(GO.PhasedMatches(ParentObject))
                {
                    GO.FireEvent(Event.New("TemperatureChange", "Amount", TempChange, "Owner", ParentObject));

                    /* Removed; Doesn't seem have a noticable effect. Could be old legacy code?
                    for (int x = 0; x < 5; x++) GO.ParticleText("&C" + (char)(219 + Rules.Stat.Random(0, 4)), 2.9f, 1);
                    for (int x = 0; x < 5; x++) GO.ParticleText("&c" + (char)(219 + Rules.Stat.Random(0, 4)), 2.9f, 1);
                    for (int x = 0; x < 5; x++) GO.ParticleText("&Y" + (char)(219 + Rules.Stat.Random(0, 4)), 2.9f, 1);
                   */
                }
            }

            base.Spawn(C, Buffer, Distance);
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            base.Mutate(GO, Level);

            // Enable temperature changing.
            Physics pPhysics = GO.GetPart("Physics") as Physics;
            if(pPhysics != null)
            {
                OldFlame = pPhysics.FlameTemperature;
                OldVapor = pPhysics.VaporTemperature;
            }

            return true;
        }

        public override bool Unmutate(GameObject GO)
        {
            // Restore temperature changes to the Creature/Player.
            Physics pPhysics = GO.GetPart("Physics") as Physics;
            if(pPhysics != null)
            {
                if(OldFlame != -1) pPhysics.FlameTemperature = OldFlame;
                if(OldVapor != -1) pPhysics.BrittleTemperature = OldVapor;
                OldFlame = -1;
                OldVapor = -1;

                pPhysics.Temperature = 25;
            }

            return base.Unmutate(GO);
        }
    }
}
