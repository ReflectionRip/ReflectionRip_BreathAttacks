// This code tries to add a Force Breath type attack.  It blows opponents back away from the player.

using ConsoleLib.Console;
using System;
using System.Collections.Generic;
using XRL.Core;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class rr_ForceBreath : rr_BaseBreath
    {
        protected override string CommandName { get { return "Breathe"; } }
        protected override string FaceItem { get { return "Powerful Breath"; } }
        protected override string Description { get { return "You can breathe a strong gust of wind."; } }
        protected override string EquipError { get { return "Your breath prevents you from equipping {0}!"; } }
        protected override string LevelText
        {
            get
            {
                return "Emits a 3-square ray of wind in the direction of your choice knocking opponents back\n" +
                       "Cooldown: {0} rounds\n" +
                       "Damage: {1}\n" +
                       "You can't wear anything on your face.";
            }
        }
        protected override string DamageMessage { get { return "from %o breath!"; } }

        public rr_ForceBreath()
        {
            Name = nameof(rr_ForceBreath);
            DisplayName = "Force Breath";
            VarCooldown = 25;
            DamageDie = "d1";
            Colors = new char[] { 'K', 'y', 'Y' };
            Particles = new char[] { '%', '*', '$', '@', '&' };
            Attributes = new string[] { };
        }

        public override void Spawn(Cell C, ScreenBuffer Buffer, int Distance = 0)
        {
            if (C == null) return;

            string Damage = Level + DamageDie;

            base.Spawn(C, Buffer, Distance);
        }

        public override bool Breathe()
        {
            // Pick the target.
            ScreenBuffer Buffer = ScreenBuffer.GetScrapBuffer1(true);
            XRLCore.Core.RenderMapToBuffer(Buffer);
            List<Cell> TargetCells = PickLine(3, AllowVis.Any, null);
            if (TargetCells == null || TargetCells.Count <= 1) return false;

            // Shoot out the breath line.
            for (int Distance = 0; Distance < 3 && Distance < TargetCells.Count; ++Distance)
            {
                if (TargetCells.Count == 1 || TargetCells[Distance] != ParentObject.pPhysics.CurrentCell)
                {
                    Spawn(TargetCells[Distance], Buffer, Distance);
                }
                foreach (GameObject gameObject in TargetCells[Distance].GetObjectsWithPart("Physics"))
                {
                    if (gameObject.pPhysics.Solid && gameObject.GetIntProperty("AllowMissiles", 0) == 0)
                    {
                        Distance = 999;
                        break;
                    }
                }
            }

            // Apply 1 turn of energy usage to the player.
            ParentObject.FireEvent(Event.New("UseEnergy", "Amount", 1000, "Type", "Physical Mutation"));

            // Restart cooldown.
            if (myActivatedAbility != null) myActivatedAbility.Cooldown = (VarCooldown + 1) * 10;

            return true;
        }
    }
}
