namespace PasswordChanger
{
    using System;
    using System.Configuration;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    class Program
    {
        static void Main(string[] args)
        {
            Linux lnx = new Linux(ConfigurationManager.AppSettings["sshUsername"], ConfigurationManager.AppSettings["sshPassword"], ConfigurationManager.AppSettings["sshHostname"]);

            ///multiple commands
            //List<String> toRun = new List<string>();

            //string commands = ConfigurationManager.AppSettings["sshCommands"];

            //foreach (string word in commands.Split(';'))
            //{
            //    Console.WriteLine(lnx.ExecuteComand(word));
            //}


            Console.ReadLine();
        }
    }
}
