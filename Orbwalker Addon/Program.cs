/*
██████╗ ██╗   ██╗     █████╗ ██╗     ██████╗ ██╗  ██╗ █████╗  ██████╗  ██████╗ ██████╗ 
██╔══██╗╚██╗ ██╔╝    ██╔══██╗██║     ██╔══██╗██║  ██║██╔══██╗██╔════╝ ██╔═══██╗██╔══██╗
██████╔╝ ╚████╔╝     ███████║██║     ██████╔╝███████║███████║██║  ███╗██║   ██║██║  ██║
██╔══██╗  ╚██╔╝      ██╔══██║██║     ██╔═══╝ ██╔══██║██╔══██║██║   ██║██║   ██║██║  ██║
██████╔╝   ██║       ██║  ██║███████╗██║     ██║  ██║██║  ██║╚██████╔╝╚██████╔╝██████╔╝
╚═════╝    ╚═╝       ╚═╝  ╚═╝╚══════╝╚═╝     ╚═╝  ╚═╝╚═╝  ╚═╝ ╚═════╝  ╚═════╝ ╚═════╝ 
*/

#region

using System;
using System.Collections.Generic;
using LeagueSharp.Common;
using Orbwalker_Addon.Classes;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

#endregion

namespace Orbwalker_Addon
{
    public class Program
    {
        public static Menu Menu,orbwalker,config,movement,Headers;

        public static int LastAttackOrder;
        public static int LastAttack;
        public static int LastMoveOrder;
        public static int UpdateTick;
        public static bool JustAttacked;
        public static bool IssueOrder;
        public static bool AttackOrder;
        public static bool ShouldMove;
        public static bool ShouldBlock;
        public static bool AttackOnRange;
        public static bool GetWhenOnRange;
        public static bool BufferMovement;
        public static bool BufferAttack;
        public static int LastServerResponseDelay;
        public static GameObject LastTarget;
        public static AIHeroClient Player = ObjectManager.Player;
        public static int NetworkId = Player.NetworkId;
        public static List<int> OnAttackList;
        public static List<int> MissileHitList;
        public static List<float[]> CustomMissileHit = new List<float[]>();
        public static List<float[]> CurrentMissileHit;
        public static string GameVersion = Game.Version.Substring(0, 4);
        public static float Sum = 0;
        public static float MediumTime = 0;
        public static bool MissileLaunched;
        public static Vector3 BufferPosition = new Vector3(0, 0, 0);
        public static GameObject BufferTarget;
        public static int BufferAttackTimer;


        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Game_OnGameLoad;
        }

        static void LoadMenu()
        {
            Menu = MainMenu.AddMenu("Orbwalker Addon", "Orbwalker Addon");
            Menu.AddLabel("Ported by Rexy");

            //Orbwalking
            orbwalker = Menu.AddSubMenu("Orbwalking", "Orbwalking");

            AddBool(orbwalker, "debug","Debug Console",false);
            AddBool(orbwalker, "missilecheck","Custom Missile Check");
            AddSlider(orbwalker, "ExtraWindup","Extra windup time",60,0,200);
            AddKeyBind(orbwalker, "LastHitKey", "Last hit",KeyBind.BindTypes.HoldActive, 'X');
            AddKeyBind(orbwalker, "LaneClearKey","LaneClear",KeyBind.BindTypes.HoldActive, 'V');
            AddKeyBind(orbwalker, "MixedKey","Mixed",KeyBind.BindTypes.HoldActive, 'C');
            AddKeyBind(orbwalker, "ComboKey","Combo",KeyBind.BindTypes.HoldActive, 32);
            
            //Block After Attack
            config = Menu.AddSubMenu("Config", "Config");

            AddBool(config, "BufferAttack","Buffer attack");
            AddBool(config, "BufferMovement","Buffer movement");
            AddBool(config, "BlockOrder","Block order on attack");
            AddText(config, "This won't block Evade movements");

            //Follow Mouse (Orbwalk)
            movement = Menu.AddSubMenu("Enable Movement", "Enable Movement");

            AddBool(movement, "LastHitMovement","On Last hit");
            AddBool(movement, "LaneClearMovement","On Lane Clear");
            AddBool(movement, "MixedMovement","On Mixed");
            AddBool(movement, "ComboMovement","On Combo");
            
            //Packets
            Headers = Menu.AddSubMenu("Headers", "Headers");

            AddBool(Headers, "forcefindheaders", "Force Auto-Find Headers",false);
            AddSlider(Headers, "headerOnAttack" + GameVersion, "Header OnAttack",0,0,400);
            AddSlider(Headers, "headerOnMissileHit" + GameVersion, "Header OnMissileHit",0,0,400);
            AddText(Headers, "This assembly does not send packets to riot");
        }

