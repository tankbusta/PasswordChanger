namespace PasswordChanger
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using Renci.SshNet;
    using log4net; //need to implement log4net to log messages

    /// <summary>
    /// Provides a wrapper around SSH.NET to perform linux commands and tasks 
    /// </summary>
    class Linux
    {
        protected internal SshClient ssh_client;
        public static bool isConnected = false;

        private string Username { get; set; }
        private string Password { get; set; }

        /// <summary>
        /// Contains exit codes for a command 
        /// </summary>
        public enum ExitStatus : int
        {
            SUCCESS = 0,
            BADCMD = 127
        }

        /// <summary>
        /// Constructor for Linux SSH Class
        /// </summary>
        /// <param name="strUsername"></param>
        /// <param name="strPassword"></param>
        /// <param name="strHost"></param>
        public Linux(string strUsername, string strPassword, string strHost)
        {
            Username = strUsername;
            Password = strPassword;

            try
            {
                ssh_client = new SshClient(Password, Username, strPassword);
                ssh_client.KeepAliveInterval = TimeSpan.FromSeconds(60);
                ssh_client.Connect();

                if (ssh_client.IsConnected)
                {
                    isConnected = true;
                }
            }
            catch (Exception ex)
            {
                //todo
                Disconnect();
            }
        }

        /// <summary>
        /// Execute's an SSH command against an active SSH session and returns a string if successful
        /// </summary>
        /// <remarks>
        ///  Exit Status
        ///     0 - Good
        ///     127 - Bad
        /// </remarks>
        /// <param name="cmd"></param>
        protected internal string ExecuteComand(string cmd)
        {
            try
            {
                if (ssh_client.IsConnected)
                {
                    var command = ssh_client.RunCommand(cmd);
                    if (command.ExitStatus == 127)
                    {
                        /// This is a bad command!
                        throw new Exception("Invalid command was executed");
                    }
                    return command.Result;
                }
            }
            catch (Exception ex)
            {
                //TODO handle exception code
                Disconnect();
            }
            return "";
        }

        /// <summary>
        /// Makes a user have root priv's - WORK IN PROGRESS
        /// </summary>
        protected internal void Sudo()
        {
            try
            {
                using (var stream = ssh_client.CreateShellStream("", 0, 0, 0, 0, 0))
                {
                    System.Threading.Thread.Sleep(1000 * 3); //хак работы...
                    stream.WriteLine("sudo su -");
                    stream.Expect("sudo su -");
                    stream.Expect(
                        new ExpectAction("[sudo] password for " + Username + ": ", (s) =>
                            {
                                System.Threading.Thread.Sleep(1000 * 3);
                                stream.WriteLine(Password);
                                System.Threading.Thread.Sleep(1000 * 3);
                                Console.WriteLine(s);
                                Console.ReadLine();
                            })
                    );
                }
            }
            catch (Exception ex)
            {
                //todo
                Disconnect();
            }
        }
        

        /// <summary>
        /// Executes command's from a list of strings
        /// </summary>
        /// <param name="cmds"></param>
        protected internal void ExecuteCommands(List<String> cmds)
        {
            //WIP
            cmds.ForEach(delegate(String command)
            {
                var toRunCommand = ExecuteComand(command);
                Console.WriteLine(toRunCommand);
            });
        }

        /// <summary>
        /// Disconnects from SSH server and clean's up resources
        /// </summary>
        protected internal void Disconnect()
        {
            ssh_client.Disconnect();
            ssh_client.Dispose();
        }
    }
}
