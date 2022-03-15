using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            page1.Text = "1";
            page2.Text = "2";
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            if (string.IsNullOrWhiteSpace(page1.Text) || string.IsNullOrWhiteSpace(page2.Text))
            {
                //dataGridView1.DataSource = new DataSet();
            }
            else
            {
                var dataset = new DataSet();
                dataset.Tables.Add("Name");
                dataset.Tables.Add("value");

                var url = "https://www.2meinv.com/";
                var startPage = Convert.ToInt32(page1.Text);
                var endPage = Convert.ToInt32(page2.Text);
                var downText = "C:/Users/smiles/Desktop/2";

                for (int i = startPage; i <= endPage; i++)
                {
                    dataset.Tables[i].Rows.Add(i);
                    //获取下载地址
                    string pathPath = Path.Combine(downText, i.ToString());
                    //判断下载地址是否存在
                    if (!Directory.Exists(pathPath))
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(pathPath);
                        directoryInfo.Create();
                    }

                    //遍历页面
                    var reData = await Http(url + "/index-" + i + ".html", "get", "");

                    //解析页面进入图片详情
                    var dirList = await GetDirList(reData);

                    foreach (var dic in dirList)
                    {
                        //获取下载地址
                        string dirPath = Path.Combine(pathPath, dic.Value);
                        //判断下载地址是否存在
                        if (!Directory.Exists(dirPath))
                        {
                            DirectoryInfo directoryInfo = new DirectoryInfo(dirPath);
                            directoryInfo.Create();
                        }

                        var dicData = await Http(dic.Key, "get", "");

                        var endimgPage = await GetEndPage(dicData);
                        for (int j = 1; j <= endimgPage; j++)
                        {
                            try
                            {
                                var imgurl = dic.Key.Replace(".html", "-" + j + ".html");
                                var imgData = await Http(imgurl, "get", "");

                                var downurl = await GetImgUrl(imgData);


                                await DownloadFile(downurl, dirPath + "/" + j + Path.GetExtension(downurl));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }

                        }

                    }


                }


            }

            button1.Enabled = true;

        }


        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="serverFilePath"></param>
        /// <param name="targetPath"></param>
        public async Task DownloadFile(string serverFilePath, string targetPath)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serverFilePath);

            WebResponse respone = request.GetResponse();
            var size = respone.ContentLength;
            long taotal = 0;
            Stream netStream = respone.GetResponseStream();
            using (Stream fileStream = new FileStream(targetPath, FileMode.Create))
            {
                byte[] read = new byte[1024];
                int realReadLen = netStream.Read(read, 0, read.Length);
                while (realReadLen > 0)
                {
                    taotal += realReadLen;
                    var jindu1 = taotal / size * 100;
                    //bar.Value = jindu1;
                    //jindu.Content = jindu1 + "%";
                    fileStream.Write(read, 0, realReadLen);
                    realReadLen = await netStream.ReadAsync(read, 0, read.Length);
                }
                netStream.Close();
                fileStream.Close();
            }
        }



        public async Task<string> GetImgUrl(string data)
        {

            var text1 = "<div class=\"pp hh\">(.|\n)*?</div>";
            var reg1 = new Regex(text1);
            var result1 = reg1.Matches(data);
            var text = "<img(.|\n)*?</a>";
            var reg = new Regex(text);
            var result = reg.Matches(result1[0].Value);

            var regHref = "src=\"(.|\n)*?\"";

            var urlreg = new Regex(regHref);
            var urlResult = urlreg.Matches(result[0].Value);
            var url = urlResult[0].Value.Replace("src=", "").Trim('"');


            return url;

        }


        public async Task<int> GetEndPage(string data)
        {

            var text1 = "<div class=\"page-show\">(.|\n)*?</div>";
            var reg1 = new Regex(text1);
            var result1 = reg1.Matches(data);
            var text = "<a(.|\n)*?</a>";
            var reg = new Regex(text);
            var result = reg.Matches(result1[0].Value);

            var ele = result.FirstOrDefault(o => o.Value.Contains("下一页"));
            var endIndex = result.ToList().IndexOf(ele);
            var endEle = result[endIndex - 1];

            var regHref = ">(.|\n)*?</a>";

            var urlreg = new Regex(regHref);
            var urlResult = urlreg.Matches(endEle.Value);
            var endPage = urlResult[0].Value.Replace("</a>", "").Trim('>');


            return Convert.ToInt32(endPage);

        }




        /// <summary>
        /// 解析页面中图片的名字及下载地址
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, string>> GetDirList(string data)
        {

            var text1 = "<ul class=\"detail-list\">(.|\n)*?</ul>";
            var reg1 = new Regex(text1);
            var result1 = reg1.Matches(data);
            var text = "<li>(.|\n)*?</li>";
            var reg = new Regex(text);
            var result = reg.Matches(result1[0].Value);


            Dictionary<string, string> dic = new Dictionary<string, string>();
            foreach (Match items in result)
            {

                var regHref = "href=\"(.|\n)*?\"";

                var urlreg = new Regex(regHref);
                var urlResult = urlreg.Matches(items.Value);

                var hrefItem = urlResult[0];

                var href = hrefItem.Value.Replace("href=", "").Trim('"');

                var regTitle = "alt=\"(.|\n)*?\"";

                var titleReg = new Regex(regTitle);
                var titleResult = titleReg.Matches(items.Value);

                var titleItem = titleResult[0];

                var title = titleItem.Value.Replace("alt=", "").Replace("\t","").Replace("\r","").Trim('"');

                dic.Add(href, title);
            }


            return dic;

        }


        /// <summary>
        /// 发送http请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<string> Http(string url, string method, string data)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = string.IsNullOrEmpty(method) ? "GET" : method;
            request.ContentType = "application/json;charset=utf-8";
            if (!string.IsNullOrEmpty(data))
            {
                Stream RequestStream = await request.GetRequestStreamAsync();
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                await RequestStream.WriteAsync(bytes, 0, bytes.Length);
                RequestStream.Close();
            }
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            Stream ResponseStream = response.GetResponseStream();
            StreamReader StreamReader = new StreamReader(ResponseStream, Encoding.GetEncoding("utf-8"));
            string re = await StreamReader.ReadToEndAsync();
            StreamReader.Close();
            ResponseStream.Close();
            return re;
        }


    }
}
