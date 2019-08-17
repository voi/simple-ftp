using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleFTP
{
    class LoginAccount
    {
        public enum LoginState
        {
            NoLogin = 0,
            Queried,
            Passed
        }

        IDictionary<string, Account> accounts_;

        public Account User { get; private set; }
        public LoginState State { get; private set; }
        public string CurrentPath { get; private set; }
        public string RemoteCurrentPath
        {
            get
            {
                return this.CurrentPath
                    .Replace(this.User?.RootPath, "")
                    .Replace(Path.DirectorySeparatorChar, '/');
            }
        }

        public LoginAccount(IDictionary<string, Account> accounts)
        {
            this.accounts_ = accounts;
            this.CurrentPath = String.Empty;
            this.State = LoginState.NoLogin;
        }

        public bool CertaineUser(string name)
        {
            if (!this.accounts_.ContainsKey(name))
            {
                return false;
            }

            this.User = this.accounts_[name];
            this.State = LoginState.Queried;

            return true;
        }

        public bool CertainePassword(string password)
        {
            if (this.User == null)
            {
                return false;
            }

            if (this.User.Password != password)
            {
                return false;
            }

            this.State = LoginState.Passed;
            this.CurrentPath = this.User.RootPath;

            return true;
        }

        public bool ChangeDirectory(string param)
        {
            string cd = this.LocalizePath(param);

            if (String.IsNullOrEmpty(cd) || !Directory.Exists(cd))
            {
                return false;
            }

            this.CurrentPath = cd;

            return true;
        }

        public string LocalizePath(string param)
        {
            if (this.User == null)
            {
                return String.Empty;
            }

            string path_part = param.TrimStart('/', '\\');

            if (Path.IsPathRooted(path_part))
            {
                return String.Empty;
            }

            // ROOTより上の階層でないことを確認する
            string cd = Path.GetFullPath(Path.Combine(this.CurrentPath,
                    path_part.Replace('/', Path.DirectorySeparatorChar)));

            if (cd.IndexOf(this.User.RootPath) < 0)
            {
                return String.Empty;
            }

            return cd;
        }
    }
}
