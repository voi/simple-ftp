using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SimpleFTP
{
    public class Server
    {
        public class FtpEventArgs : EventArgs
        {
            public string Log { get; private set; }

            public FtpEventArgs(string log)
            {
                this.Log = log;
            }
        }

        class Listener
        {
            IPEndPoint address_;
            TcpListener listener_;

            public bool IsRunning { get { return (this.listener_ != null); } }

            public Listener(byte[] ipv4, int port)
            {
                var ip = ipv4 ?? new byte[] { 127, 0, 0, 1 };

                this.address_ = new IPEndPoint(new IPAddress(ip.Take(4).ToArray()), port);
            }

            public void Start()
            {
                if(this.listener_ != null)
                {
                    this.Stop();
                }

                this.listener_ = new TcpListener(this.address_);
                this.listener_.Start();
            }

            public void Stop()
            {
                if(this.listener_ != null)
                {
                    this.listener_.Stop();
                    this.listener_ = null;
                }
            }

            public TcpClient Accept()
            {
                if(this.listener_ == null)
                {
                    return null;
                }

                return this.listener_.AcceptTcpClient();
            }
        }

        IDictionary<string, Account> accounts_;
        bool CanRun { get; set; }
        Listener listener_;
        Thread listrenerThread_;

        public event EventHandler<FtpEventArgs> Logger;

        public Server(byte[] ipv4, int port = 21, IList<Account> accounts = null)
        {
            this.CanRun = false;
            this.listener_ = new Listener(ipv4, port);
            this.accounts_ = (accounts ?? new List<Account>())
                .ToDictionary((account) => { return account.Name; });
        }

        public void Start()
        {
            if(this.listrenerThread_ != null)
            {
                this.Stop();
            }

            this.CanRun = true;

            this.listrenerThread_ = new Thread(new ThreadStart(this.Listen));
            this.listrenerThread_.IsBackground = true;
            this.listrenerThread_.Start();
        }

        public void Stop()
        {
            if(this.listrenerThread_ != null)
            {
                this.CanRun = false;
                this.listener_.Stop();

                if (!this.listrenerThread_.Join(10000))
                {
                    this.listrenerThread_.Abort();
                }

                this.listrenerThread_ = null;
            }
        }

        private void OnLogger(string log)
        {
            this.OnLogger(this, new FtpEventArgs(log));
        }

        private void OnLogger(object sender, FtpEventArgs e)
        {
            if(this.Logger != null)
            {
                this.Logger(this, e);
            }
        }

        private void Listen()
        {
            List<ProtocolInterpreter> pilist = new List<ProtocolInterpreter>();
            EventHandler<FtpEventArgs> logger = new EventHandler<FtpEventArgs>(this.OnLogger);

            this.listener_.Start();

            while (this.CanRun)
            {
                TcpClient client = null;

                try
                {
                    client = this.listener_.Accept();
                }
                catch (Exception e)
                {
                    this.OnLogger(e.Message);
                }

                if (!this.CanRun)
                {
                    break;
                }

                //
                var pi = new ProtocolInterpreter(client, this.accounts_);

                pi.Logger += logger;
                pi.Start();
                pilist.Add(pi);
            }

            this.listener_.Stop();

            foreach(var pi in pilist )
            {
                pi.Stop();
            }
        }
    }
}
