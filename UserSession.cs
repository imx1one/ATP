using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATP
{
    public static class UserSession
    {
        public static string username { get; private set; }
        public static string role { get; private set; }
        public static bool isAdmin => role == "admin";
        public static bool login(string login, string password)
        {
            if (login == "admin" && password == "admin123")
            {
                username = login; role = "admin";
                return true;
            }
            if (login == "worker" && password == "worker123")
            {
                username = login; role = "worker";
                return true;
            }
            return false;
        }
        public static void logout()
        {
            username = null; role = null;
        }
    }
}
