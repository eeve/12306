using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace cn12306
{

    class Ticket
    {
        private static string basicUrl = "https://kyfw.12306.cn/otn/leftTicket/query";

        private static HttpUtil httpUtil = new HttpUtil();

        private static string station_cache_file = null;

        private static bool IsDate(string s)
        {
            if (s == null)
            {
                return false;
            }
            else
            {
                try
                {
                    DateTime d = DateTime.Parse(s);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public Ticket() {
            station_cache_file = GetAppPath() + "/.station_cache";
        }

        private static string GetAppPath() {
            dynamic type = typeof(Program);
            return Path.GetDirectoryName(type.Assembly.Location);
        }

        public void Query(string from_station, string to_station, string date = null, string types = null)
        {
            if (inBreakTime())
            {
                Console.WriteLine("12306.cn网站每日06:00~23:00提供服务！");
                return;
            }

            // 初始化车站映射表
            Dictionary<string, string> stationMaps = StationMaps();

            // 起点站是否开启严格过滤模式
            bool strict_from_station = from_station.StartsWith("^");
            // 到达站是否开启严格过滤模式
            bool strict_to_station = to_station.StartsWith("^");

            if (strict_from_station)
            {
                from_station = from_station.Substring(1, from_station.Length - 1);
            }
            if (strict_to_station)
            {
                to_station = to_station.Substring(1, to_station.Length - 1);
            }

            string from_station_code = stationMaps.ContainsKey(from_station) ? stationMaps[from_station] : null;
            string to_station_code = stationMaps.ContainsKey(to_station) ? stationMaps[to_station] : null;
            if (string.IsNullOrEmpty(from_station_code))
            {
                throw new Exception("错误的起点站");
            }
            if (string.IsNullOrEmpty(to_station_code))
            {
                throw new Exception("错误的到达站");
            }
            if (!IsDate(date) || date.Equals("now"))
            {
                date = DateTime.Now.ToString("yyyy-MM-dd");
                Console.WriteLine($"本次查询采用默认日期：{date} (今日)");
                // throw new Exception("不正确的日期，日期格式必须为 YYYY-mm-dd");
            }
            else
            {
                if (DateTime.Parse(date) < DateTime.Now.Date)
                {
                    throw new Exception("不正确的日期，日期不能小于今日");
                }
                date = DateTime.Parse(date).ToString("yyyy-MM-dd");
                Console.WriteLine($"日期：{date}");
            }

            Console.WriteLine($"旅程：{from_station} ({from_station_code}) -> {to_station} ({to_station_code})");

            // 列车类型
            string[] train_types = null;
            if (!string.IsNullOrWhiteSpace(types))
            {
                train_types = types.Split(',');
            }

            Task<string> task;
            while (true)
            {
                var url = $"{basicUrl}?leftTicketDTO.train_date={date}&leftTicketDTO.from_station={from_station_code}&leftTicketDTO.to_station={to_station_code}&purpose_codes=ADULT";
                task = httpUtil.Get(url);
                if (task.Result != null)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("失败，正在重试...");
                }
            }
            TicketModel tm = JsonConvert.DeserializeObject<TicketModel>(task.Result);
            List<TicketDataModel> list = tm.GetTickets();

            // 精确筛选车站、车次类型
            list = list.Where(s =>
            {
                bool to = strict_from_station ? from_station.Equals(s.queryLeftNewDTO.from_station_name) : true;
                bool arrive = strict_to_station ? to_station.Equals(s.queryLeftNewDTO.to_station_name) : true;
                bool train_type = train_types != null ? train_types.Contains(s.queryLeftNewDTO.station_train_code.Substring(0, 1)) : true;
                return to && arrive && train_type;
            }).ToList();
            
            Console.WriteLine("点击购票: https://kyfw.12306.cn/otn/login/init");
            Console.WriteLine($"根据各筛选条件本次共查询到{list.Count}趟列车，详细列表如下：");
            Console.WriteLine();

            List<string> columns = new List<string>{
                "车次", "出发站", "到达站", "出发时间", "到达时间", "历时", "商务座", "特等座", "一等座", "二等座", "高级软卧", "软卧", "硬卧", "软座", "硬座", "无座", "其他", "是否可预定"
            };
            list.ForEachWithIndex((item, idx) =>
            {
                TicketNewModel model = item.queryLeftNewDTO;
                List<string> values = new List<string>{
                    model.station_train_code,
                    model.from_station_name,
                    model.to_station_name,
                    model.start_time,
                    model.arrive_time,
                    model.lishi,
                    model.swz_num,
                    model.tz_num,
                    model.zy_num,
                    model.ze_num,
                    model.gr_num,
                    model.rw_num,
                    model.yw_num,
                    model.rz_num,
                    model.yz_num,
                    model.wz_num,
                    model.qt_num,
                    string.IsNullOrEmpty(item.secretStr) ? "N" : "Y"
                };
            });
            // draw table
            Console.WriteLine(list.ToStringTable(
                columns.ToArray(),
                row => row.queryLeftNewDTO.station_train_code,
                row => row.queryLeftNewDTO.from_station_name,
                row => row.queryLeftNewDTO.to_station_name,
                row => row.queryLeftNewDTO.start_time,
                row => row.queryLeftNewDTO.arrive_time,
                row => row.queryLeftNewDTO.lishi,
                row => row.queryLeftNewDTO.swz_num,
                row => row.queryLeftNewDTO.tz_num,
                row => row.queryLeftNewDTO.zy_num,
                row => row.queryLeftNewDTO.ze_num,
                row => row.queryLeftNewDTO.gr_num,
                row => row.queryLeftNewDTO.rw_num,
                row => row.queryLeftNewDTO.yw_num,
                row => row.queryLeftNewDTO.rz_num,
                row => row.queryLeftNewDTO.yz_num,
                row => row.queryLeftNewDTO.wz_num,
                row => row.queryLeftNewDTO.qt_num,
                row => string.IsNullOrEmpty(row.secretStr) ? "N" : "Y"
            ));

        }

        private static Dictionary<string, string> ReadFile(string path)
        {
            StreamReader sr = File.OpenText(path);
            string nextLine;
            Dictionary<string, string> map = new Dictionary<string, string>();
            while ((nextLine = sr.ReadLine()) != null)
            {
                map.Add(nextLine.Split('@')[0], nextLine.Split('@')[1]);
            }
            return map;
        }

        private static void SaveFile(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        private static Dictionary<string, string> StationMaps()
        {
            Dictionary<string, string> map = null;
            try {
                map = ReadFile(station_cache_file);
            } catch {
            }
            if(map != null && map.Count > 0) {
                return map;
            } else {
                string temp = "";
                Task<string> task = httpUtil.Get("https://kyfw.12306.cn/otn/resources/js/framework/station_name.js?station_version=1.9013");
                MatchCollection mc = Regex.Matches(task.Result, @"@\w+\|([\u4e00-\u9fa5]+)\|(\w+)\|(\w+)\|(\w+)\|\d+");
                map = new Dictionary<string, string>();
                foreach (Match item in mc)
                {
                    GroupCollection gc = item.Groups;
                    // name = gc[1].Value,
                    // code = gc[2].Value,
                    // pinyin = gc[3].Value,
                    // initial = gc[4].Value
                    map.Add(gc[1].Value, gc[2].Value);
                    temp += $"{gc[1].Value}@{gc[2].Value}\n";
                }
                SaveFile(station_cache_file, temp);
                return map;
            }
        }

        private static bool inBreakTime()
        {
            bool inBreakTime = false;
            Task<HttpResponseMessage> task = httpUtil.GetResp("http://www.beijing-time.org/time.asp");
            try
            {
                foreach (var i in task.Result.Headers.GetValues("Date"))
                {
                    int hour = int.Parse(DateTime.Parse(i).ToString("HH"));
                    if (hour >= 23)
                    {
                        inBreakTime = true;
                    }
                    break;
                }
            }
            catch { }
            return inBreakTime;
        }

    }

    class TicketNewModel
    {
        public string arrive_time { get; set; } // "19:40"
        public string canWebBuy { get; set; } // "Y"
        public string controlled_train_flag { get; set; } // "0"
        public string end_station_telecode { get; set; } // "SBT"
        public string from_station_name { get; set; } // "北京"
        public string from_station_no { get; set; } // "01"
        public string from_station_telecode { get; set; } // "BJP"
        public string gg_num { get; set; } // "--"
        public string gr_num { get; set; } // "--"
        public string is_support_card { get; set; } // "0"
        public string lishi { get; set; } // "02:27"
        public string location_code { get; set; } // "P2"
        public string qt_num { get; set; } // "--"
        public string rw_num { get; set; } // "--"
        public string rz_num { get; set; } // "--"
        public string seat_types { get; set; } // "OOM"
        public string start_station_telecode { get; set; } // "BJP"
        public string start_time { get; set; } // "17:13"
        public string start_train_date { get; set; } // "20170607"
        public string station_train_code { get; set; } // "D51"
        public string swz_num { get; set; } // "--"
        public string to_station_name { get; set; } // "秦皇岛"
        public string to_station_no { get; set; } // "04"
        public string to_station_telecode { get; set; } // "QTP"
        public string train_no { get; set; } // "2400000D510P"
        public string train_seat_feature { get; set; } // "3"
        public string tz_num { get; set; } // "--"
        public string wz_num { get; set; } // "有"
        public string yb_num { get; set; } // "--"
        public string yp_ex { get; set; } // "O0O0M0"
        public string yp_info { get; set; } // "41dlKJwu5YB6Uu6XnhjnnLLkg8LCSqWs8roOsPPLL4yRXWrp"
        public string yw_num { get; set; } // "--"
        public string yz_num { get; set; } // "--"
        public string ze_num { get; set; } // "无"
        public string zy_num { get; set; } // "无"
    }

    class TicketDataModel
    {
        public string buttonTextInfo { get; set; }
        public string secretStr { get; set; }
        public TicketNewModel queryLeftNewDTO { get; set; }
    }

    class TicketDataListModel
    {
        public List<string> result { get; set; }
        public string flag { get; set; }
        public Dictionary<string, string> map { get; set; }
    }

    class TicketModel
    {
        public string validateMessagesShowId { get; set; }
        public bool status { get; set; }
        public int httpstatus { get; set; }
        public TicketDataListModel data { get; set; }
        public List<object> messages { get; set; }

        public List<TicketDataModel> GetTickets()
        {
            if (!this.status)
            {
                throw new Exception("status错误");
            }
            if (this.data == null || this.data.flag == null)
            {
                throw new Exception(string.Join("，", this.messages) ?? "没有data");
            }
            List<TicketDataModel> list = new List<TicketDataModel>();
            if (int.Parse(this.data.flag) == 1)
            {
                list = this.transfer(this.data.result, this.data.map);
            }
            return list;
        }

        // var trainListForIE = new string[list.Count];
        // list.ForEachWithIndex((item, idx) => {
        // 	trainListForIE[idx] = $"{item.queryLeftNewDTO.station_train_code}({item.queryLeftNewDTO.start_time}--{item.queryLeftNewDTO.arrive_time})";
        // });
        // Console.WriteLine(trainListForIE[0]);

        private List<TicketDataModel> transfer(List<string> result, Dictionary<string, string> map)
        {
            List<TicketDataModel> list = new List<TicketDataModel>();
            TicketDataModel tdm;
            result.ForEachWithIndex((item, idx) =>
            {
                string[] cm = item.Split('|');
                tdm = new TicketDataModel();
                tdm.secretStr = cm[0];
                tdm.buttonTextInfo = cm[1];
                tdm.queryLeftNewDTO = new TicketNewModel
                {
                    train_no = cm[2],
                    station_train_code = cm[3],
                    start_station_telecode = cm[4],
                    end_station_telecode = cm[5],
                    from_station_telecode = cm[6],
                    to_station_telecode = cm[7],
                    start_time = cm[8],
                    arrive_time = cm[9],
                    lishi = cm[10],
                    canWebBuy = cm[11],
                    yp_info = cm[12],
                    start_train_date = cm[13],
                    train_seat_feature = cm[14],
                    location_code = cm[15],
                    from_station_no = cm[16],
                    to_station_no = cm[17],
                    is_support_card = cm[18],
                    controlled_train_flag = cm[19],
                    gg_num = !string.IsNullOrEmpty(cm[20]) ? cm[20] : "--",
                    gr_num = !string.IsNullOrEmpty(cm[21]) ? cm[21] : "--",
                    qt_num = !string.IsNullOrEmpty(cm[22]) ? cm[22] : "--",
                    rw_num = !string.IsNullOrEmpty(cm[23]) ? cm[23] : "--",
                    rz_num = !string.IsNullOrEmpty(cm[24]) ? cm[24] : "--",
                    tz_num = !string.IsNullOrEmpty(cm[25]) ? cm[25] : "--",
                    wz_num = !string.IsNullOrEmpty(cm[26]) ? cm[26] : "--",
                    yb_num = !string.IsNullOrEmpty(cm[27]) ? cm[27] : "--",
                    yw_num = !string.IsNullOrEmpty(cm[28]) ? cm[28] : "--",
                    yz_num = !string.IsNullOrEmpty(cm[29]) ? cm[29] : "--",
                    ze_num = !string.IsNullOrEmpty(cm[30]) ? cm[30] : "--",
                    zy_num = !string.IsNullOrEmpty(cm[31]) ? cm[31] : "--",
                    swz_num = !string.IsNullOrEmpty(cm[32]) ? cm[32] : "--",
                    yp_ex = cm[33],
                    seat_types = cm[34],
                    from_station_name = map[cm[6]],
                    to_station_name = map[cm[7]]
                };
                list.Add(tdm);
            });
            return list;
        }
    }

    class StationModel
    {
        public string name { get; set; } // 站名
        public string code { get; set; } // 站编号
        public string pinyin { get; set; } // 站名全拼
        public string initial { get; set; } // 声母
    }

    static class ForEachExtensions
    {
        public static void ForEachWithIndex<T>(this IEnumerable<T> enumerable, Action<T, int> handler)
        {
            int idx = 0;
            foreach (T item in enumerable)
                handler(item, idx++);
        }
    }

}