        private static void AddBool(Menu m, string s, string display, bool state = true)
        {
            m.Add(s, new CheckBox(display, state));
        }
        private static void AddText(Menu m, string s)
        {
            m.AddLabel(s);
        }
        private static void AddSlider(Menu m, string s, string display, int cur = 0, int min = 0,int max = 100)
        {
            m.Add(s, new Slider(display,cur,min,max));
        }

        private static void AddKeyBind(Menu m, string s, string display, KeyBind.BindTypes type, uint key)
        {
            m.Add(s, new KeyBind(display, false, type, key));
        }

        private static bool GetBool(Menu m, string s) => m[s].Cast<CheckBox>().CurrentValue;
        private static int GetSlider(Menu m, string s) => m[s].Cast<Slider>().CurrentValue;
        private static bool GetKey(Menu m, string s) => m[s].Cast<KeyBind>().CurrentValue;

        private static void Game_OnGameLoad(EventArgs args)
        {
            OnAttackList = new List<int>();
            MissileHitList = new List<int>();
            GameVersion = Game.Version.Substring(0, 4);
            IssueOrder = true;

            LoadMenu();

            #region Set Headers

            Packets.Attack.Header = GetSlider(Headers, "headerOnAttack" + GameVersion);
            Packets.MissileHit.Header = GetSlider(Headers, "headerOnMissileHit" + GameVersion);

            #endregion

            EloBuddy.Player.OnIssueOrder += Obj_AI_Base_OnIssueOrder;
            Game.OnUpdate += OnUpdate;
            Game.OnProcessPacket += OnProcessPacket;
            MissileClient.OnCreate += MissileClient_OnCreate;
        }
        

        private static void OnUpdate(EventArgs args)
        {
            if (((GetBool(movement, "ComboMovement") && GetKey(orbwalker, "ComboKey")) ||
                (GetBool(movement, "MixedMovement") && GetKey(orbwalker, "MixedKey")) ||
                (GetBool(movement, "LaneClearMovement") && GetKey(orbwalker, "LaneClearKey")) ||
                (GetBool(movement, "LastHitMovement") && GetKey(orbwalker, "LastHitKey"))) && Orbwalker.DisableMovement)
            {
                Orbwalker.DisableMovement = false;
            }
            else
            {
                if (!Orbwalker.DisableMovement)
                {
                    Orbwalker.DisableMovement = true;
                }
            }

            if (GetWhenOnRange)
            {
                if (LastTarget == null || !LastTarget.IsValid || !(LastTarget is Obj_AI_Base) || LastTarget.IsDead)
                {
                    GetWhenOnRange = false;
                }
                else
                {
                    if (Orbwalking.InAutoAttackRange((AttackableUnit)LastTarget))
                    {
                        GetWhenOnRange = false;
                        AttackOnRange = true;
                        LastAttackOrder = Environment.TickCount;
                    }
                }
            }

            if (AttackOrder && Environment.TickCount - LastAttackOrder >= 100 + Game.Ping * 1.5)
            {
                AttackOrder = false;
            }

            if (BufferAttack && (Environment.TickCount - BufferAttackTimer >= 1000 || (BufferTarget == null || BufferTarget.IsDead)))
            {
                BufferAttack = false;
            }

            if (Player.Spellbook.IsCastingSpell && !Player.Spellbook.IsAutoAttacking && (!AttackOrder || JustAttacked))
            {
                AttackOrder = false;
                JustAttacked = false;
                AttackOnRange = false;
            }

            if (LastTarget == null || !LastTarget.IsValid || LastTarget.IsDead)
            {
                if (AttackOrder || JustAttacked)
                {
                    AttackOrder = false;
                    JustAttacked = false;
                    AttackOnRange = false;
                }
            }

            if (CanMove())
            {
                if (JustAttacked)
                {
                    JustAttacked = false;
                    IssueOrder = false;
                    AttackOnRange = false;
                    if (GetBool(orbwalker, "debug"))
                    {
                        Console.WriteLine("OnCastDone - Delay: " + (Environment.TickCount - LastAttack) + "ms");
                    }
                }
                else
                {
                    JustAttacked = false;
                    AttackOrder = false;

                }
                if (!Player.Spellbook.IsChanneling)
                {
                    if (BufferMovement)
                    {
                        EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, BufferPosition, true);
                    }
                    BufferMovement = false;
                }

                if (MissileLaunched)
                {
                    MissileLaunched = false;
                }
            }
            else
            {
                if (GetBool(config, "BlockOrder"))
                {
                    ShouldBlock = true;
                }
                else
                {
                    ShouldBlock = false;
                }
            }

