using CommonClassLibrary;
using System;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;
using System.Windows.Forms;
using System.Linq;

namespace SanguoshaServer.Game
{
    public class RoomMessage
    {
        public static void NotifyPlayerJoinorLeave(Room room, Client client, bool join)
        {
            try
            {
                string message = string.Format("{0}:{1}", join ? "@join_game" : "@leave_game", client.Profile.NickName);
                MyData data = new MyData
                {
                    Description = PacketDescription.Room2Cient,
                    Protocol = Protocol.Message2Room,
                    Body = new List<string> { string.Empty, message },
                };

                List<Client> clients = new List<Client>(room.Clients);
                foreach (Client c in clients)
                    if (c.GameRoom == room.RoomId)
                        c.SendMessage(data);
            }
            catch (Exception e)
            {
                room.Debug(string.Format("error on NotifyPlayerJoinorLeave {0} {1} {2}", e.Message, e.Source, e.HelpLink));
            }
        }

        public static void NotifyPlayerDisconnected(Room room, Client client)
        {
            try
            {
                string message = string.Format("@disconnected:{0}", client.Profile.NickName);
                MyData data = new MyData
                {
                    Description = PacketDescription.Room2Cient,
                    Protocol = Protocol.Message2Room,
                    Body = new List<string> { string.Empty, message },
                };

                List<Client> clients = new List<Client>(room.Clients);
                foreach (Client c in clients)
                    if (c.GameRoom == room.RoomId)
                        c.SendMessage(data);
            }
            catch (Exception e)
            {
                LogHelper.WriteLog(null, e);
                room.Debug(string.Format("error on NotifyPlayerDisconnected {0} {1} {2}", e.Message, e.Source, e.HelpLink));
            }
        }

        public static void SystemMessage(Room room, string message)
        {
            try
            {
                MyData data = new MyData
                {
                    Description = PacketDescription.Room2Cient,
                    Protocol = Protocol.Message2Room,
                    Body = new List<string> { string.Empty, message },
                };

                List<Client> clients = new List<Client>(room.Clients);
                foreach (Client c in clients)
                    if (c.GameRoom == room.RoomId)
                        c.SendMessage(data);
            }
            catch (Exception e)
            {
                LogHelper.WriteLog(null, e);
                room.Debug(string.Format("error on SystemMessage {0} {1} {2}", e.Message, e.Source, e.HelpLink));
            }
        }

        public static void RunOptions(Room room, Client client, IEnumerable<string> commandMessage) 
        {
            var parser = new Parser(config => config.HelpWriter = null);
            var parseresult = CommandLine.Parser.Default.ParseArguments<BanOptions>(commandMessage);
            parseresult.WithParsed<BanOptions>(options => Ban(options, room, client))
                       .WithNotParsed(errors => HandleParseError(errors, room, parseresult));
        }

        private static void HandleParseError<T>(IEnumerable<Error> errors, Room room, ParserResult<T> result)
        {
            string helpText = null;
            helpText = HelpText.AutoBuild(result);
            SystemMessage(room, helpText);

        }

        public static void Ban(BanOptions opts, Room room, Client client) 
        {          
            if(opts.RawBanList.Count() > 0)
            {
                if(client == room.Host)
                {
                    room.Setting.BanHeroList.Clear();
                    foreach (var i in opts.RawBanList)
                    {
                        room.Setting.BanHeroList.Add(i);
                        SystemMessage(room, string.Format("武将{0}加入到了禁将表中", i));
                    }
                }
                else if(room.Setting.BanHeroList.Count != 0)
                {
                    string banText = string.Join(",", room.Setting.BanHeroList);
                    SystemMessage(room, string.Format("此房间已禁将, {0}已被禁止使用", banText));
                }
                else
                {
                    SystemMessage(room, "此房间没有禁将");
                }
                

            }
        }

    }
}
