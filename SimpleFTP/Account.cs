using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SimpleFTP
{
    public class Account
    {
        public string Name { get; private set; }
        public string Password { get; private set; }
        public string RootPath { get; private set; }

        public Account(string name, string password, string rootPath)
        {
            this.Name = name;
            this.Password = password;
            this.RootPath = rootPath;
        }
    }
}
