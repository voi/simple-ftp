using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;
using System.Globalization;

namespace SimpleFTP
{
    class ProtocolInterpreter
    {
        class Replyer
        {
            StreamWriter writer_;

            public Replyer(StreamWriter writer)
            {
                this.writer_ = writer;
            }

            public string Send(ReplyCode reply)
            {
                return this.Send(reply.ToString());
            }

            public string Send(ReplyCode reply, string addMessage)
            {
                return this.Send(String.Format("{0} {1}", reply.Code.ToString(), addMessage));
            }

            private string Send(string response)
            {
                try
                {
                    writer_.WriteLine(response);
                }
                catch
                {
                }

                return response;
            }
        }

        class Command
        {
            public string Name { get; private set; }
            public string Param { get; private set; }

            public Command(string commandline)
            {
                string[] tokens = commandline.Split(new char[] { ' ', '\t' }, 2);

                this.Name = tokens[0].Trim();
                this.Param = tokens.Length > 1 ? tokens[1].Trim() : String.Empty;
            }

            public string GetOption()
            {
                var match = Regex.Match(this.Param, @"(-\w+)*");

                return (match.Success ? match.Value.Trim() : String.Empty);
            }

            public string GetParam()
            {
                return Regex.Replace(this.Param, @"(-\w+)*", "").Trim();
            }
        }

        LoginAccount currentAccount_;
        DataTransferProcess currentDTP_;
        TcpClient connection_;
        Thread interpreterThread_;

        public event EventHandler<Server.FtpEventArgs> Logger;

        public ProtocolInterpreter(TcpClient clientConn, IDictionary<string, Account> accounts)
        {
            this.connection_ = clientConn;
            this.currentDTP_ = new DataTransferProcess();
            this.currentAccount_ = new LoginAccount(
                accounts ?? new Dictionary<string, Account>());
        }

        public void Start()
        {
            if(this.connection_ == null)
            {
                return;
            }

            if (this.interpreterThread_ != null)
            {
                this.Stop();
            }

            this.interpreterThread_ = new Thread(new ThreadStart(this.Run));
            this.interpreterThread_.IsBackground = true;
            this.interpreterThread_.Start();
        }

        public void Stop()
        {
            if(this.interpreterThread_ != null)
            {
                this.connection_.Close();
                this.connection_ = null;

                if (!this.interpreterThread_.Join(5000))
                {
                    this.interpreterThread_.Abort();
                }

                this.interpreterThread_ = null;
            }
        }

        private void OnLogger(string log)
        {
            if(this.Logger != null)
            {
                this.Logger(this, new Server.FtpEventArgs(log));
            }
        }

