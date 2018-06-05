// Decompiled with JetBrains decompiler
// Type: XRL.World.Parts.Mutation.IceBreath
// This code tries to add a Ice Breathing Dragon type attack.  It uses freezing hands as its original code source.

using ConsoleLib.Console;
using System;
using System.Collections.Generic;
using XRL.Core;
using XRL.Rules;
using XRL.UI;
using XRL.World.AI.GoalHandlers;
using XRL.Messages;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class rr_IceBreath : BaseMutation
    {
        public Guid myActivatedAbilityID = Guid.Empty;
        public ActivatedAbilityEntry myActivatedAbility = null;
        public GameObject mySource;

        public int VarCooldown = 1;
        public string DamageDie = "d3+1";

        public int OldFreeze = -1;
        public int OldBrittle = -1;

        // My attempt of preparing for multi-language support.
        // Wanted to use a resource file, but couldn't get it to work correctly.
        // Also wanted to seperate the strings from the source code, to allow for better reuse.
        private class Resources
        {
            // Bright Cyan, Bright Blue, White
            private static char[] Colors = { 'C', 'B', 'Y' }; 
            // The 5 square symbols; Full square, and 4 1/2 squares.
            private static char[] Particles = { '\u00DB', '\u00DC', '\u00DD', '\u00DE', '\u00DF' };
            internal static string displayName { get { 
                    return "Ice Breath"; } }
            internal static string commandName { get {
                    return "Breathe Ice"; } }
            internal static string faceItem { get {
                    return "Icy Breath"; } }
            internal static string description { get {
                    return "You can breathe ice."; } }
            internal static string targetSelf { get {
                    return "Are you sure you want to target yourself?"; } }
            internal static string equipError { get {
                    return "Your ice breath prevents you from equipping {0}!"; } }
            internal static string levelText { get {
                    return "Emits a 9-square ray of ice in the direction of your choice\n" +
                           "Cooldown: {0} rounds\n" +
                           "Damage: {1}\n" +
                           "You can't wear anything on your face."; } }
            internal static string damageMessage { get {
                    return "from %o freezing effect!"; } }
            internal static char Color { get {
                    Stat.Random(1, 3);
                    return Colors[Stat.Random(0, Colors.Length - 1)]; } }
            internal static char Particle { get {
                    Stat.Random(1, 3);
                    return Particles[Stat.Random(0, Particles.Length - 1)]; } }
        }

        public rr_IceBreath()
        {
            Name = nameof(rr_IceBreath);
            DisplayName = Resources.displayName;
        }

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, "BeginEquip");
            Object.RegisterPartEvent(this, "rr_CommandBreath");
            Object.RegisterPartEvent(this, "AIGetOffensiveMutationList");
        }

        public override string GetDescription()
        {
            return Resources.description;
        }

        public override string GetLevelText(int Level)
        {
            return String.Format(Resources.levelText, VarCooldown, Level + DamageDie);
        }

        public void Spawn(Cell C, ScreenBuffer Buffer, int Distance = 0)
        {
            if (C == null) return;

            string Damage = Level + "d3+1";
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

            // Apply ice damage to all 'Combat' objects in the cell.
            foreach (GameObject GO in C.GetObjectsWithPart("Combat"))
            {
                if(GO.PhasedMatches(ParentObject))
                {
                    Damage damage = new Damage(Stat.Roll(Level + DamageDie));
                    damage.AddAttribute("Cold");

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
                if(TargetCells.Count == 1 || TargetCells[Distance] != ParentObject.pPhysics.CurrentCell)
                {
                    Spawn(TargetCells[Distance], Buffer, Distance);
                }
                foreach(GameObject gameObject in TargetCells[Distance].GetObjectsWithPart("Physics"))
                {
                    if(gameObject.pPhysics.Solid && gameObject.GetIntProperty("AllowMissiles", 0) == 0)
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
            if(E.ID == "AIGetOffensiveMutationList")
            {
                if (myActivatedAbility == null) return true;
                if (myActivatedAbility.Cooldown > 0) return true;

                int Distance = (int)E.GetParameter("Distance");
                GameObject Target = E.GetParameter("Target") as GameObject;
                List<AICommandList> CommandList = (List<AICommandList>)E.GetParameter("List");

                if(Distance <= 9 && ParentObject.HasLOSTo(Target))
                {
                    CommandList.Add(new AICommandList("rr_CommandBreath", 1));
                }
                return true;
            }

            if(E.ID == "rr_CommandBreath")
            {
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
            // Adjust when the player can freeze (I think)
            Physics pPhysics = ParentObject.GetPart("Physics") as Physics;
            pPhysics.BrittleTemperature = -300 * Level - 600;

            return base.ChangeLevel(NewLevel);
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            Unmutate(GO);

            // Enable temperature changing.
            Physics pPhysics = GO.GetPart("Physics") as Physics;
            if(pPhysics != null)
            {
                OldFreeze = pPhysics.FreezeTemperature;
                OldBrittle = pPhysics.BrittleTemperature;
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
            myActivatedAbilityID = AA.AddAbility("Breathe Ice", "rr_CommandBreath", "Physical Mutation");
            myActivatedAbility = AA.AbilityByGuid[myActivatedAbilityID];
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

            // Remove the breath from the Face of the Creature/Player.
            Body pBody = GO.GetPart("Body") as Body;
            if(pBody != null)
            {
                BodyPart pMainBody = pBody.GetPartByName("Face");
                if(pMainBody != null && pMainBody.Equipped != null && pMainBody.Equipped.Blueprint == Resources.faceItem)
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
