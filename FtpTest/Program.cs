using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FtpTest
{
    class Program
    {
        static void Main(string[] args)
        {
            List<SimpleFTP.Account> accounts = new List<SimpleFTP.Account>()
            {
                new SimpleFTP.Account("foo", "bar", @"D:\tmp")
            };

            SimpleFTP.Server server = new SimpleFTP.Server(
                new byte[] { 127, 0, 0, 1 }, 21, accounts);

            server.Logger += new EventHandler<SimpleFTP.Server.FtpEventArgs>(Logout);
            server.Start();

            System.Console.ReadKey();

            server.Stop();
        }

        static void Logout(object sender, SimpleFTP.Server.FtpEventArgs e)
        {
            System.Console.WriteLine(e.Log);
        }
    }
}