        private void Run()
        {
            DataTransferProcess dtp = new DataTransferProcess();

            StreamWriter writer = new StreamWriter(this.connection_.GetStream());
            StreamReader reader = new StreamReader(this.connection_.GetStream(), Encoding.GetEncoding(932));

            writer.AutoFlush = true;

            var replyer = new Replyer(writer);

            this.OnLogger(replyer.Send(ReplyCode._220));

            while (true)
            {
                string commandline;

                try
                {
                    commandline = reader.ReadLine();
                }
                catch(Exception e)
                {
                    this.OnLogger(e.Message);
                    break;
                }

                if (String.IsNullOrEmpty(commandline))
                {
                    break;
                }

                this.OnLogger(commandline);

                // RFC959
                var isQuit = false;
                var command = new Command(commandline);

                switch(this.currentAccount_.State)
                {
                    case LoginAccount.LoginState.NoLogin:
                        switch (command.Name)
                        {
                            case "USER":    // 認証するユーザー名
                                this.DoUSER(command, replyer);
                                break;

                            default:
                                this.OnLogger(replyer.Send(ReplyCode._502));
                                break;
                        }
                        break;

                    case LoginAccount.LoginState.Queried:
                        switch (command.Name)
                        {
                            case "PASS":    // 認証パスワード。
                                this.DoPASS(command, replyer);
                                break;

                            default:
                                this.OnLogger(replyer.Send(ReplyCode._502));
                                break;
                        }
                        break;

                    case LoginAccount.LoginState.Passed:
                        switch (command.Name)
                        {
                            case "APPE":    // 引数に示したファイルに対して追記する。
                                this.DoAPPE(command, replyer);
                                break;

                            case "CDUP":    // 親ディレクトリに移動する。
                                this.DoCDUP(command, replyer);
                                break;

                            case "CWD":     // 作業ディレクトリの変更。引数は移動するディレクトリ。
                                this.DoCWD(command, replyer);
                                break;

                            case "DELE":    // ファイルを削除する。引数は削除するファイル。
                                this.DoDELE(command, replyer);
                                break;

                            case "HELP":    // コマンドの一覧。引数を指定するとより詳しいコマンド情報を返す。
                                replyer.Send(ReplyCode._202);
                                break;

                            case "LIST":    // 引数に指定したファイルの情報やディレクトリの一覧。指定しない場合、現在のディレクトリの情報を一覧。
                                this.DoLIST(command, replyer);
                                break;

                            case "XMKD":
                            case "MKD":     // 引数に指定した名前のディレクトリを作成する。
                                this.DoMKD(command, replyer);
                                break;

                            case "NLST":    // 引数に指定したディレクトリのファイル一覧を返す。
                                this.DoNLST(command, replyer);
                                break;

                            case "NOOP":    // 何もしない。接続維持のためダミーパケットとして使われることがほとんど。
                                replyer.Send(ReplyCode._200);
                                break;

                            case "MODE":    // 転送モードの設定（ストリーム、ブロック、圧縮）。
                                replyer.Send(ReplyCode._202);
                                break;

                            case "PASS":    // 認証パスワード。
                                this.DoPASS(command, replyer);
                                break;

                            case "PASV":    // パッシブモードに移行する。
                                this.DoPASV(command, replyer);
                                break;

                            case "PORT":    // サーバが接続すべきポートとアドレスを指定する。
                                this.DoPORT(command, replyer);
                                break;

                            case "XPWD":
                            case "PWD":     // 作業ディレクトリを取得する。
                                this.DoPWD(command, replyer);
                                break;

                            case "QUIT":    // 接続を終了する。
                                replyer.Send(ReplyCode._221);

                                isQuit = true;
                                break;

                            case "RETR":    // リモートファイルをダウンロード（Retrieve）する。
                                this.DoRETR(command, replyer);
                                break;

                            case "XRMD":
                            case "RMD":     // 引数に指定したディレクトリを削除する。
                                this.DoRMD(command, replyer);
                                break;

                            case "STOR":    // ファイルをアップロード（Stor）する。
                                this.DoSTOR(command, replyer);
                                break;

                            case "STOU":    // ファイル名が重複しないようにファイルをアップロードする。
                                this.DoSTOU(command, replyer);
                                break;

                            case "TYPE":    // 転送モードを設定する（アスキーモード、バイナリモード）。
                                this.DoTYPE(command, replyer);
                                break;

                            case "USER":    // 認証するユーザー名
                                this.DoUSER(command, replyer);
                                break;

                            case "ABOR":    // ファイルの転送を中止する。
                            case "ACCT":    // アカウント情報。引数はユーザアカウントを示す文字列。
                            case "ALLO":    // ファイルを受け取るために十分なディスクスペースを割り当てる。引数は予約するサイズ。
                            case "REIN":    // 接続を再初期化する。
                            case "RNFR":    // 引数に指定した名前のファイル（ディレクトリ）をリネームする。
                            case "RNTO":    // 引数に指定した名前のファイル（ディレクトリ）にリネームする。
                            case "SITE":    // RFCで定義されていないようなリモートサーバ特有のコマンドを送信する。
                            case "SMNT":    // ファイル構造をマウントする
                            case "STRU":    // 転送するファイルの構造を設定する。
                            case "STAT":    // 現在の状態を取得する。
                            case "SYST":    // システムの種別を返す。
                                this.OnLogger(replyer.Send(ReplyCode._502));
                                break;

                            default:
                                this.OnLogger(replyer.Send(ReplyCode._502));
                                break;
                        }
                        break;

                    default:
                        this.OnLogger(replyer.Send(ReplyCode._502));
                        break;
                }

                if(isQuit)
                {
                    break;
                }
            }

            this.currentDTP_.StopPassive();
        }

