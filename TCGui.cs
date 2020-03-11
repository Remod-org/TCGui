//#define DEBUG
//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using Oxide.Core;
//using System.Text;
//using System.Linq;
//using Oxide.Core.Plugins;
//using System.Linq;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using Oxide.Game.Rust.Cui;
//using Oxide.Core.Libraries.Covalence;

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
    [Info("Tool Cupboard GUI", "RFC1920", "1.0.0")]
    [Description("Oxide Plugin")]
    class TCGui : RustPlugin
    {
        #region vars
        const string TCGUI = "tcgui.editor";
        #endregion

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        #region init
        void Init()
        {
            //LoadVariables();

            AddCovalenceCommand("tc", "cmdTCGUI");

            permission.RegisterPermission("tcgui.use", this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["tcgui"] = "Tool Cupboard GUI",
                ["helptext1"] = "Tool Cupboard GUI instructions:",
                ["helptext2"] = "  type /tc to do stuff",
                ["close"] = "Close",
                ["me"] = "Me",
                ["cupboard"] = "Cupboard",
                ["turrets"] = "Turrets",
                ["add"] = "Add",
                ["remove"] = "Remove"
            }, this);
        }

        void Loaded()
        {
        }

        void Unload()
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, TCGUI);
            }
        }

        protected override void LoadDefaultConfig()
        {
//            Config.Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
//            Config.Settings.Converters = new JsonConverter[] { new UnityVector3Converter() };
        }
        #endregion

        #region Main
        bool GetRaycastTarget(BasePlayer player, out object closestEntity)
        {
            closestEntity = false;

            RaycastHit hit;
            if(Physics.Raycast(player.eyes.HeadRay(), out hit, 10f))
            {
                closestEntity = hit.GetEntity();
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
                    Puts($"got here {args[0]}");
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
                        else if(args[0] == "remove" && args.Length > 1)
                        {
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

        void tcGUI(BasePlayer player, BaseEntity entity)
        {
            CuiHelper.DestroyUi(player, TCGUI);
            if(false)
            {
                return;
            }

            // Create container, add top labels and buttons
            CuiElementContainer container = UI.Container(TCGUI, UI.Color("2b2b2b", 0.9f), "0.15 0.1", "0.85 0.9", true, "Overlay");
            UI.Button(ref container, TCGUI, UI.Color("#d85540", 1f), Lang("close"), 12, "0.92 0.93", "0.985 0.98", $"tc guiclose");
            UI.Label(ref container, TCGUI, UI.Color("#ffffff", 1f), Lang("tcgui"), 18, "0.23 0.92", "0.7 1");
            UI.Label(ref container, TCGUI, UI.Color("#ffffff", 1f), Lang("cupboard"), 14, "0.01 0.83", "0.5 0.9");
            UI.Label(ref container, TCGUI, UI.Color("#ffffff", 1f), Lang("turrets"), 14, "0.5 0.83", "0.8 0.9");

            BuildingPrivlidge privs = entity.GetComponentInParent<BuildingPrivlidge>();
            int nc = 0;
            float[] n = GetButtonPosition(nc, 1);
            float[] b = GetButtonPosition(nc, 2);
            UI.Label(ref container, TCGUI, UI.Color("#ffffff", 1f), Lang("me"), 12, $"{n[0]} {n[1]}", $"{n[0] + ((n[2] - n[0]) / 2)} {n[3]}", TextAnchor.MiddleLeft);
            bool authed = false;
            foreach(var auth in privs.authorizedPlayers.Select(x => x.userid).ToArray())
            {
                var findme = BasePlayer.Find(auth.ToString());
                if(findme.userID == player.userID) authed = true;
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
                var theplayer = BasePlayer.Find(auth.ToString());
                if(theplayer.userID == player.userID) continue;
                nc++;

                float[] posn = GetButtonPosition(nc, 1);
                float[] posb = GetButtonPosition(nc, 2);

                UI.Label(ref container, TCGUI, UI.Color("#ffffff", 1f), Lang("me"), 12, $"{posn[0]} {posn[1]}", $"{posn[0] + ((posn[2] - posn[0]) / 2)} {posn[3]}", TextAnchor.MiddleLeft);
                UI.Button(ref container, TCGUI, UI.Color("#d85540", 1f), Lang("remove"), 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"tc remove {theplayer.userID}");
            }

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
                    var theplayer = BasePlayer.Find(auth.ToString());
                    if(theplayer.userID == player.userID) continue;
                    nc++;

                    posn = GetButtonPosition(nc, 5);
                    float[] posb = GetButtonPosition(nc, 6);
                    UI.Label(ref container, TCGUI, UI.Color("#ffffff", 1f), theplayer.displayName, 12, $"{posn[0]} {posn[1]}", $"{posn[0] + ((posn[2] - posn[0]) / 2)} {posn[3]}", TextAnchor.MiddleLeft);
                    UI.Button(ref container, TCGUI, UI.Color("#d85540", 1f), Lang("remove"), 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"tc tremove {theplayer.userID} {turret.net.ID.ToString()}");
                }
            }

            CuiHelper.AddUi(player, container);
        }

        private int RowNumber(int max, int count) => Mathf.FloorToInt(count / max);
        private float[] GetButtonPosition(int rowNumber, int columnNumber)
        {
            // Left, Bottom, Right, Top
            float offsetX = 0.05f + (0.096f * columnNumber);
            float offsetY = (0.75f - (rowNumber * 0.074f));

            return new float[] { offsetX, offsetY, offsetX + 0.196f, offsetY + 0.05f };
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