using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Timers;
namespace consoleappforwebmobile
{
    class Program
    {
        private static string serversip = "192.168.20.55;";
        private static string serverurl = "/mobile/publicationUpd/index"; //"/mobile/Areas/Manage/RunConfigUpdate.aspx";
        private static System.Timers.Timer timer;
        private static string timeoverserver = "";
      //  private static int i = 0;
        private static long requesttimes = 0;
        private static int continuetime = 1;//持续时间，分钟
        private static DateTime dt;
        private static string paramvalue ="";
        private static bool secondsync = true;
        private static double repeatspantime = 20000;//首次超时，重复请求间隔
        private static double lastspantime = 30000;//最后一次请求距离上次请求间隔，小时
        private static Object thisLock = new Object();
        static void Main(string[] args)
        {
            paramvalue = "12";
            if (!string.IsNullOrWhiteSpace(serversip) && !string.IsNullOrWhiteSpace(serverurl))
            {
                action(serversip);
                Console.WriteLine("主函数调用结束！");
                if (!string.IsNullOrWhiteSpace(timeoverserver))
                {
                    SetTimer(repeatspantime);
                }
               
            }else
            {
                Console.WriteLine("web.config配置有问题！");
            }
            Console.Read();
        }

        private static void  action(string serversip)
        {
            StringBuilder strb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(serversip))
            {
                Console.WriteLine("第" + (++requesttimes) + "次请求 {0:HH:mm:ss.fff}",
          DateTime.Now);
                timeoverserver = "";
                foreach (string ip in serversip.Split(new char[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    strb.Append("主机"+ip + ":");
                    WebClient wc = new WebClient();
                    wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    byte[] paramater = Encoding.UTF8.GetBytes("productId=OverSeasAdmin!&param=" + paramvalue);
                    string requerurl = "http://" + ip;
                    if (!JudgeServer("http://" + ip))
                    {
                        timeoverserver += ip + ";";
                        strb.Append("服务器响应超时" + ";");                   
                        continue;
                    }
                    if (serverurl.Substring(0, 1) != "/")
                    {
                        serverurl = "/" + serverurl;
                    }
                    requerurl += serverurl;
                    byte[] data = wc.UploadData(requerurl, "Post", paramater);
                    strb.Append(Encoding.UTF8.GetString(data) + ";");
                }
                Console.WriteLine(strb.ToString());
            }
            else
            {
                timer.Stop();
                Console.WriteLine("全部成功,请求结束！");
            }
        }

        private static bool JudgeServer(string URL)
        {
            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(URL);
                myRequest.Method = "HEAD";
                myRequest.Timeout = 10000;  //超时时间10秒
                HttpWebResponse res = (HttpWebResponse)myRequest.GetResponse();
                return (res.StatusCode == HttpStatusCode.OK);
            }
            catch(Exception e)
            {
                return false;
            }
        }

        private static void SetTimer(double interval)
        {
            dt = DateTime.Now.AddMinutes(continuetime);
            timer = new System.Timers.Timer(interval);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            lock (thisLock)
            {
                if (dt < DateTime.Now)
            {
                    SyncLog();
                    Console.WriteLine("请求结束!" + DateTime.Now);
                    timer.Elapsed -= OnTimedEvent;
                    requesttimes = 0;
                    timeoverserver = GetValueByLog();
                    timer.Interval = lastspantime;
                    timer.Elapsed += OnSecondTimedEvent;
                    Console.WriteLine("_____________________________________________");
                    Console.WriteLine("第二次重复请求"+ timer.Interval/1000 + "秒后开始!" + DateTime.Now);
                    
                }
                else
            {
                    action(timeoverserver);
            }
                }
        }
        private static  void SyncLog()
        {
            if (!string.IsNullOrWhiteSpace(timeoverserver))
            {
                StreamWriter sw = new StreamWriter(@"E:\TeamFondationServers\consoleappforwebmobile\consoleappforwebmobile\synctimeoutlog.txt", false, Encoding.GetEncoding("GB2312"));
                foreach (string ip in timeoverserver.Split(new char[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    sw.WriteLine(DateTime.Now+"::"+ip+"::"+ serverurl+"::"+ paramvalue);
                }
                sw.Flush();
                sw.Close();
            }
        }
        //private static  void SecondSetTimer()
        //{
        //    requesttimes = 0;
        //    timeoverserver = GetValueByLog();
        //    timer.Interval=90000;
        //    timer.Elapsed += OnSecondTimedEvent;
        //    Console.WriteLine("_____________________________________________");
        //    Console.WriteLine("第二次重复请求开始!" + DateTime.Now);
        //}

        private static string GetValueByLog()
        {
            StringBuilder sb = new StringBuilder();
            StreamReader sr = new StreamReader(@"E:\TeamFondationServers\consoleappforwebmobile\consoleappforwebmobile\synctimeoutlog.txt", Encoding.GetEncoding("GB2312"));
            string value = sr.ReadLine();
            bool first = true;
            while (!string.IsNullOrEmpty(value))
            {
                string[] values = value.Split(new string[] { "::" }, StringSplitOptions.None);
                if (first)
                {
                    first = false;
                    serverurl = values[2];
                    paramvalue= values[3];
                }
                sb.Append(values[1]+";");
                value = sr.ReadLine();
            }
            sr.Close();
            return sb.ToString();
        }

        private static void OnSecondTimedEvent(Object source, ElapsedEventArgs e)
        {
            lock (thisLock)
            {
                if (secondsync)
                {
                    secondsync = false;
                    timer.Interval = repeatspantime;
                    dt = DateTime.Now.AddMinutes(continuetime);
                }
                if (dt < DateTime.Now)
                {
                    timer.Stop();
                    timer.Close();
                    timer.Dispose();
                    Console.WriteLine("第二次重复请求结束!" + DateTime.Now);
                    SyncForeverLog();
                }
                else
                {
                    action(timeoverserver); return;
                }
            }
        }
        private static void SyncForeverLog()
        {
            if (!string.IsNullOrWhiteSpace(timeoverserver))
            {
                StreamWriter sw = new StreamWriter(@"E:\TeamFondationServers\consoleappforwebmobile\consoleappforwebmobile\syncforeveroverlog.txt", true, Encoding.GetEncoding("GB2312"));
                foreach (string ip in timeoverserver.Split(new char[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    sw.WriteLine(DateTime.Now + "::" + ip + "::" + serverurl + "::" + paramvalue);
                }
                sw.Flush();
                sw.Close();
            }
        }

        private static double GetHour(double seconds)
        {
            return seconds * 60 * 60;
        }

        //private static void threadtimer() {
        //    var autoEvent = new AutoResetEvent(false);
        //    var stateTimer = new System.Threading.Timer(CheckStatus,
        //                     autoEvent, 1000, 250);

        //    stateTimer.Change(0, 500);
        //    Console.WriteLine("\nChanging period to .5 seconds.\n");

        //    // When autoEvent signals the second time, dispose of the timer. 

        //    stateTimer.Dispose();
        //    Console.WriteLine("\nDestroying timer.");
        //}
        //public static void CheckStatus(object stateInfo)
        //{
        //    Console.WriteLine(i++);
        //}
    }
}