        private void DoPASV(Command command, Replyer replyer)
        {
            string host_port = MakeHostPortPart(
                this.connection_.Client.LocalEndPoint, FindUnusedPort());

            this.currentDTP_.SetPort(host_port);
            this.currentDTP_.IsPassive = true;
            this.currentDTP_.StartPassive();

            this.OnLogger(replyer.Send(
                ReplyCode._227, String.Format("Entering Passive Mode. ({0})", host_port)));
        }

        private void DoTYPE(Command command, Replyer replyer)
        {
            string[] tokens = command.Param.Split(' ');

            switch(tokens[0])
            {
                case "A":
                    this.currentDTP_.IsBinary = false;
                    this.OnLogger(replyer.Send(ReplyCode._200));
                    break;

                case "I":
                    this.currentDTP_.IsBinary = false;
                    this.OnLogger(replyer.Send(ReplyCode._200));
                    break;

                case "E":
                case "L":
                    this.OnLogger(replyer.Send(ReplyCode._202));
                    break;

                default:
                    this.OnLogger(replyer.Send(ReplyCode._501));
                    break;
            }
        }

        private void DoLIST(Command command, Replyer replyer)
        {
            // Ignore LIST option `-al`
            string path = this.currentAccount_.LocalizePath(command.GetParam());

            if(String.IsNullOrEmpty(path))
            {
                this.OnLogger(replyer.Send(ReplyCode._451));

                return;
            }

            StringBuilder nlst = new StringBuilder();
            string host_name = Dns.GetHostName();
            DateTimeFormatInfo dateTimeFormat = new CultureInfo("en-US", true).DateTimeFormat;

            // [permission]<SP>[?]<SP>[user]<SP>[datetime]<SP>[name]
            foreach (var entry in Directory.GetFileSystemEntries(path))
            {
                if (Directory.Exists(entry))
                {
                    DirectoryInfo info = new DirectoryInfo(entry);

                    nlst.AppendLine(
                        String.Format("drwxrw-rw-     0 owner {0,15} {1} {2,8} {3}",
                        0,
                        dateTimeFormat.GetAbbreviatedMonthName(info.LastWriteTime.Month),
                        info.LastWriteTime.ToString("d HH:MM"),
                        info.Name));
                }
                else
                {
                    FileInfo info = new FileInfo(entry);

                    nlst.AppendLine(
                        String.Format("-rwxrw-rw-     0 owner {0,15} {1} {2,8} {3}",
                        info.Length,
                        dateTimeFormat.GetAbbreviatedMonthName(info.LastWriteTime.Month),
                        info.LastWriteTime.ToString("d HH:MM"),
                        info.Name));
                }
            }
#if MLST
            // MLST format
            // [name];[type];[size];[datetime];[user];[permission]
            foreach (var entry in Directory.GetFileSystemEntries(path))
            {
                if (Directory.Exists(entry))
                {
                    DirectoryInfo info = new DirectoryInfo(entry);

                    nlst.AppendLine(
                        String.Format("{0};D;{1};{2};1;\"owner\" [0];\"\" [0];rwxrw-rw-;1",
                        info.Name, 0,
                        info.LastWriteTime.ToString("yyyy-MM-ddTHH:MM:ss.fffZ")
                        ));
                }
                else
                {
                    FileInfo info = new FileInfo(entry);

                    nlst.AppendLine(
                        String.Format("{0};-;{1};{2};1;\"owner\" [0];\"\" [0];rwxrw-rw-;1",
                        info.Name, info.Length,
                        info.LastWriteTime.ToString("yyyy-MM-ddTHH:MM:ss.fffZ")
                        ));
                }
            }
#endif
            this.NotifyType(replyer);

            if (this.currentDTP_.Send(nlst.ToString()))
            {
                this.OnLogger(replyer.Send(ReplyCode._226));
            }
            else
            {
                this.OnLogger(replyer.Send(ReplyCode._451));
            }
        }

