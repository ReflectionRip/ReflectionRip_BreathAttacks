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
    public class rr_BaseBreath : BaseMutation
    {
        public Guid myActivatedAbilityID = Guid.Empty;
        public ActivatedAbilityEntry myActivatedAbility = null;
        public GameObject mySource;

        public int VarCooldown = 1;
        public string DamageDie = "d2";

        // My attempt of preparing for multi-language support.
        // Wanted to use a resource file, but couldn't get it to work correctly.
        // Also wanted to seperate the strings from the source code, to allow for better reuse.
        private class Resources
        {
            // Yellow, Dark Green; Yuck!
            private static char[] Colors = { 'W', 'g' };
            // 1/2 Squares.
            private static char[] Particles = { '\u00DC', '\u00DD', '\u00DE', '\u00DF' };
            internal static string DisplayName { get { return "Bad Breath"; } }
            internal static string CommandName { get { return "Breathe"; } }
            internal static string FaceItem { get { return "Bad Breath"; } }
            internal static string Description { get { return "Your breath stinks!"; } }
            internal static string TargetSelf { get { return "Are you sure you want to target yourself?"; } }
            internal static string EquipError { get { return "Your bad breath prevents you from equipping {1}!"; } }
            internal static string LevelText { get {
                    return "Emits a 9-square ray of bad breath in the direction of your choice\n" +
                           "Cooldown: {1} rounds\n" +
                           "Damage: {2}\n" +
                           "You can't wear anything on your face."; } }
            internal static string damageMessage { get { return "from %o breath effect!"; } }
            internal static char Color { get {
                    Stat.Random(1, 3);
                    return Colors[Stat.Random(0, Colors.Length - 1)]; } }
            internal static char Particle { get {
                    Stat.Random(1, 3);
                    return Particles[Stat.Random(0, Particles.Length - 1)]; } }
        }

        public rr_BaseBreath()
        {
            Name = nameof(rr_BaseBreath);
            DisplayName = Resources.DisplayName;
        }

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, "BeginEquip");
            Object.RegisterPartEvent(this, "rr_CommandBreath");
            Object.RegisterPartEvent(this, "AIGetOffensiveMutationList");
        }

        public override string GetDescription()
        {
            return Resources.Description;
        }

        public override string GetLevelText(int Level)
        {
            return String.Format(Resources.LevelText, VarCooldown, Level + DamageDie);
        }

        public void Spawn(Cell C, ScreenBuffer Buffer, int Distance = 0)
        {
            if (C == null) return;

            // Apply ice damage to all 'Combat' objects in the cell.
            foreach (GameObject GO in C.GetObjectsWithPart("Combat"))
            {
                if (GO.PhasedMatches(ParentObject))
                {
                    Damage damage = new Damage(Stat.Roll(Level + DamageDie));

                    Event eTakeDamage = Event.New("TakeDamage");
                    eTakeDamage.AddParameter("Damage", damage);
                    eTakeDamage.AddParameter("Owner", ParentObject);
                    eTakeDamage.AddParameter("Attacker", ParentObject);
                    eTakeDamage.AddParameter("Message", Resources.damageMessage);
                    GO.FireEvent(eTakeDamage);
                }
            }

            // Add particle effects, but only in visible active zones.
            if (C.ParentZone.IsActive() && C.IsVisible())
            {
                for (int Fade = 0; Fade < 3; ++Fade)
                {
                    string str1 = "&" + Resources.Color + "^" + Resources.Color + Resources.Particle;

                    XRLCore.ParticleManager.Add(str1, C.X, C.Y, 0.0f, 0.0f, 10 + 2 * Distance + (6 - 2 * Fade));
                }
            }
        }

        public bool Breathe()
        {
            // Pick the target.
            ScreenBuffer Buffer = new ScreenBuffer(80, 25);
            XRLCore.Core.RenderMapToBuffer(Buffer);
            List<Cell> TargetCells = PickLine(9, AllowVis.Any, null);
            if (TargetCells == null || TargetCells.Count <= 1) return false;

            // Shoot out the breath line.
            for (int Distance = 0; Distance < 9 && Distance < TargetCells.Count; ++Distance)
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

        public override bool FireEvent(Event E)
        {
            // When should the AI use this ability?
            if (E.ID == "AIGetOffensiveMutationList")
            {
                if (myActivatedAbility == null) return true;
                if (myActivatedAbility.Cooldown > 0) return true;

                int Distance = (int)E.GetParameter("Distance");
                GameObject Target = E.GetParameter("Target") as GameObject;
                List<AICommandList> CommandList = (List<AICommandList>)E.GetParameter("List");

                if (Distance <= 9 && ParentObject.HasLOSTo(Target))
                {
                    CommandList.Add(new AICommandList("rr_CommandBreath", 1));
                }
                return true;
            }

            if (E.ID == "rr_CommandBreath")
            {
                return Breathe();
            }

            if (E.ID == "BeginEquip")
            {
                GameObject Equipment = E.GetParameter("Object") as GameObject;
                string BodyPartName = E.GetParameter("BodyPartName") as string;

                if (BodyPartName == "Face")
                {
                    if (IsPlayer())
                    {
                        Popup.Show(String.Format(Resources.EquipError, Equipment.DisplayName));
                    }
                    return false;
                }
            }
            return base.FireEvent(E);
        }

        public override bool ChangeLevel(int NewLevel)
        {
            return base.ChangeLevel(NewLevel);
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            Unmutate(GO);

            // Add the breath to the face of the Creature/Player.
            Body pBody = GO.GetPart("Body") as Body;
            if (pBody != null)
            {
                GO.FireEvent(Event.New("CommandForceUnequipObject", "BodyPartName", "Face"));
                mySource = GameObjectFactory.Factory.CreateObject(Resources.FaceItem);
                Event eCommandEquipObject = Event.New("CommandEquipObject");
                eCommandEquipObject.AddParameter("Object", mySource);
                eCommandEquipObject.AddParameter("BodyPartName", "Face");
                GO.FireEvent(eCommandEquipObject);
            }

            // Add the ability.
            ActivatedAbilities AA = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;
            myActivatedAbilityID = AA.AddAbility(Resources.CommandName, "rr_CommandBreath", "Physical Mutation");
            myActivatedAbility = AA.AbilityByGuid[myActivatedAbilityID];
            return true;
        }

        public override bool Unmutate(GameObject GO)
        {
            // Remove the breath from the Face of the Creature/Player.
            Body pBody = GO.GetPart("Body") as Body;
            if (pBody != null)
            {
                BodyPart pMainBody = pBody.GetPartByName("Face");
                if (pMainBody != null && pMainBody.Equipped != null && pMainBody.Equipped.Blueprint == Resources.FaceItem)
                {
                    pMainBody.Equipped.FireEvent(Event.New("Unequipped", "UnequippingObject", ParentObject, "BodyPart", pMainBody));
                    pMainBody.Unequip();
                }
            }

            // Remove the ability.
            if (myActivatedAbilityID != Guid.Empty)
            {
                ActivatedAbilities AA = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;
                AA.RemoveAbility(myActivatedAbilityID);
                myActivatedAbilityID = Guid.Empty;
            }

            return true;
        }
    }
}
