using Microsoft.Phone.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mini_monster
{
    class MonsterExecutor
    {
        public static async Task<string> GetTemperature(string monster_url)
        {
            string response_task = await ReadUrlAsync(monster_url + "?temp=");
            return ParseParameters(response_task)[1];
        }

        public static async Task<string> GetMonsterName(string monster_url)
        {
            string response_task = await ReadUrlAsync(monster_url);
            return ParseParameters(response_task)[5];
        }

        public static async Task<bool> RenamePort(string monster_url, int port_index, string port_name)
        {
            string response_task = await ReadUrlAsync(monster_url + "?chn&prt=" + port_index + "&p_vl=" + port_name);
            ObservableCollection<Port> ports = await GetPorts(monster_url);
            return ports.Any(port => (port.PortIndex == port_index && port.PortName.Equals(port_name)));
        }

        public static async Task<bool> RenameMonster(string monster_url, string monster_name)
        {
            string response_task = await ReadUrlAsync(monster_url + "?sett&id=" + monster_name);
            return response_task.Contains("Saved");
        }

        public static async Task<int> GetPWMValue(string monster_url)
        {
            string response_task = await ReadUrlAsync(monster_url + "?pwm=");
            return ParsePWM(response_task);
        }

        public static async Task<bool> UpdatePWM(string monster_url, int value)
        {
            string response_task = await ReadUrlAsync(monster_url + "?ecpw=" + value);
            return (ParsePWM(response_task) == value);
        }

        private static int ParsePWM(string content)
        {
            int value_start = content.IndexOf("value=\"") + 7;
            int value_end = content.IndexOf("\" ", value_start);
            return int.Parse(content.Substring(value_start, value_end - value_start));
        }

        public static async Task<ObservableCollection<Port>> SwitchPort(string monster_url, int port_index, bool port_state)
        {
            string response_task = await ReadUrlAsync(monster_url + "?sw=" + port_index + "-" + (port_state ? 1 : 0));
            return ParseSwitchersList(response_task);
        }

        public static async Task<ObservableCollection<Port>> GetPorts(string monster_url)
        {
            string response_task = await ReadUrlAsync(monster_url + "?main=");
            return ParseSwitchersList(response_task);
        }

        private static string[] ParseParameters(string content)
        {
            content = Regex.Escape(content);
            string REG_EXP = "^(.*?)</frameset>(.*?)$";
            Regex regex = new Regex(REG_EXP, RegexOptions.IgnoreCase);
            Match match = regex.Match(content);
            if (match.Success)
            {
                string settings = match.Groups[2].Value;
                settings = Regex.Unescape(settings);
                string[] param = settings.Split(',');
                return param;
            }
            throw new InvalidDataException();
        }

        private static string ParseTemperature(string content)
        {
            content = Regex.Escape(content);
            string REG_EXP = "^(.*?)</frameset>(.*?)$";
            Regex regex = new Regex(REG_EXP, RegexOptions.IgnoreCase);
            Match match = regex.Match(content);
            if (match.Success)
            {
                string settings = match.Groups[2].Value;
                int tempStart = settings.IndexOf(',') + 1;
                int tempLength = settings.IndexOf(',', tempStart) - tempStart;
                string temperature = settings.Substring(tempStart, tempLength);
                return Regex.Unescape(temperature);
            }
            throw new InvalidDataException();
        }

        private static ObservableCollection<Port> ParseSwitchersList(string content)
        {
            bool series_16 = !content.Contains("\"./?chn=\"");
            string SW_LINK_START = "<a href=\"./?sw=";
            string SW_LINK_CLOSE = series_16 ? "a>" : "font>]";
            string REG_EXP = series_16 ? "^<a href=\"./\\?sw=(\\d)-(\\d)\">turn(.*?)</$" : "^<a href=\"./\\?sw=(\\d)-(\\d)\">turn(.*?)\">(.*?)</$";
            int startIndex, closeIndex = 0;
            ObservableCollection<Port> SwitchersList = new ObservableCollection<Port>();
            Regex regex = new Regex(REG_EXP, RegexOptions.IgnoreCase);
            while ((startIndex = content.IndexOf(SW_LINK_START, closeIndex)) != -1)
            {
                closeIndex = content.IndexOf(SW_LINK_CLOSE, startIndex);
                string switcher = content.Substring(startIndex, closeIndex - startIndex);

                Match match = regex.Match(switcher);
                if (match.Success)
                {
                    int port = int.Parse(match.Groups[1].Value);
                    bool value = int.Parse(match.Groups[2].Value) == 0;
                    string name = string.Empty;
                    if (match.Groups.Count >= 4)
                    {
                        name = match.Groups[4].Value;
                    }
                    if (name.Length == 0)
                    {
                        name = "Порт " + port;
                    }
                    Debug.WriteLine("port: " + port + " value: " + value);
                    SwitchersList.Add(new Port(name, port, value, series_16));
                }
                else
                {
                    Debug.WriteLine("Couldn't parse: [" + switcher + "]");
                }
            }
            return SwitchersList;
        }

        public static async Task<string> ReadUrlAsync(string request_url)
        {
            using (var client = new HttpClient())
            {
                request_url += "&cache=" + Guid.NewGuid().ToString();
                var data = await client.GetStringAsync(request_url);
                return data;
            }
        }

        public static JsonAnswer ReadJson(string value) {
            JsonAnswer answer = JsonConvert.DeserializeObject<JsonAnswer>(value);
            return answer;
        }
    }

    public class JsonAnswer
    {
        public JsonAnswer() { }

        public string type { get; set; }
        public string fwv { get; set; }
        public string id { get; set; }
        public int[] outs { get; set; }
        public int[] ins { get; set; }
        public float t { get; set; }
        public int wdr { get; set; }
    }

    public class Monster
    {
        public Monster() { }
        public Monster(string name, string url, string password)
        {
            MonsterName = name;
            MonsterUrl = url;
            MonsterPassword = password;
            PortsList = new ObservableCollection<Port>();
        }
        public string MonsterName { get; set; }
        public string MonsterUrl { get; set; }
        public string MonsterPassword { get; set; }
        public ObservableCollection<Port> PortsList { get; set; }
    }

    public class Port
    {
        public Port() { }
        public Port(string portName, int portIndex, bool portState, bool portImmutable)
        {
            PortName = portName;
            PortIndex = portIndex;
            PortState = portState;
            PortImmutable = portImmutable;
        }
        public string PortName { get; set; }
        public int PortIndex { get; set; }
        public bool PortState { get; set; }
        public bool PortImmutable { get; set; }
        public override string ToString()
        {
            return PortName;
        }
    }
}
