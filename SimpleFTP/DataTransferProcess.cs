using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace SimpleFTP
{
    class DataTransferProcess
    {
        public bool IsPassive { get; set; }
        public bool IsBinary { get; set; }
        public IPEndPoint Address { get; private set; }

        TcpListener passiveListener_;
        TcpClient passiveClient_;
        ManualResetEvent passiveAccepted_ = new ManualResetEvent(false);

        public DataTransferProcess()
        {
            this.IsPassive = false;
            this.IsBinary = false;
            this.Address = null;
        }

        public void StartPassive()
        {
            if(this.Address == null)
            {
                return;
            }

            if(this.IsPassive)
            {
                this.StopPassive();

                this.passiveListener_ = new TcpListener(this.Address);
                this.passiveListener_.Start();
                this.passiveListener_.BeginAcceptTcpClient(new AsyncCallback((result) => {
                    DataTransferProcess owner = result.AsyncState as DataTransferProcess;

                    try
                    {
                        if(result.IsCompleted /* && result.CompletedSynchronously */)
                        {
                            owner.passiveClient_ = owner.passiveListener_?.EndAcceptTcpClient(result);
                            owner.passiveAccepted_.Set();
                        }
                    }
                    catch(ObjectDisposedException)
                    {
                        owner.passiveClient_ = null;
                    }
                }), this);
            }
        }

        public void StopPassive()
        {
            this.passiveAccepted_.Reset();

            if (this.passiveClient_ != null)
            {
                this.passiveClient_.Close();
                this.passiveClient_ = null;
            }

            if (this.passiveListener_ != null)
            {
                this.passiveListener_.Stop();
                this.passiveListener_ = null;
            }
        }

        public bool SetPort(string host_port)
        {
            if(String.IsNullOrEmpty(host_port))
            {
                return false;
            }

            byte[] tokens = host_port
                .Split(new char[] { ',' })
                .Select((token) =>
                {
                    byte number;

                    if (Byte.TryParse(token, out number))
                    {
                        return number;
                    }

                    return (byte)0;
                })
                .ToArray();

            if(tokens.Length != 6)
            {
                return false;
            }

            this.Address = new IPEndPoint(
                new IPAddress(tokens.Take(4).Cast<byte>().ToArray()),
                (((int)tokens[4]) << 8) + (int)tokens[5]);

            return true;
        }

        public bool Send(string content)
        {
            return this.HandleDataStream(
                (client) => {
                    return this.Send(client, content);
                });
        }

        public bool Delete(string entry_path, bool isDirectory)
        {
            try
            {
                if(isDirectory)
                {
                    if(!Directory.Exists(entry_path))
                    {
                        return false;
                    }

                    Directory.Delete(entry_path);
                }
                else
                {
                    if(!File.Exists(entry_path))
                    {
                        return false;
                    }

                    File.Delete(entry_path);
                }

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool Retrieve(string file_path)
        {
            if(!File.Exists(file_path))
            {
                return false;
            }

            if (this.Address == null)
            {
                return false;
            }

            return this.HandleDataStream(
                (client) => {
                    return this.Retrieve(client, file_path);
                });
        }

        public bool Store(string file_path)
        {
            if (this.Address == null)
            {
                return false;
            }

            return this.HandleDataStream(
                (client) => {
                    return this.StoreEx(client, file_path, FileMode.Create);
                });
        }

        public bool Append(string file_path)
        {
            if (!File.Exists(file_path))
            {
                return false;
            }

            if (this.Address == null)
            {
                return false;
            }

            return this.HandleDataStream(
                (client) => {
                    return this.StoreEx(client, file_path, FileMode.Append);
                });
        }

        private bool HandleDataStream(Func<TcpClient, bool> handler)
        {
            try
            {
                bool isSuccess = false;

                if (this.IsPassive)
                {
                    if(this.passiveListener_ == null)
                    {
                        return false;
                    }

                    this.passiveAccepted_.WaitOne(30000);

                    if(this.passiveClient_ == null)
                    {
                        return false;
                    }

                    isSuccess = handler(this.passiveClient_);
                }
                else
                {
                    using (TcpClient client = new TcpClient())
                    {
                        client.Connect(this.Address);

                        isSuccess = handler(client);
                    }
                }

                return isSuccess;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private bool Send(TcpClient client, string content)
        {
            try
            {
                using (NetworkStream dtp_stream = client.GetStream())
                {
                    byte[] buffer = Encoding.GetEncoding(932).GetBytes(content);

                    dtp_stream.Write(buffer, 0, buffer.Length);
                    dtp_stream.Flush();
                }

                return true;
            }
            catch(Exception e)
            {
                return false;
            }
        }

        private bool Retrieve(TcpClient client, string file_path)
        {
            try
            {
                using (BinaryWriter dtp_stream = new BinaryWriter(client.GetStream()))
                using (FileStream file_stream = new FileStream(file_path, FileMode.Open, FileAccess.Read))
                {
                    byte[] content = new byte[file_stream.Length];

                    file_stream.Read(content, 0, content.Length);

                    dtp_stream.Write(content);
                    dtp_stream.Flush();
                }

                return true;
            }
            catch(Exception e)
            {
                return false;
            }
        }

        private bool StoreEx(TcpClient client, string file_path, FileMode mode)
        {
            try
            {
                using (NetworkStream dtp_stream = client.GetStream())
                using (FileStream file_stream = new FileStream(file_path, mode, FileAccess.Write))
                {
                    // buffer is 10KB
                    int count = 0;
                    byte[] buffer = new byte[10240];

                    do
                    {
                        count = dtp_stream.Read(buffer, 0, buffer.Length);

                        file_stream.Write(buffer, 0, count);
                    }
                    while (count > 0);

                    file_stream.Flush();
                }

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
