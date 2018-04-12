/*Copyright Hermein Developer
 License CC BY-SA 4.0 
 06.04.2018 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;

namespace GroupsDeleteBanned
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }
        public static string VkGid;
        public static string VkToken;
        public static List<string> Lst;
        public static int Usercount = 0;
        private void button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://oauth.vk.com/authorize?client_id=6438972&display=page&redirect_uri=https://oauth.vk.com/blank.html&scope=groups,offline&response_type=token&v=5.74");
        }
        public bool Validator()
        {
            int a = 0;
            if (int.TryParse(gid.Text, out a))
            {
                if (!string.IsNullOrWhiteSpace(vkToken.Text))
                    VkGid = a.ToString();
                VkToken = vkToken.Text;
                return true;
            }
            return false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (Validator())
            {
                Properties.Settings.Default.gid = VkGid;
                Properties.Settings.Default.token = VkToken;
                Properties.Settings.Default.Save();
            }
            else
            {
                MessageBox.Show("Неверно заполнены поля ввода", "Ошибка");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.gid))
            {
                VkGid = Properties.Settings.Default.gid;
                gid.Text = VkGid;
            }
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.token))
            {
                VkToken = Properties.Settings.Default.token;
                vkToken.Text = VkToken;
            }
        }
        private static string Post(string url, string post)
        {
            ServicePointManager.ServerCertificateValidationCallback =
                     ((sender, certificate, chain, sslPolicyErrors) => true);
            var html = string.Empty;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent =
                    "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/32.0.1700.102 YaBrowser/14.2.1700.12599 Safari/537.36";
                request.Method = "POST";
                request.AllowAutoRedirect = true;
                request.Timeout = 60000;
           
                request.ContentType = "application/x-www-form-urlencoded";
                request.Accept = "*/*";
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate,sdch");
                request.Headers.Add(HttpRequestHeader.AcceptLanguage, "ru,en;q=0.8");
                var encodedPostParams = Encoding.Default.GetBytes(post);
                request.ContentLength = encodedPostParams.Length;
                request.GetRequestStream().Write(encodedPostParams, 0, encodedPostParams.Length);
                request.GetRequestStream().Close();
                var response = (HttpWebResponse)request.GetResponse();
                var responseStream = response.GetResponseStream();
                if (responseStream != null)
                {
                    if (response.ContentEncoding.ToLower().Contains("gzip"))
                        responseStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    else if (response.ContentEncoding.ToLower().Contains("deflate"))
                        responseStream = new System.IO.Compression.DeflateStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
                    var sRead = new StreamReader(responseStream, Encoding.Default);
                    html = sRead.ReadToEnd();
                    sRead.Close();
                    sRead.Dispose();
                    responseStream.Close();
                    responseStream.Dispose();
                }
            }
            catch
            {

            }
            return html;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (Validator())
            {
                button3.Enabled = false;
                Lst = new List<string>();
                Usercount = 999;
                progressBar1.Value = 0;
                new Thread(() => {
                    for (var i = 0; i < 100000; i++)
                    {
                        var offset = (i * 1000);
                        if (Usercount <= 0 || offset >= Usercount)
                        {
                            progressBar1.Value = progressBar1.Maximum;
                            break;
                        }
                        var all = Post("https://api.vk.com/method/groups.getMembers", "group_id=" + VkGid + "&count=1000&offset=" + offset + "&fields=last_seen&v=5.74&access_token=" + VkToken);
                        Parse(all);
                        Thread.Sleep(350);

                    }
                    if (Lst.Count > 0)
                    {
                        var msg = MessageBox.Show("Вы действительно хотите удалить " + Lst.Count + " собак из группы?", "Внимание!", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                        if (msg == DialogResult.OK)
                        {
                            progressBar1.Value = 0;
                            progressBar1.Maximum = Lst.Count;
                            foreach (var line in Lst)
                            {
                                var rem = Post("https://api.vk.com/method/groups.removeUser", "group_id=" + VkGid + "&user_id=" + line + "&v=5.74&access_token=" + VkToken);
                                if (!RemParse(rem))
                                {
                                    MessageBox.Show("Не удалось удалить одного или нескольких пользователей!", "Ошибка!");
                                    break;
                                }
                                if (progressBar1.Value < progressBar1.Maximum)
                                {
                                    progressBar1.Value++;
                                }
                                Thread.Sleep(350);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Собак в группе не обнаружено!", "Информация!");
                    }
                    button3.Enabled = true;


                }).Start();
            }
            else
            {
                MessageBox.Show("Неверно заполнены поля ввода", "Ошибка");
            }
           
        }
        private void Parse(string all)
        {
            try
            {
                var obj = JObject.Parse(all);
                var resp = obj["response"];
                var cnt = resp["count"].Value<int>();
                Usercount = cnt;
                foreach(JToken line in resp["items"])
                {
                    var banned = line["deactivated"];
                    if (banned != null && !string.IsNullOrWhiteSpace(banned.Value<string>()))
                    {
                        Lst.Add(line["id"].Value<string>());
                    }
                }
                progressBar1.Maximum = (Usercount/1000+1);
                if (progressBar1.Value < progressBar1.Maximum)
                {
                    progressBar1.Value++;
                }
            }
            catch { Usercount = 0; }
        }
        private bool RemParse(string rem)
        {
            try
            {
                var obj = JObject.Parse(rem);
                var resp = obj["response"];
                if (resp.Value<string>() == "1")
                {
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
