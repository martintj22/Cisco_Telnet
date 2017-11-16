using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Telnet
{
    public class TelnetConnection : IDisposable
    {
        public string hostname;
        private TcpClient tcpSocket;
        private int TimeoutMs = 100;
        
        public bool IsConnected
        {
            get
            {
                try
                {
                    return tcpSocket.Connected;
                }
                catch
                {
                    return false;
                }
            }
        }
        
        public TelnetConnection(string hostname, int port)
        {
            try
            {
                tcpSocket = new TcpClient(hostname, port);
            }
            catch
            {
                Write("Connection failed");
            }
        }

        ~TelnetConnection()
        {
            Dispose(false);
        }

        public string CiscoLogin(string username, string password)
        {
            string s = ReadUntil("Username:");
            WriteLine(username);
            s = ReadUntil("Password:");
            WriteLine(password);
            s = ReadUntil("[>|#]");

            string[] s2 = s.Split('\r', '\n');
            s = s2[s2.Length - 1];
            Match match = Regex.Match(s, @"(.*)[>|#]$");
            if (match.Success)
            {
                hostname = match.Groups[1].Value;
            }
            else
            {
                Console.WriteLine("ERROR: User prompt not returned as expected - Is it a Cisco router?");
            }
            WriteLine("terminal length 0"); // Disable multipage outputs
            s = ReadUntil(hostname + "#");
            return s;
        }

        public string CiscoLogin(string password)
        {
            string s = ReadUntil("Password:");
            WriteLine(password);
            s = ReadUntil(">");

            string[] s2 = s.Split('\r', '\n');
            s = s2[s2.Length - 1];
            Match match = Regex.Match(s, @"(.*)>$");
            if (match.Success)
            {
                hostname = match.Groups[1].Value;
            }
            else
            {
                Console.WriteLine("ERROR: User prompt not returned as expected - Is it a Cisco router?");
            }
            return s;
        }

        public void CiscoEnable(string password)
        {
            WriteLine("enable");
            Console.WriteLine("Sent enable");
            string s = ReadUntil("Password:");
            WriteLine(password);
            s = ReadUntil(hostname + "#");
            WriteLine("terminal length 0"); // Disable multipage outputs
            s = ReadUntil(hostname + "#");
        }
        
        public List<String> CiscoCommand(string Command)
        {
            String Result;

            List<String> ResultList = new List<String>();
            WriteLine(Command);
            Result = ReadUntil("(" + hostname + "#|" + hostname + "\\(config.*\\)#)");
            String[] ResultArray = Result.Split('\r');
            //Remove newline and replace tabs with space
            for (int i = 0; i < ResultArray.Length; i++)
            {
                ResultArray[i] = Regex.Replace(ResultArray[i], @"\n|\r", "");    // Remove \r and \n
                ResultArray[i] = Regex.Replace(ResultArray[i], @"\t+", " ");        // Replace one or more tabs with one space
                ResultList.Add(ResultArray[i]);
            }
            return ResultList;
        }

        public void WriteLine(string cmd)
        {
            Write(cmd + "\n");
        }

        public void Write(string cmd)
        {

            if (!tcpSocket.Connected)
            {
                return;
            }
            
            byte[] buf = ASCIIEncoding.ASCII.GetBytes(cmd.Replace("\0xFF", "\0xFF\0xFF"));
            tcpSocket.GetStream().Write(buf, 0, buf.Length);
        }
        
        public string ReadUntil(string Delimiter)
        {
            var sb = new StringBuilder();
            string s;

            do
            {
                int input = tcpSocket.GetStream().ReadByte();
                switch (input)
                {
                    case -1:
                        break;

                    case (int)Verbs.Iac:
                        // interpret as command
                        int inputVerb = tcpSocket.GetStream().ReadByte();
                        if (inputVerb == -1)
                        {
                            break;
                        }

                        switch (inputVerb)
                        {
                            case (int)Verbs.Iac:
                                // literal IAC = 255 escaped, so append char 255 to string
                                sb.Append(inputVerb);
                                break;

                            case (int)Verbs.Do:
                            case (int)Verbs.Dont:
                            case (int)Verbs.Will:
                            case (int)Verbs.Wont:
                                // reply to all commands with "WONT", unless it is SGA (suppres go ahead)
                                int inputoption = tcpSocket.GetStream().ReadByte();
                                if (inputoption == -1)
                                {
                                    break;
                                }

                                tcpSocket.GetStream().WriteByte((byte)Verbs.Iac);

                                if (inputoption == (int)Options.Sga)
                                {
                                    tcpSocket.GetStream().WriteByte(inputVerb == (int)Verbs.Do ? (byte)Verbs.Will : (byte)Verbs.Do);
                                }
                                else
                                {
                                    tcpSocket.GetStream().WriteByte(inputVerb == (int)Verbs.Do ? (byte)Verbs.Wont : (byte)Verbs.Dont);
                                }

                                tcpSocket.GetStream().WriteByte((byte)inputoption);
                                break;
                        }

                        break;

                    default:

                        sb.Append((char)input);
                        break;
                }
                s = sb.ToString();

            } while (!Regex.IsMatch(s, Delimiter + "$"));
            //} while ( !s.EndsWith(Delimiter)) ;

            return (s);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (tcpSocket != null)
                {
                    ((IDisposable)tcpSocket).Dispose();
                    tcpSocket = null;
                }
            }
        }
        
        #region Private Enums

        enum Verbs
        {
            Will = 251,
            Wont = 252,
            Do = 253,
            Dont = 254,
            Iac = 255
        }

        enum Options
        {
            Sga = 3
        }

        #endregion
    }
}