        private void DoNLST(Command command, Replyer replyer)
        {
            string path = this.currentAccount_.LocalizePath(command.GetParam());

            if (String.IsNullOrEmpty(path))
            {
                this.OnLogger(replyer.Send(ReplyCode._451));

                return;
            }

            StringBuilder nlst = new StringBuilder();

            foreach (var entry in Directory.GetFileSystemEntries(path))
            {
                nlst.AppendLine(Path.GetFileName(entry));
            }

            this.NotifyType(replyer);

            if(this.currentDTP_.Send(nlst.ToString()))
            {
                this.OnLogger(replyer.Send(ReplyCode._250));
            }
            else
            {
                this.OnLogger(replyer.Send(ReplyCode._451));
            }
        }

        private void DoRMD(Command command, Replyer replyer)
        {
            string file_path = this.currentAccount_.LocalizePath(command.Param);

            if (String.IsNullOrEmpty(file_path))
            {
                this.OnLogger(replyer.Send(ReplyCode._550));
            }
            else
            {
                if (this.currentDTP_.Delete(file_path, true))
                {
                    this.OnLogger(replyer.Send(ReplyCode._250));
                }
                else
                {
                    this.OnLogger(replyer.Send(ReplyCode._550));
                }
            }
        }

        private void DoDELE(Command command, Replyer replyer)
        {
            string file_path = this.currentAccount_.LocalizePath(command.Param);

            if (String.IsNullOrEmpty(file_path))
            {
                this.OnLogger(replyer.Send(ReplyCode._550));
            }
            else
            {
                if (this.currentDTP_.Delete(file_path, false))
                {
                    this.OnLogger(replyer.Send(ReplyCode._250));
                }
                else
                {
                    this.OnLogger(replyer.Send(ReplyCode._550));
                }
            }
        }

        private void DoSTOU(Command command, Replyer replyer)
        {
            string file_path = this.currentAccount_.LocalizePath(command.Param);
            string extension = Path.GetExtension(file_path);

            Path.ChangeExtension(file_path, 
                DateTime.Now.ToString(".yyyy-MM-dd_HH-mm-ss_ffff") + extension);

            if (String.IsNullOrEmpty(file_path))
            {
                this.OnLogger(replyer.Send(ReplyCode._553));
            }
            else
            {
                this.NotifyType(replyer);

                if (this.currentDTP_.Store(file_path))
                {
                    this.OnLogger(replyer.Send(ReplyCode._250));
                }
                else
                {
                    this.OnLogger(replyer.Send(ReplyCode._553));
                }
            }
        }

        private void DoSTOR(Command command, Replyer replyer)
        {
            string file_path = this.currentAccount_.LocalizePath(command.Param);

            if (String.IsNullOrEmpty(file_path))
            {
                this.OnLogger(replyer.Send(ReplyCode._553));
            }
            else
            {
                this.NotifyType(replyer);

                if (this.currentDTP_.Store(file_path))
                {
                    this.OnLogger(replyer.Send(ReplyCode._250));
                }
                else
                {
                    this.OnLogger(replyer.Send(ReplyCode._553));
                }
            }
        }

        private void DoRETR(Command command, Replyer replyer)
        {
            string file_path = this.currentAccount_.LocalizePath(command.Param);

            if (String.IsNullOrEmpty(file_path))
            {
                this.OnLogger(replyer.Send(ReplyCode._553));
            }
            else
            {
                this.NotifyType(replyer);

                if (this.currentDTP_.Retrieve(file_path))
                {
                    this.OnLogger(replyer.Send(ReplyCode._250));
                }
                else
                {
                    this.OnLogger(replyer.Send(ReplyCode._553));
                }
            }
        }

