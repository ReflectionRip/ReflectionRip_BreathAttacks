// Decompiled with JetBrains decompiler
// Type: XRL.World.Parts.Mutation.FireBreath
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
    public class rr_AcidBreath : BaseMutation
    {
        public Guid myActivatedAbilityID = Guid.Empty;
        public ActivatedAbilityEntry myActivatedAbility = null;
        public GameObject mySource;

        public int VarCooldown = 25;
        public string DamageDie = "d4+1";

        public string Gas = "AcidGas";
        public int BaseDensity = 30;
        public int LevelDensity = 10;
        private string DamageTypeImmunity;

        // My attempt of preparing for multi-language support.
        // Wanted to use a resource file, but couldn't get it to work correctly.
        // Also wanted to seperate the strings from the source code, to allow for better reuse.
        private class Resources
        {
            private static char[] Colors = { 'g', 'G' }; // Dark Green, Light Green
            // The 5 square symbols; Full square, and 4 1/2 squares.
            private static char[] Particles = { '\u00DB', '\u00DC', '\u00DD', '\u00DE', '\u00DF' };
            internal static string displayName { get {
                    return "Acid Breath"; } }
            internal static string commandName { get {
                    return "Breathe Acid"; } }
            internal static string faceItem { get {
                    return "Corrosive Breath"; } }
            internal static string description { get {
                    return "You can breathe acid and acid doesn't hurt you."; } }
            internal static string targetSelf { get {
                    return "Are you sure you want to target yourself?"; } }
            internal static string equipError { get {
                    return "Your acid breath prevents you from equipping {0}!"; } }
            internal static string levelText { get {
                    return "Emits a 9-square ray of acid gas in the direction of your choice\n" +
                           "Cooldown: {0} rounds\n" +
                           "Damage: {1}\n" + 
                           "Density: {2}\n" +
                           "You can't wear anything on your face."; } }
            internal static string damageMessage { get {
                    return "from a jet of corrosive gas!"; } }
            internal static char Color { get {
                    Stat.Random(1, 3);
                    return Colors[Stat.Random(0, Colors.Length - 1)]; } }
            internal static char Particle { get {
                    Stat.Random(1, 3);
                    return Particles[Stat.Random(0, Particles.Length - 1)]; } }
        }

        public rr_AcidBreath()
        {
            Name = nameof(rr_AcidBreath);
            DisplayName = Resources.displayName;
        }

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, "BeginEquip");
            Object.RegisterPartEvent(this, "rr_CommandBreath");
            Object.RegisterPartEvent(this, "BeforeApplyDamage");
            Object.RegisterPartEvent(this, "AIGetOffensiveMutationList");
        }

        public override string GetDescription()
        {
            return Resources.description;
        }

        public override string GetLevelText(int Level)
        {
            return String.Format(Resources.levelText, VarCooldown, Level + DamageDie, BaseDensity + (Level * LevelDensity));
        }

        // Create a gas cloud on a cell.
        public void Spawn(Cell C, ScreenBuffer Buffer, int Distance = 0)
        {
            if (C == null) return;

            // Apply acid damage to all 'Combat' objects in the cell.
            // Should I change this to all objects?
            foreach (GameObject GO in C.GetObjectsWithPart("Combat"))
            {
                if (GO.PhasedMatches(ParentObject))
                {
                    Damage damage = new Damage(Stat.Roll(Level + DamageDie));
                    damage.AddAttribute("Acid");

                    Event eTakeDamage = Event.New("TakeDamage");
                    eTakeDamage.AddParameter("Damage", damage);
                    eTakeDamage.AddParameter("Owner", ParentObject);
                    eTakeDamage.AddParameter("Attacker", ParentObject);
                    eTakeDamage.AddParameter("Message", Resources.damageMessage);
                    GO.FireEvent(eTakeDamage);
                }
            }

            // Create a gas cloud
            GameObject gasObject = GameObjectFactory.Factory.CreateObject(Gas);
            Gas spawnedGas = gasObject.GetPart("Gas") as Gas;
            spawnedGas.Creator = ParentObject;
            spawnedGas.Density = BaseDensity + (Level * LevelDensity);
            C.AddObject(gasObject);

            // Add particle effects, but only in visible active zones.
            if (C.ParentZone.IsActive() && C.IsVisible())
            {
                for (int Fade = 0; Fade < 3; ++Fade)
                {
                    // Pick a Forground color, background color, and particle.
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
            if(E.ID == "AIGetOffensiveMutationList")
            {
                // Stop of the AI doesn't have the ability, or it is recharging.
                if (myActivatedAbility == null) return true;
                if (myActivatedAbility.Cooldown > 0) return true;

                int Distance = (int)E.GetParameter("Distance");
                GameObject Target = E.GetParameter("Target") as GameObject;
                List<AICommandList> CommandList = (List<AICommandList>)E.GetParameter("List");

                // Get the Distance and Line of Sight to the Target and use the ability if able.
                if (Distance <= 9 && ParentObject.HasLOSTo(Target))
                {
                    CommandList.Add(new AICommandList("rr_CommandBreath", 1));
                }
                return true;
            }

            if (E.ID == "BeforeApplyDamage")
            {
                return DamageTypeImmunity == null || !E.GetParameter<Damage>("Damage").HasAttribute(DamageTypeImmunity);
            }

            if (E.ID == "rr_CommandBreath")
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
            return base.ChangeLevel(NewLevel);
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            Unmutate(GO);

            // Add acid gas immunity.
            GameObjectBlueprint gameObjectBlueprint = GameObjectFactory.Factory.GetBlueprint(Gas);
            DamageTypeImmunity = gameObjectBlueprint.GetTag("GasGenerationDamageTypeImmunity", null);

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
            myActivatedAbilityID = AA.AddAbility(Resources.commandName, "rr_CommandBreath", "Physical Mutation");
            myActivatedAbility = AA.AbilityByGuid[myActivatedAbilityID];

            return true;
        }

        public override bool Unmutate(GameObject GO)
        {
            // Remove acid immunity from the Creature/Player.
            DamageTypeImmunity = null;

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
