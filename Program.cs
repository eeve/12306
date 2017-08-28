using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace cn12306
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 2) {
                Console.WriteLine("ERR: 请指定起点站和到达站！\n");
                Console.WriteLine("请按照此顺序指定参数：起点站 到达站 [时间] [列车类型（G,C,D,Z,T,K）]");
                Console.WriteLine("小技巧：可使用 ^ 标记精确站点。例如：明确指定起点站为北京西站，可指定起点站为 ^北京西");
                return;
            }
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            try {
                Ticket ticket = new Ticket();
                ticket.Query(
                    from_station: args[0],
                    to_station: args[1],
                    date: args.Length >= 3 ? args[2] : null,
                    types: args.Length >= 4 ? args[3] : null
                );
            } catch(Exception e) {
                Console.WriteLine($"ERR: {e.Message}");
            }   
        }

    }
}
