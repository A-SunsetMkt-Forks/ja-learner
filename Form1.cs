using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MeCab;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Interfaces;
using OpenAI;

namespace ja_learner
{
    public partial class Form1 : Form
    {

        // ���� Windows API ����
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rectangle rect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point point);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        // ���� RECT �ṹ��
        [StructLayout(LayoutKind.Sequential)]
        public struct Rectangle
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // ���� POINT �ṹ��
        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X;
            public int Y;
        }


        IDisposable _server = null;

        private MeCabParam parameter;
        private MeCabTagger tagger;

        private string sentence = "";

        public Form1()
        {
            InitializeComponent();
            InitializeWebView();
            // DisableCors();
        }
        private async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            // ��ʼ�� HTTP ������
            HttpServer.StartServer();
            // webView.Source = new Uri("http://localhost:8080"); // build
            webView.Source = new Uri("http://localhost:5173"); // dev

            // ��ʼ�� mecab dotnet
            parameter = new MeCabParam();
            tagger = MeCabTagger.Create(parameter);

            // ��ȡ api key
            textBoxApiKey.Text = File.ReadAllText("apikey.txt");
        }

        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true; // ��ֹWebView2���´���

            // ʹ��Ĭ�������������
            Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true });
        }

        private string RunMecab()
        {
            string result = "[";
            foreach (var node in tagger.ParseToNodes(sentence))
            {
                if (node.CharType > 0)
                {
                    var features = node.Feature.Split(',');
                    var displayFeatures = string.Join(", ", features);
                    // features[0] �Ǵ��ԣ�[6] ��ԭ�ͣ�[7] �Ƿ���������еĻ���
                    string pos = features[0];
                    string basic = features[6];
                    string reading = features.Length > 7 ? features[7] : "";
                    result += $"{{surface:'{node.Surface}',pos:'{pos}',basic:'{basic}',reading:'{reading}'}},";
                }
            }
            result += "]";
            return result;
        }

        private async void UpdateMecabResult(string json)
        {
            string result = await webView.ExecuteScriptAsync($"updateData({json})");

        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (timerWindowAlign.Enabled)
            {
                heightAfter = this.Height;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private System.Drawing.Point locationBefore; // ��¼��ͨģʽ�´��ڵ�λ��
        private Size sizeBefore; // ��¼��ͨģʽ�´��ڵĴ�С
        private int heightAfter = 200; // ����ģʽʱ������ͨ����Ƚϰ�

        private void timerWindowAlign_Tick(object sender, EventArgs e)
        {
            int processId = int.Parse(textBox1.Text);
            try
            {
                // ��ȡ���̶���
                Process process = Process.GetProcessById(processId);

                // ��ȡ�����ھ��
                IntPtr mainWindowHandle = process.MainWindowHandle;

                // ���� GetWindowRect ������ȡ����λ�úʹ�С
                Rectangle rect;
                if (GetWindowRect(mainWindowHandle, out rect))
                {
                    this.Top = rect.Bottom;
                    this.Left = rect.Left;
                    this.Width = rect.Right - rect.Left; // ������
                }
            }
            finally
            {

            }

        }

        private void btnSelectWindow_Click(object sender, EventArgs e)
        {

        }

        private void checkBoxClipboardMode_CheckedChanged(object sender, EventArgs e)
        {
            btnInputText.Enabled = !checkBoxClipboardMode.Checked;
        }

        private void checkBoxAlignWindow_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAlignWindow.Checked)
            {
                sizeBefore = this.Size;
                locationBefore = this.Location;
                this.Height = heightAfter;
                timerWindowAlign.Enabled = true;
            }
            else
            {
                timerWindowAlign.Enabled = false;
                heightAfter = this.Height;
                this.Size = sizeBefore;
                this.Location = locationBefore;
            }

        }

        private void btnInputText_Click(object sender, EventArgs e)
        {
            sentence = Microsoft.VisualBasic.Interaction.InputBox("Prompt", "Title", "�Ĥξ���(�Ǥ��ޤ��{)������(�Ǥ����{)���������JavaScript�饤�֥��", 0, 0);
            string json = RunMecab();
            UpdateMecabResult(json);
        }

        private async void btnCallGPT_Click(object sender, EventArgs e)
        {
            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = textBoxApiKey.Text
            });

            var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem("����һ��������ʦ���ҽ��ṩһЩ������ӣ���Ĺ����ǽ����Ƿ�������ģ������������Ĳ������������Ľ��͡�"),
                    ChatMessage.FromUser(sentence),
                },
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = 150//optional
            });
            MessageBox.Show(sentence);
            MessageBox.Show(completionResult.Error.Message);
            if (completionResult.Successful)
            {
                textBoxChat.Text=completionResult.Choices.First().Message.Content;
            }
        }
    }
}