            if (Orbwalker.CanAutoAttack)
            {
                if (Orbwalker.IsAutoAttacking && BufferAttack && BufferTarget != null && BufferTarget.IsValid && Orbwalking.InAutoAttackRange((AttackableUnit)BufferTarget))
                {
                    EloBuddy.Player.IssueOrder(GameObjectOrder.AttackUnit, BufferTarget, true);
                }
                BufferAttack = false;
            }
        }


        private static bool CanMove()
        {
            if (Player.CharData.BaseSkinName.Contains("Kalista"))
            {
                return true;
            }
            if (AttackOrder)
            {
                return false;
            }
            if (Player.IsRanged && GetBool(orbwalker, "missilecheck") && MissileLaunched)
            {
                return true;
            }
            if (LastTarget == null || !LastTarget.IsValid || LastTarget.IsDead)
            {
                return true;
            }
            if (!Orbwalker.DisableMovement)
            {
                return Orbwalker.CanMove;
            }
            else
            {
                if (BufferMovement)
                {
                    return Utils.GameTimeTickCount + Game.Ping / 2 >= Orbwalker.LastAutoAttack + Player.AttackCastDelay * 1000f + GetSlider(orbwalker, "ExtraWindup");
                }
                else
                {
                    return Utils.GameTimeTickCount + Game.Ping / 2 >= Orbwalker.LastAutoAttack + Player.AttackCastDelay * 1000f;
                }
            }
        }

        private static void OnProcessPacket(GamePacketEventArgs args)
        {
            short header = BitConverter.ToInt16(args.PacketData, 0);

            var length = BitConverter.ToString(args.PacketData, 0).Length;

            int networkID = BitConverter.ToInt32(args.PacketData, 2);

            #region AutoFind Headers

            if (GetBool(Headers, "forcefindheaders"))
            {
                Headers["headerOnAttack" + GameVersion].Cast<Slider>().CurrentValue = 0;
                Headers["headerOnMissileHit" + GameVersion].Cast<Slider>().CurrentValue = 0;
                Packets.Attack.Header = 0;
                Packets.MissileHit.Header = 0;
                Headers["forcefindheaders"].Cast<CheckBox>().CurrentValue = false;
            }

            if (GetSlider(Headers, "headerOnAttack" + GameVersion) == 0 && length == Packets.Attack.Length && networkID > 0)
            {
                foreach (Obj_AI_Minion obj in ObjectManager.Get<Obj_AI_Minion>().Where(obj => obj.NetworkId == networkID))
                {
                    OnAttackList.Add(header);
                    if (OnAttackList.Count<int>(x => x == header) == 10)
                    {
                        Headers["headerOnAttack" + GameVersion].Cast<Slider>().CurrentValue = header;
                        Packets.Attack.Header = header;
                        try
                        {
                            OnAttackList.Clear();
                        }
                        catch (Exception)
                        {
                            //ignored
                        }
                    }
                }
            }

            if (GetSlider(Headers, "headerOnMissileHit" + GameVersion) == 0 && length == Packets.MissileHit.Length && networkID > 0)
            {
                foreach (Obj_AI_Minion obj in ObjectManager.Get<Obj_AI_Minion>().Where(obj => obj.IsRanged && obj.NetworkId == networkID))
                {
                    MissileHitList.Add(header);
                    if (MissileHitList.Count<int>(x => x == header) == 10)
                    {
                        Headers["headerOnMissileHit" + GameVersion].Cast<Slider>().CurrentValue = header;
                        Packets.MissileHit.Header = header;
                        try
                        {
                            MissileHitList.Clear();
                        }
                        catch (Exception)
                        {
                            //ignored
                        }
                    }
                }
            }


            if (GetSlider(Headers, "headerOnAttack" + GameVersion) == 0 || GetSlider(Headers, "headerOnMissileHit" + GameVersion) == 0)
            {
                return;
            }

            #endregion

            if (networkID == NetworkId)
            {
                #region OnAttack

                if (length == Packets.Attack.Length && header == Packets.Attack.Header)
                {
                    if (GetBool(orbwalker, "debug"))
                    {
                        Console.WriteLine("---------------------------------------------------------");
                        Console.Write("OnAttack (server) - Expected Attack Delay: " + (Player.AttackCastDelay * 1000) + "ms");
                        Console.WriteLine(" - Delay: " + (Environment.TickCount - LastAttackOrder) + "ms");
                    }
                    LastServerResponseDelay = Environment.TickCount - LastAttackOrder;
                    LastAttack = Environment.TickCount;
                    JustAttacked = true;
                    Console.WriteLine("JustAttack Is True");
                    IssueOrder = false;
                    AttackOrder = false;
                }

                if (Player.CharData.BaseSkinName.Contains("Kalista"))
                {
                    return;
                }
                #endregion

                #region Custom Missile Check

                if (GetBool(orbwalker, "missilecheck") && Player.IsRanged &&
                    length == Packets.MissileHit.Length && header == Packets.MissileHit.Header &&
                    JustAttacked)
                {
                    float missileHitTime = (Environment.TickCount - LastAttack);

                    if (missileHitTime >= Player.AttackCastDelay * 1000 * 0.6)
                    {
                        JustAttacked = false;
                        IssueOrder = false;
                        if (GetBool(orbwalker, "debug"))
                        {
                            Console.WriteLine("OnMissileHit (server) - Delay: " + (Environment.TickCount - LastAttack) + "ms");
                        }

                        if (!Orbwalker.DisableMovement)
                        {
                            EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos, true);
                            LastMoveOrder = Environment.TickCount;
                        }
                        MissileLaunched = true;
                    }
                }
                #endregion
            }
        }

        private static void Obj_AI_Base_OnIssueOrder(Obj_AI_Base sender, PlayerIssueOrderEventArgs args)
        {
            if (sender == null || !sender.IsValid || !sender.IsMe)
            {
                return;
            }

            if (args.Order == GameObjectOrder.AttackTo || args.Order == GameObjectOrder.AttackUnit ||
                args.Order == GameObjectOrder.AutoAttack || args.IsAttackMove)
            {
                if (GetBool(config, "BufferAttack") && !Orbwalker.CanAutoAttack && CanMove() && !Orbwalker.DisableAttacking)
                {
                    if (args.Target != null && args.Target.IsValid && args.Target is GameObject)
                    {
                        BufferAttack = true;
                        BufferTarget = args.Target;
                        BufferAttackTimer = Environment.TickCount;
                    }

                    if (args.IsAttackMove)
                    {
                        if (GetBool(config, "BufferMovement") && args.Order == GameObjectOrder.MoveTo)
                        {
                            BufferMovement = true;
                            BufferPosition = args.TargetPosition;
                        }
                    }
                    args.Process = false;
                    return;
                }


                if ((AttackOrder || JustAttacked) && ShouldBlock)
                {
                    args.Process = false;
                    return;
                }

                AttackOnRange = false;

                if (!Orbwalker.DisableMovement)
                {
                    if (args.Target != null && args.Target.IsValid && args.Target is GameObject && Orbwalking.InAutoAttackRange((AttackableUnit)args.Target))
                    {
                        LastAttackOrder = Environment.TickCount;
                        AttackOrder = true;
                        LastTarget = args.Target;
                        AttackOnRange = true;
                        return;
                    }
                    else
                    {
                        args.Process = false;
                        return;
                    }
                }
                else
                {
                    if (args.Target != null && args.Target.IsValid && args.Target is GameObject)
                    {
                        if (Orbwalking.InAutoAttackRange((AttackableUnit)args.Target))
                        {
                            LastAttackOrder = Environment.TickCount;
                            AttackOrder = true;
                            LastTarget = args.Target;
                            AttackOnRange = true;
                            return;
                        }
                        else
                        {
                            LastTarget = args.Target;
                            GetWhenOnRange = true;
                        }
                    }
                }
            }



            if (args.Order == GameObjectOrder.MoveTo && ((AttackOrder && Environment.TickCount - LastAttackOrder > 0) || JustAttacked) &&
                !Player.CharData.BaseSkinName.Contains("Kalista") && ShouldBlock)
            {
                args.Process = false;
                if (GetBool(config, "BufferMovement"))
                {
                    BufferMovement = true;
                    BufferPosition = args.TargetPosition;
                }
                return;
            }

            if (!IssueOrder && args.Order == GameObjectOrder.MoveTo && GetBool(orbwalker, "debug") && args.Process)
            {
                Console.WriteLine("Movement - Delay: " + (Environment.TickCount - LastAttack) + "ms");
            }
            IssueOrder = true;
        }

        private static void MissileClient_OnCreate(GameObject sender, EventArgs args)
        {
            if (GetBool(orbwalker, "debug"))
            {
                var missile = sender as MissileClient;
                if (missile != null && missile.SpellCaster.IsMe && Orbwalking.IsAutoAttack(missile.SData.Name) && JustAttacked)
                {
                    Console.WriteLine("OnMissileHit (client) - Delay: " + (Environment.TickCount - LastAttack) + "ms");
                }
            }
        }
    }
}