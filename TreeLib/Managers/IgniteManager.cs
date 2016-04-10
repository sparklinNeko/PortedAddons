﻿using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK.Menu.Values;
using LeagueSharp.Common;
using TreeLib.Extensions;
using Menu = EloBuddy.SDK.Menu.Menu;

namespace TreeLib.Managers
{
    internal static class IgniteManager
    {
        public static Menu Menu;

        public static void Initialize()
        {
            Menu = SpellManager.Menu.AddSubMenu("Ignite");
            Menu.AddBool("IgniteEnabled", "Ignite Enabled");

            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!Menu["IgniteEnabled"].Cast<CheckBox>().CurrentValue || SpellManager.Ignite == null ||
                !SpellManager.Ignite.IsReady() ||
                ObjectManager.Player.IsDead)
            {
                return;
            }

            var target =
                HeroManager.Enemies.FirstOrDefault(
                    h =>
                        h.IsValidTarget(SpellManager.Ignite.Range) &&
                        h.Health < ObjectManager.Player.GetSummonerSpellDamage(h, Damage.SummonerSpell.Ignite));
            if (target != null)
            {
                SpellManager.Ignite.Cast(target);
            }
        }
    }
}