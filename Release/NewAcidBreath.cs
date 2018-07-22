// Decompiled with JetBrains decompiler
// Type: XRL.World.Parts.Mutation.FireBreath
// This code tries to add a Acid Breathing Dragon type attack.  It uses acid wall trap as its original code source.

using ConsoleLib.Console;
using System;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class rr_NewAcidBreath : rr_BaseBreath
    {
        public string Gas = "AcidGas";
        public int BaseDensity = 30;
        public int LevelDensity = 10;
        private string DamageTypeImmunity;

        // My attempt of preparing for multi-language support.
        // Wanted to use a resource file, but couldn't get it to work correctly.
        // Also wanted to seperate the strings from the source code, to allow for better reuse.
        protected override string CommandName { get { return "Breathe Acid"; } }
        protected override string FaceItem { get { return "Corroding Breath"; } }
        protected override string Description { get { return "You can breathe acid and acid doesn't hurt you."; } }
        protected override string EquipError { get { return "Your acid breath prevents you from equipping {0}!"; } }
        protected override string LevelText { get {
                return "Emits a 9-square ray of acid gas in the direction of your choice\n" +
                       "Cooldown: {0} rounds\n" +
                       "Damage: {1}\n" + 
                       "Density: {2}\n" +
                       "You can't wear anything on your face."; } }
        protected override string DamageMessage { get { return "from a jet of corrosive gas!"; } }

        public rr_NewAcidBreath()
        {
            Name = nameof(rr_NewAcidBreath);
            DisplayName = "Acid Breath";
            VarCooldown = 25;
            DamageDie = "d4+1";
            Colors = new char[] { 'g', 'G' };
            Particles = new char[] { '\u00DB', '\u00DC', '\u00DD', '\u00DE', '\u00DF' };
            Attributes = new string[] { "Acid" };
        }

        public override void Register(GameObject GO)
        {
            GO.RegisterPartEvent(this, "BeforeApplyDamage");
            base.Register(GO);
        }

        public override string GetLevelText(int Level)
        {
            return String.Format(LevelText, VarCooldown, Level + DamageDie, BaseDensity + (Level * LevelDensity));
        }

        // Create a gas cloud on a cell.
        public override void Spawn(Cell C, ScreenBuffer Buffer, int Distance = 0)
        {
            if (C == null) return;

            // Create a gas cloud
            GameObject gasObject = GameObjectFactory.Factory.CreateObject(Gas);
            Gas spawnedGas = gasObject.GetPart("Gas") as Gas;
            spawnedGas.Creator = ParentObject;
            spawnedGas.Density = BaseDensity + (Level * LevelDensity);
            C.AddObject(gasObject);

            base.Spawn(C, Buffer, Distance);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "BeforeApplyDamage")
            {
                return DamageTypeImmunity == null || !E.GetParameter<Damage>("Damage").HasAttribute(DamageTypeImmunity);
            }

            return base.FireEvent(E);
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            base.Mutate(GO, Level);

            // Add acid gas immunity.
            GameObjectBlueprint gameObjectBlueprint = GameObjectFactory.Factory.GetBlueprint(Gas);
            DamageTypeImmunity = gameObjectBlueprint.GetTag("GasGenerationDamageTypeImmunity", null);

            return true;
        }

        public override bool Unmutate(GameObject GO)
        {
            // Remove acid immunity from the Creature/Player.
            DamageTypeImmunity = null;

            return base.Unmutate(GO);
        }
    }
}