        private void DoAPPE(Command command, Replyer replyer)
        {
            string file_path = this.currentAccount_.LocalizePath(command.Param);

            if(String.IsNullOrEmpty(file_path))
            {
                this.OnLogger(replyer.Send(ReplyCode._553));
            }
            else
            {
                this.NotifyType(replyer);

                if (this.currentDTP_.Append(file_path))
                {
                    this.OnLogger(replyer.Send(ReplyCode._250));
                }
                else
                {
                    this.OnLogger(replyer.Send(ReplyCode._553));
                }
            }
        }

        private void DoPORT(Command command, Replyer replyer)
        {
            this.currentDTP_.IsPassive = false;
            this.currentDTP_.StopPassive();

            if (this.currentDTP_.SetPort(command.Param))
            {
                this.OnLogger(replyer.Send(ReplyCode._200));
            }
            else
            {
                this.OnLogger(replyer.Send(ReplyCode._501));
            }
        }

        private void DoCDUP(Command command, Replyer replyer)
        {
            if (this.currentAccount_.ChangeDirectory(".."))
            {
                this.OnLogger(replyer.Send(ReplyCode._200));
            }
            else
            {
                this.OnLogger(replyer.Send(ReplyCode._550));
            }
        }

        private void DoCWD(Command command, Replyer replyer)
        {
            if(this.currentAccount_.ChangeDirectory(command.Param))
            {
                this.OnLogger(replyer.Send(ReplyCode._250));
            }
            else
            {
                this.OnLogger(replyer.Send(ReplyCode._550));
            }
        }

        private void DoMKD(Command command, Replyer replyer)
        {
            try
            {
                string path = this.currentAccount_.LocalizePath(command.Param);

                if(String.IsNullOrEmpty(path))
                {
                    this.OnLogger(replyer.Send(ReplyCode._550));
                }
                else
                {
                    Directory.CreateDirectory(path);

                    this.OnLogger(replyer.Send(ReplyCode._250));
                }
            }
            catch(Exception e)
            {
                this.OnLogger(replyer.Send(ReplyCode._550));
            }
        }

        private void DoPWD(Command command, Replyer replyer)
        {
            string remotePath = this.currentAccount_.RemoteCurrentPath;

            if(String.IsNullOrEmpty(remotePath))
            {
                remotePath = "/";
            }

            this.OnLogger(replyer.Send(ReplyCode._257, String.Format("\"{0}\" is your directory.", remotePath)));
        }

        private void DoPASS(Command command, Replyer replyer)
        {
            if(this.currentAccount_.CertainePassword(command.Param))
            {
                this.OnLogger(replyer.Send(ReplyCode._230));
            }
            else
            {
                if(String.IsNullOrEmpty(command.Param))
                {
                    this.OnLogger(replyer.Send(ReplyCode._501));
                }
                else
                {
                    this.OnLogger(replyer.Send(ReplyCode._530));
                }
            }
        }

        private void DoUSER(Command command, Replyer replyer)
        {
            if (this.currentAccount_.CertaineUser(command.Param))
            {
                replyer.Send(ReplyCode._331);
            }
            else
            {
                this.OnLogger(replyer.Send(ReplyCode._501));
            }
        }

        private void NotifyType(Replyer replyer)
        {
            if(!this.currentDTP_.IsPassive)
            {
                if (this.currentDTP_.IsBinary)
                {
                    this.OnLogger(replyer.Send(ReplyCode._150, "Open BINARY mode."));
                }
                else
                {
                    this.OnLogger(replyer.Send(ReplyCode._150, "Open ASCII mode."));
                }
            }
        }

        private static string MakeHostPortPart(EndPoint address, ushort port)
        {
            IPEndPoint ownIP = address as IPEndPoint;

            return String.Format("{0},{1},{2}",
                ownIP.Address.ToString().Replace('.', ','),
                (port >> 8), (port & 0x00FF));
        }

        private static ushort FindUnusedPort()
        {
            int[] used_ports = IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpConnections()
                .Select((conn) => { return conn.LocalEndPoint.Port; })
                .ToArray();
            Random generator = new Random();
            ushort port = 0;

            do
            {
                port = (ushort)generator.Next(50000, 60000);
            }
            while (used_ports.Contains(port));

            return port;
        }
    }
}
