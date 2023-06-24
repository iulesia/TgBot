using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Npgsql;
using Model;
using static System.Net.WebRequestMethods;

namespace BotCons
{
    public class Constants
    {
        public static string address = "https://localhost:7092";
        public static string Connect = "Host=localhost;Username=postgres;Password=207234;Database=postgres";

    }
}
