// Decompiled with JetBrains decompiler
// Type: XRL.World.Parts.Mutation.FireBreath
// Assembly: Assembly-CSharp, Version=2.0.6684.37340, Culture=neutral, PublicKeyToken=null
// MVID: 5B4FB8C1-2DD4-47AE-B531-7F4329DC0775
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed\Assembly-CSharp.dll
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
        public string Gas = "AcidGas";
        public string DamageDie = "d4+1";
        public int BaseDensity = 30;
        public int LevelDensity = 10;
        public int VarCooldown = 25;
        public GameObject mySource;
        private string DamageTypeImmunity;

        [NonSerialized]
        private string[] PrimaryColors = { "G", "g" };
        [NonSerialized]
        private string[] Particles = { "ø", "ù", "ú" };

        // My attempt of preparing for multi-language support.
        // Wanted to use a resource file, but couldn't get it to work correctly.
        // Also wanted to seperate the strings from the source code, to allow for better reuse.
        private class Resources
        {
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
                    return "from a jet of corrosive gas!"; }
            }
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
        public void Spawn(Cell C)
        {
            if (C == null) return;
            foreach (GameObject GO in C.GetObjectsWithPart("Combat"))
            {
                Damage damage = new Damage(Stat.Roll(Level + DamageDie));
                damage.AddAttribute("Acid");
                Event E = Event.New("TakeDamage");
                E.AddParameter("Damage", damage);
                E.AddParameter("Owner", ParentObject);
                E.AddParameter("Attacker", ParentObject);
                E.AddParameter("Message", Resources.damageMessage);
                GO.FireEvent(E);
            }
            GameObject gasObject = GameObjectFactory.Factory.CreateObject(Gas);
            Gas spawnedGas = gasObject.GetPart("Gas") as Gas;
            spawnedGas.Creator = ParentObject;
            spawnedGas.Density = BaseDensity + (Level * LevelDensity);
            C.AddObject(gasObject);
        }

        public bool Breathe()
        {
            ScreenBuffer Buffer = new ScreenBuffer(80, 25);
            XRLCore.Core.RenderMapToBuffer(Buffer);
            List<Cell> TargetCells = PickLine(9, AllowVis.Any, null);
            if (TargetCells == null || TargetCells.Count <= 1) return false;

            if (myActivatedAbility != null) myActivatedAbility.Cooldown = (VarCooldown + 1) * 10;

            ParentObject.FireEvent(Event.New("UseEnergy", "Amount", 1000, "Type", "Physical Mutation"));

            foreach (Cell TargetCell in TargetCells)
            {
                if (TargetCell == ParentObject.pPhysics.CurrentCell) continue;

                Spawn(TargetCell);

                if (TargetCell.ParentZone.IsActive() && TargetCell.IsVisible())
                {
                    for (int index2 = 0; index2 < 3; ++index2)
                    {
                        // Pick a Forground color, background color, and particle.
                        int num1 = Stat.Random(0, PrimaryColors.Length - 1);
                        int num2 = Stat.Random(0, PrimaryColors.Length - 1);
                        int num3 = Stat.Random(0, Particles.Length - 1);
                        string str1 = "&" + PrimaryColors[num1] + "^" + PrimaryColors[num2] + Particles[num3];

                        ParticleManager particleManager = XRLCore.ParticleManager;
                        particleManager.Add(str1, TargetCell.X, TargetCell.Y, 0.0f, 0.0f, 16 + 2 * index2);
                    }
                }
                foreach (GameObject gameObject in TargetCell.GetObjectsWithPart("Physics"))
                {
                    if (gameObject.pPhysics.Solid && gameObject.GetIntProperty("AllowMissiles", 0) == 0)
                    {
                        return true;
                    }
                }
            }

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

            Physics pPhysics = GO.GetPart("Physics") as Physics;

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

            ActivatedAbilities pAA = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;

            myActivatedAbilityID = pAA.AddAbility(Resources.commandName, "rr_CommandBreath", "Physical Mutation");
            myActivatedAbility = pAA.AbilityByGuid[myActivatedAbilityID];

            GameObjectBlueprint gameObjectBlueprint = GameObjectFactory.Factory.GetBlueprint(Gas);
            DamageTypeImmunity = gameObjectBlueprint.GetTag("GasGenerationDamageTypeImmunity", null);

            return true;
        }

        public override bool Unmutate(GameObject GO)
        {
            Physics pPhysics = GO.GetPart("Physics") as Physics;

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

            if(myActivatedAbilityID != Guid.Empty)
            {
                ActivatedAbilities pAA = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;
                pAA.RemoveAbility(myActivatedAbilityID);
                myActivatedAbilityID = Guid.Empty;
            }

            DamageTypeImmunity = null;

            return true;
        }
    }
}
