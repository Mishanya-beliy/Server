using System.Collections.Generic;

namespace Server
{
    class Authorizating
    {
        private const string successfulRegistration = "Registration is successful.";
        private const string usedLogin = "Login is already in use.";
        
        public static void Registration(int idClient, string login, string password)
        {
            if (Registered(registeredLogins, login))
                Send.Registration(idClient, usedLogin);
            else
            {
                registeredLogins.Add(login);
                registeredPasswords.Add(password);
                Send.Registration(idClient, successfulRegistration);
            }
        }

        private const string successfulAuthorization = "Authorization is successful.";
        private const string failedPassword = "Invalid password.";
        private const string failedLogin = "Invalid login.";

        public static void Authorization(int idClient, string login, string password)
        {
            if (Registered(registeredLogins, login))
                if (Registered(registeredPasswords, password))
                {
                    Server.clients[idClient].Authorizated(login);
                    Send.Authorization(idClient, successfulAuthorization);
                }
                else
                    Send.Authorization(idClient, failedPassword);
            else
                Send.Authorization(idClient, failedLogin);
        }

        private static List<string> registeredLogins = new List<string>();
        private static List<string> registeredPasswords = new List<string>();
        private static bool Registered(List<string> list, string parametr)
        {
            if (list.Contains(parametr))
                return true;
            else
                return false;
        }        
    }
}
