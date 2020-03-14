//#define DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Diagnostics;

namespace Oxide.Plugins
{
    [Info("Tool Cupboard GUI", "RFC1920", "1.0.1")]
    [Description("Manage TC and Turret auth")]
    class TCGui : RustPlugin
    {
        #region vars
        [PluginReference]
        Plugin HumanNPC;

        const string TCGUI = "tcgui.editor";
        const string TCGUP = "tcgui.players";
        const string TCGUB = "tcgui.button";
        private static TCGui ins;
        private Dictionary<string, string> onlinePlayers = new Dictionary<string, string>();
        private Dictionary<string, string> offlinePlayers = new Dictionary<string, string>();
        private uint cuploot;
        #endregion

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        #region init
        void Init()
        {
            AddCovalenceCommand("tc", "cmdTCGUI");

            permission.RegisterPermission("tcgui.use", this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["tcgui"] = "Tool Cupboard GUI",
                ["tcguisel"] = "Tool Cupboard GUI - Player Select",
                ["helptext1"] = "Tool Cupboard GUI instructions:",
                ["helptext2"] = "  type /tc to do stuff",
                ["close"] = "Close",
                ["me"] = "Me",
                ["manage"] = "Manage",
                ["none"] = "None found!",
                ["cupboard"] = "Cupboard",
                ["turret"] = "Turret",
                ["turrets"] = "Turrets",
                ["select"] = "Select",
                ["add"] = "Add",
                ["remove"] = "Remove"
            }, this);
        }

