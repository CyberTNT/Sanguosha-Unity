using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace SanguoshaServer.Game
{
    [Verb("ban", HelpText = "禁将相关的命令")]
    public class BanOptions 
    {
        [Value(0, MetaName = "禁将表", Required = true, HelpText = "这里写出一个或多个所禁武将的内部名(拼音的形式，如caocao)，武将名之间用空格分隔")]
        public IEnumerable<string> RawBanList { get; set; }
    }
}
