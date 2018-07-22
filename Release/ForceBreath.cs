// This code tries to add a Force Breath type attack.  It blows opponents back away from the player.

using ConsoleLib.Console;
using System;
using System.Collections.Generic;
using XRL.Core;
using XRL.Rules;
using XRL.Messages;
using XRL.World.Parts.Effects;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class rr_ForceBreath : rr_BaseBreath
    {
        public int PushMultiplier = 3;

        protected override string CommandName { get { return "Breathe"; } }
        protected override string FaceItem { get { return "Powerful Breath"; } }
        protected override string Description { get { return "You can breathe a strong gust of wind."; } }
        protected override string EquipError { get { return "Your breath prevents you from equipping {0}!"; } }
        protected override string LevelText { get {
                return "Emits a 3-square ray of wind in the direction of your choice knocking opponents back and stunning them\n" +
                       "Cooldown: {0} rounds\n" +
                       "Damage: {1}; Collision with other objects will increase damage\n" +
                       "Push Distance: {2}\n" +
                       "You can't wear anything on your face."; } }
        protected override string DamageMessage { get { return "from %o breath!"; } }

        public rr_ForceBreath()
        {
            Name = nameof(rr_ForceBreath);
            DisplayName = "Force Breath";
            VarCooldown = 25;
            DamageDie = "d2";
            Colors = new char[] { 'K', 'y', 'Y' };
            Particles = new char[] { '%', '*', '$', '@', '&' };
            Attributes = new string[] { };
        }

        public override string GetLevelText(int Level)
        {
            return String.Format(LevelText, VarCooldown, Level + DamageDie, Level * PushMultiplier);
        }

        
        public bool Slam(GameObject target, string sDirection, int MaxDistance, Dictionary<GameObject, string> effects)
        {
            if (MaxDistance < 0) return false;

            Cell currentCell = target.pPhysics.CurrentCell;
            if (currentCell == null || currentCell == XRLCore.Core.Game.Graveyard) return false;

            Cell cellFromDirection = currentCell.GetLocalCellFromDirection(sDirection, true);
            if (cellFromDirection == null) return false;

            // Check for walls in the next tile.
            if (cellFromDirection.HasObjectWithTag("Wall"))
            {
                if (!effects.ContainsKey(target)) effects.Add(target, string.Empty);
                //effects[target] = effects[target] + "w";

                foreach (GameObject gameObject in cellFromDirection.GetObjectsWithTag("Wall"))
                {
                    // Stop if the walls AV is greater than the remaining distance to travel. (may adjust later)
                    if (gameObject.GetStatValue("AV", 0) >= MaxDistance) return false;

                    // Add an effect and destroy the wall
                    gameObject.DustPuff();
                    for (int index2 = 0; index2 < 5; ++index2)
                        gameObject.ParticleText(gameObject.pRender.TileColor + (char)(219 + Stat.Random(0, 4)), 4.9f, 5);
                    if (gameObject.IsVisible())
                        MessageQueue.AddPlayerMessage(gameObject.The + gameObject.ShortDisplayName + " is destroyed!");
                    gameObject.Destroy();
                }
            }

            // Check for doors in the next tile.
            if (cellFromDirection.HasObjectWithTag("Door"))
            {
                if (!effects.ContainsKey(target)) effects.Add(target, string.Empty);
                //effects[target] = effects[target] + "w";

                foreach (GameObject gameObject in cellFromDirection.GetObjectsWithTag("Door"))
                {
                    // Stop if the walls AV is greater than the remaining distance to travel. (may adjust later)
                    if (gameObject.GetStatValue("AV", 0) >= MaxDistance) return false;

                    // Add an effect and destroy the door
                    gameObject.DustPuff();
                    for (int index2 = 0; index2 < 5; ++index2)
                        gameObject.ParticleText(gameObject.pRender.TileColor + (object)(char)(219 + Stat.Random(0, 4)), 4.9f, 5);
                    if (gameObject.IsVisible())
                        MessageQueue.AddPlayerMessage(gameObject.The + gameObject.ShortDisplayName + " is destroyed!");
                    gameObject.Destroy();
                }
            }

            // Empty tile, move the target; does empty include tiles with non-solid objects?
            if (cellFromDirection.IsEmpty() && target.pPhysics.Weight < 2000)
            {
                if (target.FireEvent(Event.New("CommandMove", "Direction", sDirection, "Forced", 1)))
                {
                    if (!effects.ContainsKey(target)) effects.Add(target, string.Empty);
                    effects[target] = effects[target] + "s";
                }
            }

            // Taget collides with another target?
            foreach (GameObject target1 in cellFromDirection.GetObjectsWithPart("Combat"))
            {
                if (target1.PhasedMatches(target))
                {
                    Slam(target1, sDirection, MaxDistance - 1, effects);
                }
            }

            return true;
        }
        

        public override void Spawn(Cell C, ScreenBuffer Buffer, int Distance = 0)
        {
            if (C == null) return;

            // Find the direction from the player that this cell is.
            Physics part = ParentObject.GetPart("Physics") as Physics;
            string sDirection = part.CurrentCell.GetDirectionFromCell(C);

            foreach (GameObject GO in C.GetObjectsWithPart("Combat"))
            {
                if (GO.PhasedMatches(ParentObject))
                {
                    Dictionary<GameObject, string> GODeffects = new Dictionary<GameObject, string>();
                    Slam(GO, sDirection, Level * PushMultiplier, GODeffects);

                    if (GODeffects.Count <= 0) continue;

                    foreach (KeyValuePair<GameObject, string> entry in GODeffects)
                    {
                        int dieSides = 2;
                        int StunDuration = 1;
                        for (int index = 0; index < entry.Value.Length; ++index)
                        {
                            if (entry.Value[index] == 'w') dieSides = dieSides + 2;
                            if (entry.Value[index] == 's') ++StunDuration;
                        }
                        DamageDie = "d" + dieSides;

                        entry.Key.ApplyEffect(new Stun(StunDuration, 9999));

                        Damage damage = new Damage(Stat.Roll(Level + DamageDie));
                        for (int i = 0; i < Attributes.Length - 1; i++) damage.AddAttribute(Attributes[i]);

                        Event eTakeDamage = Event.New("TakeDamage");
                        eTakeDamage.AddParameter("Damage", damage);
                        eTakeDamage.AddParameter("Owner", ParentObject);
                        eTakeDamage.AddParameter("Attacker", ParentObject);
                        eTakeDamage.AddParameter("Message", DamageMessage);
                        GO.FireEvent(eTakeDamage);

                    }
                    
                }
            }

            // Add particle effects, but only in visible active zones.
            if (C.ParentZone.IsActive() && C.IsVisible())
            {
                for (int Fade = 0; Fade < 3; ++Fade)
                {
                    string str1 = "&" + Color + "^" + Color + Particle;

                    XRLCore.ParticleManager.Add(str1, C.X, C.Y, 0.0f, 0.0f, 10 + 2 * Distance + (6 - 2 * Fade));
                }
            }
        }

        public override bool Breathe()
        {
            // Pick the target.
            ScreenBuffer Buffer = ScreenBuffer.GetScrapBuffer1(true);
            XRLCore.Core.RenderMapToBuffer(Buffer);
            List<Cell> TargetCells = PickLine(3, AllowVis.Any, null);
            if (TargetCells == null || TargetCells.Count <= 1) return false;

            // Shoot out the breath line.
            for (int Distance = 0; Distance <= 3 && Distance < TargetCells.Count; ++Distance)
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