        void Unload()
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, TCGUI);
                CuiHelper.DestroyUi(player, TCGUP);
                CuiHelper.DestroyUi(player, TCGUB);
            }
        }

        private object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            BuildingPrivlidge privs = container.GetComponentInParent<BuildingPrivlidge>();
            if(privs == null) return null;
            cuploot = privs.net.ID;
            tcButtonGUI(player, privs);

            return null;
        }

        void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if(cuploot == 0) return;
            if(entity == null) return;
            if(entity.net.ID == cuploot)
            {
                CuiHelper.DestroyUi(player, TCGUB);
                cuploot = 0;
            }
        }
        #endregion

        #region Main
        bool GetRaycastTarget(BasePlayer player, out object closestEntity)
        {
            Puts($"Trying to find target TC for {player.displayName}");
            closestEntity = false;

            RaycastHit hit;
            if(Physics.Raycast(player.eyes.HeadRay(), out hit, 3f))
            {
                closestEntity = hit.GetEntity();
#if DEBUG
                Puts($"Found entity {(closestEntity as BaseEntity).ShortPrefabName}");
#endif
                return true;
            }
            return false;
        }

        [Command("tc")]
        void cmdTCGUI(IPlayer iplayer, string command, string[] args)
        {
            var player = iplayer.Object as BasePlayer;
            if(args.Length > 0)
            {
                if(args[0] == "guiclose")
                {
                    CuiHelper.DestroyUi(player, TCGUI);
                }
            }

            object target;
            if(GetRaycastTarget(player, out target))
            {
                var ent = target as BaseEntity;
                string remplayer = null;
                if(ent.ShortPrefabName.Contains("cupboard"))
                {
                    if(args.Length > 0)
                    {
                        if(args[0] == "gui")
                        {
                            tcGUI(player, ent);
                        }
                        else if(args[0] == "guibtn")
                        {
                            tcGUI(player, ent);
                        }
                        else if(args[0] == "guisel")
                        {
                            if(args.Length > 2)
                            {
                                // tc guisel turret turretid
                                PlayerSelectGUI(player, "turret", uint.Parse(args[2]));
                            }
                            else
                            {
                                PlayerSelectGUI(player);
                            }
                        }
                        else if(args[0] == "guiselclose")
                        {
                            CuiHelper.DestroyUi(player, TCGUP);
                        }
                        else if(args[0] == "remove" && args.Length > 1)
                        {
#if DEBUG
                            Puts($"Removing player ({args[1]}) from TC");
#endif
                            CuiHelper.DestroyUi(player, TCGUP);
                            // tc remove 7656XXXXXXXXXXXX
                            BuildingPrivlidge privs = ent.GetComponentInParent<BuildingPrivlidge>();

                            foreach (var p in privs.authorizedPlayers.ToArray ())
                            {
                                if (p.userid == ulong.Parse(args[1]))
                                {
                                    privs.authorizedPlayers.Remove(p);
                                    privs.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                                }
                            }
                            tcGUI(player, ent);
                        }
                        else if(args[0] == "add" && args.Length > 2)
                        {
#if DEBUG
                            Puts($"Adding player ({args[1]}/{args[2]}) to TC");
#endif
                            CuiHelper.DestroyUi(player, TCGUP);
                            // tc add 7656XXXXXXXXXXXX RFC1920
                            BuildingPrivlidge privs = ent.GetComponentInParent<BuildingPrivlidge>();

                            privs.authorizedPlayers.Add(new ProtoBuf.PlayerNameID()
                            {
                                userid = ulong.Parse(args[1]),
                                username = args[2]
                            });
                            privs.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            tcGUI(player, ent);
                        }
                        else if(args[0] == "tremove" && args.Length > 2)
                        {
                            CuiHelper.DestroyUi(player, TCGUP);
                            // tc tremove 7656XXXXXXXXXXXX TURRETID
                            var turret = BaseNetworkable.serverEntities.Find(uint.Parse(args[2])) as AutoTurret;

                            foreach(var p in turret.authorizedPlayers.ToArray())
                            {
                                if(p.userid == ulong.Parse(args[1]))
                                {
                                    turret.authorizedPlayers.Remove(p);
                                    turret.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                                }
                            }
                            tcGUI(player, ent);
                        }
                        else if(args[0] == "tadd" && args.Length > 3)
                        {
                            CuiHelper.DestroyUi(player, TCGUP);
                            // tc tadd 7656XXXXXXXXXXXX NAME TURRETID
                            var turret = BaseNetworkable.serverEntities.Find(uint.Parse(args[3])) as AutoTurret;

                            turret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID()
                            {
                                userid = ulong.Parse(args[1]),
                                username = args[2]
                            });
                            turret.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            tcGUI(player, ent);
                        }
                    }
                    else
                    {
                        Message(iplayer, "Found a TC:");
                        BuildingPrivlidge privs = ent.GetComponentInParent<BuildingPrivlidge>();

                        foreach(var auth in privs.authorizedPlayers.Select(x => x.userid).ToArray())
                        {
                            var theplayer = BasePlayer.Find(auth.ToString());
                            Message(iplayer, theplayer.displayName);
                        }
                    }
                }
            }
        }

        BuildingPrivlidge GetBP(BaseEntity entity)
        {
            return entity.GetComponentInParent<BuildingPrivlidge>();
        }

        List<AutoTurret> GetTurrets(Vector3 location, float range = 30f)
        {
            List<AutoTurret> turrets = new List<AutoTurret>();
            Vis.Entities<AutoTurret>(location, range, turrets);
            return turrets;
        }

        private static HashSet<BasePlayer> FindPlayers(string nameOrIdOrIp)
        {
            var players = new HashSet<BasePlayer>();
            if (string.IsNullOrEmpty(nameOrIdOrIp)) return players;
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
                else if (!string.IsNullOrEmpty(activePlayer.displayName) && activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(activePlayer);
                else if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(sleepingPlayer);
                else if (!string.IsNullOrEmpty(sleepingPlayer.displayName) && sleepingPlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(sleepingPlayer);
            }
            return players;
        }

        void tcGUI(BasePlayer player, BaseEntity entity)
        {
            CuiHelper.DestroyUi(player, TCGUI);

            // Create container, add top labels and buttons
            CuiElementContainer container = UI.Container(TCGUI, UI.Color("2b2b2b", 0.9f), "0.15 0.1", "0.85 0.9", true, "Overlay");
            UI.Button(ref container, TCGUI, UI.Color("#d85540", 1f), Lang("close"), 12, "0.92 0.93", "0.985 0.98", $"tc guiclose");
            UI.Label(ref container, TCGUI, UI.Color("#ffffff", 1f), Lang("tcgui"), 18, "0.23 0.92", "0.7 1");
            UI.Label(ref container, TCGUI, UI.Color("#ffffff", 1f), Lang("cupboard"), 14, "0.15 0.83", "0.28 0.9");
            UI.Label(ref container, TCGUI, UI.Color("#ffffff", 1f), Lang("turrets"), 14, "0.3 0.83", "0.8 0.9");

            int nc = 0;
            float[] n = GetButtonPosition(nc, 1);
            float[] b = GetButtonPosition(nc, 2);
            UI.Label(ref container, TCGUI, UI.Color("#ffffff", 1f), Lang("me"), 12, $"{n[0]} {n[1]}", $"{n[0] + ((n[2] - n[0]) / 2)} {n[3]}", TextAnchor.MiddleLeft);
            bool authed = false;

            BuildingPrivlidge privs = entity.GetComponentInParent<BuildingPrivlidge>();
            foreach(var auth in privs.authorizedPlayers.Select(x => x.userid).ToArray())
            {
                var findme = BasePlayer.Find(auth.ToString());
                if(findme == null) continue;
                if(findme.userID == player.userID) authed = true;
                break;
            }

            if(authed)
            {
                UI.Button(ref container, TCGUI, UI.Color("#d85540", 1f), Lang("remove"), 12, $"{b[0]} {b[1]}", $"{b[0] + ((b[2] - b[0]) / 2)} {b[3]}", $"tc remove {player.userID}");
            }
            else
            {
                UI.Button(ref container, TCGUI, UI.Color("#cccccc", 1f), Lang("add"), 12, $"{b[0]} {b[1]}", $"{b[0] + ((b[2] - b[0]) / 2)} {b[3]}", $"tc add {player.userID} {player.displayName}");
            }

            foreach(var auth in privs.authorizedPlayers.Select(x => x.userid).ToArray())
            {
                BasePlayer theplayer = FindPlayers(auth.ToString()).FirstOrDefault();
                if(theplayer == null) continue;
#if DEBUG
                Puts($"Found authorized player ({theplayer.userID}/{theplayer.displayName})");
#endif

                if(theplayer.userID == player.userID) continue;
                nc++;

                float[] posn = GetButtonPosition(nc, 1);
                float[] posb = GetButtonPosition(nc, 2);

                UI.Label(ref container, TCGUI, UI.Color("#ffffff", 1f), theplayer.displayName, 12, $"{posn[0]} {posn[1]}", $"{posn[0] + ((posn[2] - posn[0]) / 2)} {posn[3]}", TextAnchor.MiddleLeft);
                UI.Button(ref container, TCGUI, UI.Color("#d85540", 1f), Lang("remove"), 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"tc remove {theplayer.userID}");

            }
            nc++;

            float[] poss = GetButtonPosition(nc, 2);
            UI.Button(ref container, TCGUI, UI.Color("115540", 1f), Lang("select"), 12, $"{poss[0]} {poss[1]}", $"{poss[0] + ((poss[2] - poss[0]) / 2)} {poss[3]}", $"tc guisel");

            List<AutoTurret> turrets = GetTurrets(player.transform.position, 30f);
            List<ulong> foundturrets = new List<ulong>();

            nc = -1;
            foreach(var turret in turrets)
            {
                if(foundturrets.Contains(turret.net.ID)) continue;
                foundturrets.Add(turret.net.ID);

                nc++;
                float[] posn = GetButtonPosition(nc, 4);
                UI.Label(ref container, TCGUI, UI.Color("#ffffff", 1f), turret.net.ID.ToString(), 12, $"{posn[0]} {posn[1]}", $"{posn[0] + ((posn[2] - posn[0]) / 2)} {posn[3]}", TextAnchor.MiddleLeft);

                n = GetButtonPosition(nc, 5);
                b = GetButtonPosition(nc, 6);
                UI.Label(ref container, TCGUI, UI.Color("#ffffff", 1f), Lang("me"), 12, $"{n[0]} {n[1]}", $"{n[0] + ((n[2] - n[0]) / 2)} {n[3]}", TextAnchor.MiddleLeft);

                authed = false;
                foreach(var auth in turret.authorizedPlayers.Select(x => x.userid).ToArray())
                {
                    var findme = BasePlayer.Find(auth.ToString());
                    if(findme.userID == player.userID) authed = true;
                }
    
                if(authed)
                {
                    UI.Button(ref container, TCGUI, UI.Color("#d85540", 1f), Lang("remove"), 12, $"{b[0]} {b[1]}", $"{b[0] + ((b[2] - b[0]) / 2)} {b[3]}", $"tc tremove {player.userID} {turret.net.ID.ToString()}");
                }
                else
                {
                    UI.Button(ref container, TCGUI, UI.Color("#cccccc", 1f), Lang("add"), 12, $"{b[0]} {b[1]}", $"{b[0] + ((b[2] - b[0]) / 2)} {b[3]}", $"tc tadd {player.userID} {player.displayName} {turret.net.ID.ToString()}");
                }

                foreach(var auth in turret.authorizedPlayers.Select(x => x.userid).ToArray())
                {
                    BasePlayer theplayer = FindPlayers(auth.ToString()).FirstOrDefault();
                    if(theplayer.userID == player.userID) continue;
                    nc++;

                    posn = GetButtonPosition(nc, 5);
                    float[] posb = GetButtonPosition(nc, 6);
                    UI.Label(ref container, TCGUI, UI.Color("#ffffff", 1f), theplayer.displayName, 12, $"{posn[0]} {posn[1]}", $"{posn[0] + ((posn[2] - posn[0]) / 2)} {posn[3]}", TextAnchor.MiddleLeft);
                    UI.Button(ref container, TCGUI, UI.Color("#d85540", 1f), Lang("remove"), 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"tc tremove {theplayer.userID} {turret.net.ID.ToString()}");
                    // NOT SHOWING :(
                }
                nc++;
                poss = GetButtonPosition(nc, 6);
                UI.Button(ref container, TCGUI, UI.Color("#115540", 1f), Lang("select"), 12, $"{poss[0]} {poss[1]}", $"{poss[0] + ((poss[2] - poss[0]) / 2)} {poss[3]}", $"tc guisel turret {turret.net.ID.ToString()}");
            }

            CuiHelper.AddUi(player, container);
        }

        void PlayerSelectGUI(BasePlayer player, string mode = "cupboard", uint turretid=0)
        {
            CuiHelper.DestroyUi(player, TCGUP);
            Puts($"Building player select gui for mode {mode}...");

            // Create container, add top labels and buttons
            string description = Lang("tcguisel") + ": " + Lang(mode);
            if(mode == "turret") description += $" {turretid.ToString()}";
            CuiElementContainer container = UI.Container(TCGUP, UI.Color("242424", 1f), "0.15 0.1", "0.85 0.9", true, "Overlay");
            UI.Label(ref container, TCGUP, UI.Color("#ffffff", 1f), description, 18, "0.23 0.92", "0.7 1");
            UI.Button(ref container, TCGUP, UI.Color("#d85540", 1f), Lang("close"), 12, "0.92 0.93", "0.985 0.98", $"tc guiselclose");
            int col = 1;
            int row = 1;
            bool found = false;

            List<ulong> npcs = (List<ulong>)HumanNPC?.Call("HumanNPCs");
            foreach(BasePlayer user in BasePlayer.activePlayerList)
            {
                if(npcs != null && npcs.Contains(user.userID)) continue;
                if(user.userID == player.userID) continue;
                found = true;
                if(row > 9)
                {
                    row = 1;
                    col++;
                }
                float[] posb = GetButtonPosition(row, col);
                if(mode == "turret" && turretid > 0)
                {
                    UI.Button(ref container, TCGUP, UI.Color("#d85540", 1f), user.displayName, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"tc tadd {user.userID} {turretid.ToString()}");
                }
                else
                {
                    UI.Button(ref container, TCGUP, UI.Color("#d85540", 1f), user.displayName, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"tc add {user.userID} {user.displayName}");
                }
                row++;
            }
            foreach(BasePlayer user in BasePlayer.sleepingPlayerList)
            {
                found = true;
                if(npcs != null && npcs.Contains(user.userID)) continue;
                if(row > 9)
                {
                    row = 1;
                    col++;
                }
                float[] posb = GetButtonPosition(row, col);
                if(mode == "turret" && turretid > 0)
                {
                    UI.Button(ref container, TCGUP, UI.Color("#555500", 1f), user.displayName, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"tc tadd {user.userID} {turretid.ToString()}");
                }
                else
                {
                    UI.Button(ref container, TCGUP, UI.Color("#555500", 1f), user.displayName, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"tc add {user.userID} {user.displayName}");
                }
                row++;
            }
            if(!found)
            {
                UI.Label(ref container, TCGUP, UI.Color("#ffffff", 1f), Lang("none"), 12, "0.2 0.4", "0.7 1");
            }

            CuiHelper.AddUi(player, container);
        }

        void tcButtonGUI(BasePlayer player, BaseEntity entity)
        {
            CuiHelper.DestroyUi(player, TCGUB);

            CuiElementContainer container = UI.Container(TCGUB, UI.Color("cccccc", 1f), "0.9 0.812", "0.946 0.835", true, "Overlay");
            UI.Button(ref container, TCGUB, UI.Color("#333333", 1f), Lang("manage"), 10, "0 0", "1 1", $"tc guibtn");

            CuiHelper.AddUi(player, container);
        }

        private int RowNumber(int max, int count) => Mathf.FloorToInt(count / max);
        private float[] GetButtonPosition(int rowNumber, int columnNumber)
        {
            float offsetX = 0.05f + (0.096f * columnNumber);
            float offsetY = (0.80f - (rowNumber * 0.064f));

            return new float[] { offsetX, offsetY, offsetX + 0.196f, offsetY + 0.03f };
        }
        #endregion

        #region Classes
        public static class UI
        {
            public static CuiElementContainer Container(string panel, string color, string min, string max, bool useCursor = false, string parent = "Overlay")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = { Color = color },
                            RectTransform = {AnchorMin = min, AnchorMax = max},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };
                return container;
            }
            public static void Panel(ref CuiElementContainer container, string panel, string color, string min, string max, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = min, AnchorMax = max },
                    CursorEnabled = cursor
                },
                panel);
            }
            public static void Label(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = min, AnchorMax = max }
                },
                panel);

            }
            public static void Button(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f },
                    RectTransform = { AnchorMin = min, AnchorMax = max },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
            public static void Input(ref CuiElementContainer container, string panel, string color, string text, int size, string command, string min, string max)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            CharsLimit = 30,
                            Color = color,
                            Command = command + text,
                            FontSize = size,
                            IsPassword = false,
                            Text = text
                        },
                        new CuiRectTransformComponent { AnchorMin = min, AnchorMax = max },
                        new CuiNeedsCursorComponent()
                    }
                });
            }
            public static string Color(string hexColor, float alpha)
            {
                if(hexColor.StartsWith("#"))
                {
                    hexColor = hexColor.Substring(1);
                }
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        #endregion
    }
}
