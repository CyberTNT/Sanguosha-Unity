﻿using System;
using System.Collections.Generic;
using System.Data;
using CommonClass;
using CommonClass.Game;
using CommonClassLibrary;

namespace SanguoshaServer.Game
{
    public class Bot
    {
        public static Profile GetBot(Room room)
        {
            List<string> used = new List<string>();
            foreach (Client client in room.Clients)
            {
                if (client.UserID < 0)
                    used.Add(client.Profile.NickName);
            }

            DataRow row = Engine.GetRandomBot(used);
            Profile profile = new Profile
            {
                NickName = row["id"].ToString(),
                Avatar = int.Parse(row["avatar"].ToString()),
                Frame = int.Parse(row["frame"].ToString()),
                Bg = int.Parse(row["bg"].ToString()),
                Title = int.Parse(row["title"].ToString()),
            };

            return profile;
        }

        public static string GetGreeting(string id)
        {
            DataRow row = Engine.GetLines(id);
            return row["greeting"].ToString();
        }

        public static void OnSkillShow(Room room, Player player, string skill)
        {
            List<Client> ais = new List<Client>();
            foreach (Client client in room.Clients)
                if (client.UserID < 0)
                    ais.Add(client);

            if (ais.Count == 0) return;

            DataRow[] rows = Engine.GetSkillShowingLines();
            foreach (DataRow row in rows)
            {
                if (row["skills"].ToString() == skill)
                {
                    foreach (Client ai in ais)
                    {
                        //几率1/3聊天
                        if (Shuffle.random(1, 3))
                        {
                            //50%随机是发言还是表情
                            bool speak = Shuffle.random(1, 2);
                            if (room.Setting.SpeakForbidden)
                                speak = false;
                            //50%随机
                            int num = Shuffle.random(1,2)? 1 : 2;
                            if (room.GetClient(player) == ai)
                            {
                                if (speak)
                                {
                                    string message = row[string.Format("self_lines{0}", num)].ToString();
                                    if (!string.IsNullOrEmpty(message))
                                        room.Speak(ai, message);
                                }
                                else
                                {
                                    string messages = row[string.Format("self_emotion{0}", num)].ToString();
                                    if (!string.IsNullOrEmpty(messages))
                                    {
                                        string[] ms = messages.Split('/');
                                        room.Emotion(ai, ms[0], ms[1]);
                                    }
                                }
                            }
                            else
                            {
                                if (speak)
                                {
                                    string message = row[string.Format("lines{0}", num)].ToString();
                                    if (!string.IsNullOrEmpty(message))
                                        room.Speak(ai, message);
                                }
                                else
                                {
                                    string messages = row[string.Format("emotion{0}", num)].ToString();
                                    if (!string.IsNullOrEmpty(messages))
                                    {
                                        string[] ms = messages.Split('/');
                                        room.Emotion(ai, ms[0], ms[1]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void BotChat(TriggerEvent triggerEvent, Room room, Player player, object data)
        {
            if (triggerEvent == TriggerEvent.EventPhaseStart && player.Phase == Player.PlayerPhase.Play)
            {
                OnPlayStart(room, player);
            }
            else if (triggerEvent == TriggerEvent.GameStart)
            {
                OnGameStart(room, player);
            }
            else if (triggerEvent == TriggerEvent.DamageDone)
            {
                OnDamaged(room, player, data);
            }
            else if (triggerEvent == TriggerEvent.Death)
            {
                OnDeath(room, player, data);
            }
            else if (triggerEvent == TriggerEvent.FinishJudge)
            {
                OnJudgeDone(room, player, data);
            }
            else if (triggerEvent == TriggerEvent.CardTargetAnnounced)
            {
                OnUseCard(room, player, data);
            }
            else if (triggerEvent == TriggerEvent.TargetConfirming)
            {
                OnTargeted(room, player, data);
            }
        }

        private static void OnTargeted(Room room, Player player, object data)
        {
            if (data is CardUseStruct use)
            {
                if (use.Card.Name.Contains("Slash") && use.To.Count > 0 && room.Setting.GameMode == "Hegemony")
                {
                    List<Client> ais = new List<Client>();
                    foreach (Player p in use.To)
                    {
                        Client client = room.GetClient(p);
                        if (p.ClientId < 0 && !ais.Contains(client) && room.GetClient(p) != client && !room.GetAI(player, true).IsKnown(player, p) && Shuffle.random(1, 3))
                            ais.Add(client);
                    }

                    if (ais.Count == 0) return;

                    foreach (Client client in ais)
                    {
                        bool speak = Shuffle.random(1, 2);
                        if (room.Setting.SpeakForbidden)
                            speak = false;

                        DataRow row = Engine.GetLines(client.Profile.NickName);
                        if (speak)
                        {
                            string message = row["slash_target1"].ToString();
                            if (!string.IsNullOrEmpty(message))
                            {
                                message = message.Replace("%1", player.SceenName);
                                room.Speak(client, message);
                            }
                        }
                        else
                        {
                            string messages = row["slash_target2"].ToString();
                            if (!string.IsNullOrEmpty(messages))
                            {
                                string[] ms = messages.Split('/');
                                room.Emotion(client, ms[0], ms[1]);
                            }
                        }
                    }
                }
            }
        }

        private static void OnUseCard(Room room, Player player, object data)
        {
            if (data is CardUseStruct use)
            {
                if (use.Card.Name == "Analeptic"
                    && (use.Reason == CardUseStruct.CardUseReason.CARD_USE_REASON_PLAY || room.GetRoomState().GetCurrentCardUsePattern(player) == "@@rende")
                    && use.Card.Skill != "_zhendu")
                {
                    List<Client> ais = new List<Client>();
                    foreach (Player p in room.GetAlivePlayers())
                    {
                        Client client = room.GetClient(p);
                        if (p.ClientId < 0 && !ais.Contains(client) && RoomLogic.InMyAttackRange(room, player, p) && !RoomLogic.IsFriendWith(room, player, p)
                            && room.GetClient(player) != client && Shuffle.random(1, 3))
                            ais.Add(client);
                    }

                    if (ais.Count == 0) return;
                    foreach (Client client in ais)
                    {
                        bool speak = Shuffle.random(1, 2);
                        if (room.Setting.SpeakForbidden)
                            speak = false;

                        DataRow row = Engine.GetLines(client.Profile.NickName);
                        if (speak)
                        {
                            string message = row["other_drunk1"].ToString();
                            if (!string.IsNullOrEmpty(message))
                            {
                                message = message.Replace("%1", player.SceenName);
                                room.Speak(client, message);
                            }
                        }
                        else
                        {
                            string messages = row["other_drunk2"].ToString();
                            if (!string.IsNullOrEmpty(messages))
                            {
                                string[] ms = messages.Split('/');
                                room.Emotion(client, ms[0], ms[1]);
                            }
                        }
                    }
                }
                else if (player.ClientId < 0 && use.Card.Name.Contains("Slash")
                    && (use.Reason == CardUseStruct.CardUseReason.CARD_USE_REASON_PLAY || room.GetRoomState().GetCurrentCardUsePattern(player) == "@@rende")
                    && use.To.Count == 1 && !RoomLogic.IsFriendWith(room, player, use.To[0]) && Shuffle.random(1, 3))
                {
                    Client client = room.GetClient(player);

                    bool speak = Shuffle.random(1, 2);
                    if (room.Setting.SpeakForbidden)
                        speak = false;

                    DataRow row = Engine.GetLines(client.Profile.NickName);
                    if (speak)
                    {
                        string message = row["slash_use1"].ToString();
                        if (!string.IsNullOrEmpty(message))
                        {
                            message = message.Replace("%1", use.To[0].SceenName);
                            room.Speak(client, message);
                        }
                    }
                    else
                    {
                        string messages = row["slash_use2"].ToString();
                        if (!string.IsNullOrEmpty(messages))
                        {
                            string[] ms = messages.Split('/');
                            room.Emotion(client, ms[0], ms[1]);
                        }
                    }
                }
            }
        }

        //判定结束
        private static void OnJudgeDone(Room room, Player player, object data)
        {
            if (player.ClientId < 0 && data is JudgeStruct judge)
            {
                Client client = room.GetClient(player);
                if (judge.Reason == "Lightning")
                {
                    bool suc = RoomLogic.GetCardSuit(room, judge.Card) == WrappedCard.CardSuit.Spade
                        && RoomLogic.GetCardNumber(room, judge.Card) > 1 && RoomLogic.GetCardNumber(room, judge.Card) < 10;
                    if (suc && Shuffle.random(1, 3))
                    {
                        bool speak = Shuffle.random(1, 2);
                        if (room.Setting.SpeakForbidden)
                            speak = false;

                        DataRow row = Engine.GetLines(client.Profile.NickName);
                        if (speak)
                        {
                            string message = row["lightning_success1"].ToString();
                            if (!string.IsNullOrEmpty(message))
                            {
                                room.Speak(client, message);
                            }
                        }
                        else
                        {
                            string messages = row["lightning_success2"].ToString();
                            if (!string.IsNullOrEmpty(messages))
                            {
                                string[] ms = messages.Split('/');
                                room.Emotion(client, ms[0], ms[1]);
                            }
                        }
                    }
                }
                else if (judge.Reason == "Indulgence" && Shuffle.random(1, 3))
                {
                    bool suc = RoomLogic.GetCardSuit(room, judge.Card) != WrappedCard.CardSuit.Heart;
                    bool speak = Shuffle.random(1, 2);
                    if (room.Setting.SpeakForbidden)
                        speak = false;

                    DataRow row = Engine.GetLines(client.Profile.NickName);
                    if (speak)
                    {
                        string message = suc ? row["indu_success1"].ToString() : row["indu_fail1"].ToString();
                        if (!string.IsNullOrEmpty(message))
                        {
                            room.Speak(client, message);
                        }
                    }
                    else
                    {
                        string messages = suc ? row["indu_success2"].ToString() : row["indu_fail2"].ToString();
                        if (!string.IsNullOrEmpty(messages))
                        {
                            string[] ms = messages.Split('/');
                            room.Emotion(client, ms[0], ms[1]);
                        }
                    }
                }
            }
        }

        //阵亡聊天
        private static void OnDeath(Room room, Player player, object data)
        {
            if (player.ClientId < 0 && data is DeathStruct death)
            {
                //几率1/3聊天
                if (Shuffle.random(1, 3))
                {
                    Client client = room.GetClient(player);
                    //50%随机是发言还是表情
                    bool speak = Shuffle.random(1, 2);
                    if (room.Setting.SpeakForbidden)
                        speak = false;

                    DataRow row = Engine.GetLines(client.Profile.NickName);
                    if (speak)
                    {
                        string message = row["death1"].ToString();
                        if (!string.IsNullOrEmpty(message) && (!message.Contains("%1") || death.Damage.From != null))
                        {
                            message = message.Replace("%1", death.Damage.From.SceenName);
                            room.Speak(client, message);
                        }
                    }
                    else
                    {
                        string messages = row["death2"].ToString();
                        if (!string.IsNullOrEmpty(messages))
                        {
                            string[] ms = messages.Split('/');
                            room.Emotion(client, ms[0], ms[1]);
                        }
                    }
                }

                //杀死非友方时
                if (death.Damage.From != null && death.Damage.From != player && death.Damage.From.ClientId < 0
                    && !RoomLogic.IsFriendWith(room, death.Damage.From, player) && Shuffle.random(1, 3))
                {
                    Client client = room.GetClient(death.Damage.From);
                    //50%随机是发言还是表情
                    bool speak = Shuffle.random(1, 2);
                    if (room.Setting.SpeakForbidden)
                        speak = false;

                    DataRow row = Engine.GetLines(client.Profile.NickName);
                    if (speak)
                    {
                        string message = row["kill1"].ToString();
                        if (!string.IsNullOrEmpty(message) && !message.Contains("%1"))
                        {
                            message = message.Replace("%1", player.SceenName);
                            room.Speak(client, message);
                        }
                    }
                    else
                    {
                        string messages = row["kill2"].ToString();
                        if (!string.IsNullOrEmpty(messages))
                        {
                            string[] ms = messages.Split('/');
                            room.Emotion(client, ms[0], ms[1]);
                        }
                    }
                }
            }
        }

        //受伤时聊天
        private static void OnDamaged(Room room, Player player, object data)
        {
            if (player.Alive && player.ClientId < 0 && data is DamageStruct damage && damage.Damage > 0 && damage.From != player && !damage.Transfer)
            {
                //几率1/3聊天
                if (Shuffle.random(1, 3))
                {
                    Client client = room.GetClient(player);
                    //50%随机是发言还是表情
                    bool speak = Shuffle.random(1, 2);
                    if (room.Setting.SpeakForbidden)
                        speak = false;

                    DataRow row = Engine.GetLines(client.Profile.NickName);
                    if (speak)
                    {
                        string message = row["damaged1"].ToString();
                        if (!string.IsNullOrEmpty(message) && (!message.Contains("%1") || damage.From != null))
                        {
                            message = message.Replace("%1", damage.From.SceenName);
                        }
                        room.Speak(client, message);
                    }
                    else
                    {
                        string messages = row["damaged2"].ToString();
                        if (!string.IsNullOrEmpty(messages))
                        {
                            string[] ms = messages.Split('/');
                            room.Emotion(client, ms[0], ms[1]);
                        }
                    }
                }
            }
        }

        private static void OnGameStart(Room room, Player player)
        {
            //机器人换皮肤
            if (player != null && player.ClientId < 0 && Shuffle.random(1, 3))
            {
                DataRow[] data1 = Engine.GetGeneralSkin(player.ActualGeneral1, room.Setting.GameMode);

                bool head = data1.Length > 0 && (string.IsNullOrEmpty(player.ActualGeneral2) || Shuffle.random(1, 2));
                if (head)
                {
                    string name = player.ActualGeneral1;
                    Random ra = new Random();
                    int result = ra.Next(1, data1.Length);
                    player.HeadSkinId = result;
                    if (player.General1Showed)
                        room.BroadcastProperty(player, "HeadSkinId");
                }

                if (!string.IsNullOrEmpty(player.ActualGeneral2) && (!head || Shuffle.random(1, 2)))
                {
                    DataRow[] data2 = Engine.GetGeneralSkin(player.ActualGeneral2, room.Setting.GameMode);
                    Random ra = new Random();
                    int result = ra.Next(0, data2.Length);
                    player.DeputySkinId = result;
                    if (player.General2Showed)
                        room.BroadcastProperty(player, "DeputySkinId");
                }
            }
        }

        public static void OnPlayStart(Room room, Player player)
        {
            List<Client> ais = new List<Client>();
            foreach (Client client in room.Clients)
                if (client.UserID < 0)
                    ais.Add(client);

            if (ais.Count == 0) return;

            DataRow[] rows = Engine.GetStarPlayLines();
            foreach (DataRow row in rows)
            {
                string str = row["skills"].ToString();
                bool check = false;
                string[] skills = str.Split('|');
                foreach (string skill in skills)
                {
                    bool _check = true;
                    foreach (string _skill in skill.Split('+'))
                    {
                        if (!RoomLogic.PlayerHasShownSkill(room, player, _skill))
                        {
                            _check = false;
                            break;
                        }
                    }

                    if (_check)
                    {
                        check = true;
                        break;
                    }
                }

                if (check)
                {
                    foreach (Client ai in ais)
                    {
                        //几率1/3聊天
                        if (Shuffle.random(1,3))
                        {
                            //50%随机是发言还是表情
                            bool speak = Shuffle.random(1,2);
                            if (room.Setting.SpeakForbidden)
                                speak = false;
                            //50%随机
                            int num = Shuffle.random(1, 2) ? 1 : 2;
                            if (room.GetClient(player) == ai)
                            {
                                if (speak)
                                {
                                    string message = row[string.Format("self_lines{0}", num)].ToString();
                                    if (!string.IsNullOrEmpty(message))
                                        room.Speak(ai, message);
                                }
                                else
                                {
                                    string messages = row[string.Format("self_emotion{0}", num)].ToString();
                                    if (!string.IsNullOrEmpty(messages))
                                    {
                                        string[] ms = messages.Split('/');
                                        room.Emotion(ai, ms[0], ms[1]);
                                    }
                                }
                            }
                            else
                            {
                                if (speak)
                                {
                                    string message = row[string.Format("lines{0}", num)].ToString();
                                    if (!string.IsNullOrEmpty(message))
                                        room.Speak(ai, message);
                                }
                                else
                                {
                                    string messages = row[string.Format("emotion{0}", num)].ToString();
                                    if (!string.IsNullOrEmpty(messages))
                                    {
                                        string[] ms = messages.Split('/');
                                        room.Emotion(ai, ms[0], ms[1]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
