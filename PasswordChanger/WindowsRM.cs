namespace PasswordChanger
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Security;
    using System.Text;

    /// <summary>
    /// Uses Windows RemoteManagement to connect and execute PS scripts
    /// </summary>
    /// <remarks>
    /// ([adsi]“WinNT://PC-NAME/USERNAME”).SetPassword(PASSWORD) - Note for future reference. PS command to change password on local machine
    /// </remarks>
    class WindowsRM
    {
        public static string RunScript(string script, string server, string username, string password)
        {
            Runspace remoteRunspace = null;
            openRunspace("http://" + server + ":5985/wsman", "http://schemas.microsoft.com/powershell/Microsoft.PowerShell", username, password, ref remoteRunspace);

            StringBuilder stringBuilder = new StringBuilder();
            using (PowerShell powershell = PowerShell.Create())
            {
                powershell.Runspace = remoteRunspace;
                powershell.AddCommand(script);
                powershell.Invoke();
                ICollection<PSObject> results = powershell.Invoke();
                remoteRunspace.Close();
                foreach (PSObject obj in results)
                {
                    stringBuilder.AppendLine(obj.ToString());
                }
            }
            return stringBuilder.ToString();
        }

        public static void openRunspace(string uri, string schema, string username, string livePass, ref Runspace remoteRunspace)
        {
            System.Security.SecureString password = new System.Security.SecureString();
            foreach (char c in livePass.ToCharArray())
            {
                password.AppendChar(c);
            }
            PSCredential psc = new PSCredential(username, password);
            WSManConnectionInfo rri = new WSManConnectionInfo(new Uri(uri), schema, psc);
            rri.AuthenticationMechanism = AuthenticationMechanism.Kerberos;
            rri.ProxyAuthentication = AuthenticationMechanism.Negotiate;
            remoteRunspace = RunspaceFactory.CreateRunspace(rri);
            remoteRunspace.Open();
        }
    }
}
