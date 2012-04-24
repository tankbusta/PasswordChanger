namespace PasswordChanger
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using Renci.SshNet;
    ///using log4net; //need to implement log4net to log messages

    /// <summary>
    /// Provides a wrapper around SSH.NET to perform linux commands and tasks 
    /// </summary>
    /// <remarks>
    ///     -It might be worth moving this to a thread that runs with shellstream code so you can keep a persistant root shell should you sudo.
    /// </remarks>
    class Linux
    {
        KeyValuePair<Renci.SshNet.Common.TerminalModes, uint> termkvp = new KeyValuePair<Renci.SshNet.Common.TerminalModes, uint>(Renci.SshNet.Common.TerminalModes.ECHO, 53);

        protected internal SshClient ssh_client;
        public static bool isConnected = false;
        public static bool usingKeyAuth = false;

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
                ssh_client = new SshClient(strHost, Username, Password);
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
        /// Constructor that takes SSH Key authentication
        /// </summary>
        /// <param name="sshKey"></param>
        public Linux(string sshKey, string strHost, string strUsername, string strPassph)
        {
            Username = strUsername;

            try
            {
                ssh_client = new SshClient(strHost, strUsername, new PrivateKeyFile(File.OpenRead(sshKey), strPassph));
                ssh_client.KeepAliveInterval = TimeSpan.FromSeconds(60);
                ssh_client.Connect();

                if (ssh_client.IsConnected)
                {
                    isConnected = true;
                    usingKeyAuth = true;
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
        /// Makes a user have root priv's
        /// </summary>
        /// <remarks>
        ///     This gains root access - now I got to get some persistancy into it... (the input doesnt work yet).
        /// </remarks>
        protected internal void ExecuteRootCommand(string cmd)
        {
            try
            {
                var shellStream = ssh_client.CreateShellStream("xterm", 80, 24, 800, 600, 1024, termkvp);

                var rep = shellStream.Expect("$");

                shellStream.WriteLine("/bin/su");
                rep = shellStream.Expect(":");

                shellStream.WriteLine(Password);
                rep = shellStream.Expect(new System.Text.RegularExpressions.Regex(@"[$#]"));

                if (rep.Contains("Sorry"))
                {
                    throw new Exception("Unable to achieve root access");
                }
                rep = shellStream.Expect("#");
                shellStream.Close(); //this might break it at this point in time
            }
            catch (Exception ex)
            {
                //todo
                Disconnect();
            }
        }

        /// <summary>
        /// This changes the password for the current logged in user
        /// </summary>
        /// <remarks>
        /// Currently does not work with keybased auth
        /// </remarks>
        protected internal void ChangePassword(string newPassword)
        {
            if (usingKeyAuth)
            {
                throw new Exception("Unsupported with key authentication");
            }

            try
            {
                var shellStream = ssh_client.CreateShellStream("xterm", 80, 24, 800, 600, 1024, termkvp);
                var rep = shellStream.Expect("$");

                shellStream.WriteLine("passwd");
                rep = shellStream.Expect(":"); //expect to ask for the password
                shellStream.WriteLine(Password); //send current password for logged in user

                rep = shellStream.Expect(":"); //setting us up for the new password
                if (rep.Contains("New")) // does it ask us for the new password
                {
                    shellStream.WriteLine(newPassword);
                    rep = shellStream.Expect("Retype");
                    if (rep.Contains("Retype")) // it should ask us to retype the password
                    {
                        shellStream.WriteLine(newPassword);
                    }
                    else
                    {
                        throw new Exception("Could not detect password prompt asking us to repeat password.");
                    }
                }
                else
                {
                    throw new Exception("Could not detect password prompt asking us for new password.");
                }
                rep = shellStream.Expect("#");
            }
            catch (Exception ex)
            {
                //do some logging stuff
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
