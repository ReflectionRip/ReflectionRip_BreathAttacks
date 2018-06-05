using ConsoleLib.Console;
using System;
using System.Collections.Generic;
using XRL.Core;
using XRL.Messages;
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

        public int VarCooldown = 10;
        public string DamageDie = "d2-1";
        public string[] Attributes = { };

        [NonSerialized]
        protected char[] Colors = { 'W', 'g' };

        [NonSerialized]
        protected char[] Particles = { '\u00DC', '\u00DD', '\u00DE', '\u00DF' };

        protected virtual string CommandName { get { return "Breathe"; } }
        protected virtual string FaceItem { get { return "Bad Breath"; } }
        protected virtual string Description { get { return "Your breath stinks!"; } }
        protected virtual string TargetSelf { get { return "Are you sure you want to target yourself?"; } }
        protected virtual string EquipError { get { return "Your bad breath prevents you from equipping {0}!"; } }
        protected virtual string LevelText { get {
                return "Emits a 9-square ray of bad breath in the direction of your choice\n" +
                       "Cooldown: {0} rounds\n" +
                       "Damage: {1}\n" +
                       "You can't wear anything on your face."; } }
        protected virtual string DamageMessage { get { return "from %o breath effect!"; } }
        protected virtual char Color { get {
                Stat.Random(1, 3);
                return Colors[Stat.Random(0, Colors.Length - 1)]; } }
        protected virtual char Particle { get {
                Stat.Random(1, 3);
                return Particles[Stat.Random(0, Particles.Length - 1)]; } }

        public rr_BaseBreath()
        {
            Name = nameof(rr_BaseBreath);
            DisplayName = "Bad Breath";
        }

        public override void Register(GameObject GO)
        {
            GO.RegisterPartEvent(this, "BeginEquip");
            GO.RegisterPartEvent(this, "rr_CommandBreath");
            GO.RegisterPartEvent(this, "AIGetOffensiveMutationList");
        }

        public override string GetDescription()
        {
            return Description;
        }

        public override string GetLevelText(int Level)
        {
            return String.Format(LevelText, VarCooldown, Level + DamageDie);
        }

        public virtual void Spawn(Cell C, ScreenBuffer Buffer, int Distance = 0)
        {
            if (C == null) return;

            // Apply ice damage to all 'Combat' objects in the cell.
            foreach (GameObject GO in C.GetObjectsWithPart("Combat"))
            {
                if (GO.PhasedMatches(ParentObject))
                {
                    Damage damage = new Damage(Stat.Roll(Level + DamageDie));
                    for (int i = 0; i < Attributes.Length - 1; i++) damage.AddAttribute(Attributes[i]);

                    Event eTakeDamage = Event.New("TakeDamage");
                    eTakeDamage.AddParameter("Damage", damage);
                    eTakeDamage.AddParameter("Owner", ParentObject);
                    eTakeDamage.AddParameter("Attacker", ParentObject);
                    eTakeDamage.AddParameter("Message", DamageMessage);
                    GO.FireEvent(eTakeDamage);
                    MessageQueue.AddPlayerMessage("Parent Obhect is " + ParentObject.DisplayName);
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

        public virtual bool Breathe()
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
                if (myActivatedAbility.Enabled == false) return true;

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
                        Popup.Show(String.Format(EquipError, Equipment.DisplayName));
                    }
                    return false;
                }
            }
            return base.FireEvent(E);
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            Unmutate(GO);

            // Add the breath to the face of the Creature/Player.
            Body pBody = GO.GetPart("Body") as Body;
            if (pBody != null)
            {
                GO.FireEvent(Event.New("CommandForceUnequipObject", "BodyPartName", "Face"));
                mySource = GameObjectFactory.Factory.CreateObject(FaceItem);
                Event eCommandEquipObject = Event.New("CommandEquipObject");
                eCommandEquipObject.AddParameter("Object", mySource);
                eCommandEquipObject.AddParameter("BodyPartName", "Face");
                GO.FireEvent(eCommandEquipObject);
            }

            // Add the ability.
            ActivatedAbilities AA = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;
            myActivatedAbilityID = AA.AddAbility(CommandName, "rr_CommandBreath", "Physical Mutation");
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
                if (pMainBody != null && pMainBody.Equipped != null && pMainBody.Equipped.Blueprint == FaceItem)
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
