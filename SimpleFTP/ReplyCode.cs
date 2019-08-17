using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleFTP
{
    sealed class ReplyCode
    {
        /// <summary>再開のマーカー応答。テキストは厳密で特有の実装は許されない。フォーマットはMARK yyyy = mmmmでyyyyはユーザプロセスデータストリームマーカ、mmmmはサーバに対応するマーカ</summary>
        public static readonly ReplyCode _110 = new ReplyCode(110, "Restart marker reply.");
        /// <summary>サービスはnnn分後に準備できる。</summary>
        public static readonly ReplyCode _120 = new ReplyCode(120, "Service ready in nnn minutes.");
        /// <summary>データコネクションは既に確立されている。転送をはじめる。</summary>
        public static readonly ReplyCode _125 = new ReplyCode(125, "Data connection already open; transfer starting.");
        /// <summary>ファイルステータスは問題ない。データコネクションを確立する</summary>
        public static readonly ReplyCode _150 = new ReplyCode(150, "File status okay; about to open data connection.");
        /// <summary>コマンドOK</summary>
        public static readonly ReplyCode _200 = new ReplyCode(200, "Command okay.");
        /// <summary>コマンドは実装されていない。このサイトでは不必要である。</summary>
        public static readonly ReplyCode _202 = new ReplyCode(202, "Command not implemented, superfluous at this site.");
        /// <summary>システムのステータスまたは、システムヘルプの返答</summary>
        public static readonly ReplyCode _211 = new ReplyCode(211, "System status, or system help reply.");
        /// <summary>ディレクトリのステータス</summary>
        public static readonly ReplyCode _212 = new ReplyCode(212, "Directory status.");
        /// <summary>ファイルのステータス</summary>
        public static readonly ReplyCode _213 = new ReplyCode(213, "File status.");
        /// <summary>ヘルプメッセージ。サーバや特有の非標準コマンドの使用法。この返答は人間のユーザにのみ有用である。</summary>
        public static readonly ReplyCode _214 = new ReplyCode(214, "Help message.");
        /// <summary>NAME システムタイプ。NAMEにはAssigned Numbersにリストされている公式のシステム名が入る。</summary>
        public static readonly ReplyCode _215 = new ReplyCode(215, "NAME system type.");
        /// <summary>新しいユーザのためにサービスが準備できた。</summary>
        public static readonly ReplyCode _220 = new ReplyCode(220, "Service ready for new user.");
        /// <summary>コントロールコネクションのサービスを切断する。</summary>
        public static readonly ReplyCode _221 = new ReplyCode(221, "Service closing control connection.");
        /// <summary>データコネクションを確立した。現在転送は行われていない。</summary>
        public static readonly ReplyCode _225 = new ReplyCode(225, "Data connection open; no transfer in progress.");
        /// <summary>データコネクションを切断する。リクエストしたファイルへのアクションは成功した（例としてファイル転送時やファイル転送中止時）</summary>
        public static readonly ReplyCode _226 = new ReplyCode(226, "Closing data connection.");
        /// <summary>パッシブモードに入る (host1,host2,host3,host4,port1,port2)。</summary>
        public static readonly ReplyCode _227 = new ReplyCode(227, "Entering Passive Mode (h1,h2,h3,h4,p1,p2).");
        /// <summary>ユーザのログインに成功した。</summary>
        public static readonly ReplyCode _230 = new ReplyCode(230, "User logged in, proceed.");
        /// <summary>リクエストされたファイルへのアクションは問題なく完了した。</summary>
        public static readonly ReplyCode _250 = new ReplyCode(250, "Requested file action okay, completed.");
        /// <summary>PATHNAMEを作成した</summary>
        public static readonly ReplyCode _257 = new ReplyCode(257, "PATHNAME created.");
        /// <summary>ユーザーネームは正しい。パスワードが必要である。</summary>
        public static readonly ReplyCode _331 = new ReplyCode(331, "User name okay, need password.");
        /// <summary>ログインアカウントが必要である。</summary>
        public static readonly ReplyCode _332 = new ReplyCode(332, "Need account for login.");
        /// <summary>リクエストされたファイルへのアクションは、更に詳細な情報が必要である。</summary>
        public static readonly ReplyCode _350 = new ReplyCode(350, "Requested file action pending further information.");
        /// <summary>サービスは利用できない。コントロールコネクションを切断する。サービスがシャットダウンすることを知っている場合、いかなるコマンドに対しても返す。</summary>
        public static readonly ReplyCode _421 = new ReplyCode(421, "Service not available, closing control connection.");
        /// <summary>データコネクションを確立できない。</summary>
        public static readonly ReplyCode _425 = new ReplyCode(425, "Can't open data connection.");
        /// <summary>コネクションは切断された。転送は中止された。</summary>
        public static readonly ReplyCode _426 = new ReplyCode(426, "Connection closed; transfer aborted.");
        /// <summary>リクエストされたファイルのアクションは実行できない。</summary>
        public static readonly ReplyCode _450 = new ReplyCode(450, "Requested file action not taken.");
        /// <summary>リクエストされたアクションは中止された。実行中ローカルエラーが発生した。.</summary>
        public static readonly ReplyCode _451 = new ReplyCode(451, "Requested action aborted: local error in processing.");
        /// <summary>リクエストされたアクションは実行できない。ストレージの容量が不足している。ファイルが利用できない（ビジー状態）。</summary>
        public static readonly ReplyCode _452 = new ReplyCode(452, "Requested action not taken.");
        /// <summary>文法エラー。コマンドが解釈できない。このエラーはコマンドが長すぎる場合も含まれる。</summary>
        public static readonly ReplyCode _500 = new ReplyCode(500, "Syntax error, command unrecognized.");
        /// <summary>パラメータや引数において文法エラーがある。</summary>
        public static readonly ReplyCode _501 = new ReplyCode(501, "Syntax error in parameters or arguments.");
        /// <summary>コマンドは実装されていない。</summary>
        public static readonly ReplyCode _502 = new ReplyCode(502, "Command not implemented.");
        /// <summary>コマンドの順序が間違っている。</summary>
        public static readonly ReplyCode _503 = new ReplyCode(503, "Bad sequence of commands.");
        /// <summary>そのパラメータに対してコマンドは実装されていない。</summary>
        public static readonly ReplyCode _504 = new ReplyCode(504, "Command not implemented for that parameter.");
        /// <summary>ログインできない。</summary>
        public static readonly ReplyCode _530 = new ReplyCode(530, "Not logged in.");
        /// <summary>ファイルの格納にはアカウントが必要である。</summary>
        public static readonly ReplyCode _532 = new ReplyCode(532, "Need account for storing files.");
        /// <summary>リクエストされたアクションは実行できない。ファイルは利用できない（見つからない、アクセスできない）</summary>
        public static readonly ReplyCode _550 = new ReplyCode(550, "Requested action not taken.");
        /// <summary>リクエストされたアクションは中止された。ページタイプが不明である。</summary>
        public static readonly ReplyCode _551 = new ReplyCode(551, "Requested action aborted: page type unknown.");
        /// <summary>リクエストされたアクションは中止された。（現在のディレクトリやデータセットに）割り当てられたストレージを超過した。</summary>
        public static readonly ReplyCode _552 = new ReplyCode(552, "Requested file action aborted.");
        /// <summary>リクエストされたアクションは実行できない。ファイル名が受け付けられない。</summary>
        public static readonly ReplyCode _553 = new ReplyCode(553, "Requested action not taken.");


        //
        public int Code { get; private set; }
        public string Message { get; private set; }

        public ReplyCode(int code, string message)
        {
            this.Code = code;
            this.Message = message;
        }

        public override string ToString()
        {
            return String.Format("{0} {1}", this.Code, this.Message);
        }
    }
}
