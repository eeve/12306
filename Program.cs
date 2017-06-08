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
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Ticket.Query(
                from_station: args[0],
                to_station: args[1],
                date: args.Length >= 3 ? args[2] : null,
                types: args.Length >= 4 ? args[3] : null
            );
        }

    }
}
