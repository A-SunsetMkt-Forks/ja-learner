using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MeCab;
using System.Text;
using System.Reflection.Metadata;

namespace ja_learner
{
    public partial class Form1 : Form
    {

        // ���� Windows API ����
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rectangle rect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);
        // �������λ�û�ȡĿ�괰�ھ��hwnd
        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point point);

        // ����hwnd��ȡ���ڱ���
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder title, int size);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd); 
        
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        static extern IntPtr GetParent(IntPtr hWnd);

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

        DictForm dictForm;


        private string sentence = "�Ĥξ���(�Ǥ��ޤ��{)������(�Ǥ����{)���������JavaScript�饤�֥��";

        public Form1()
        {
            InitializeComponent();
            // DisableCors();
        }
        private async Task InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Light;
            webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
        }
        private async void Form1_Load(object sender, EventArgs e)
        {
            // ��ʼ�� webview
            await InitializeWebView();
            // ��ʼ�� HTTP ������
            HttpServer.StartServer();
            // webView.Source = new Uri("http://localhost:8080"); // build
            webView.Source = new Uri("http://localhost:5173"); // dev

            // ��ʼ�� mecab dotnet
            parameter = new MeCabParam();
            tagger = MeCabTagger.Create(parameter);

            UpdateMecabResult(RunMecab());
            dictForm = new DictForm(this);
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


        private System.Drawing.Point locationBefore; // ��¼��ͨģʽ�´��ڵ�λ��
        private Size sizeBefore; // ��¼��ͨģʽ�´��ڵĴ�С
        private int heightAfter = 200; // ����ģʽʱ������ͨ����Ƚϰ�

        private void timerWindowAlign_Tick(object sender, EventArgs e)
        {
            IntPtr hwnd = IntPtr.Parse(textBoxHwnd.Text);
            try
            {
                // ���� GetWindowRect ������ȡ����λ�úʹ�С
                Rectangle rect;
                if (GetWindowRect(hwnd, out rect))
                {
                    this.Top = rect.Bottom;
                    this.Left = rect.Left;
                    this.Width = rect.Right - rect.Left; // ������
                }
            }
            catch (Exception ex)
            {
                WindowAlign = false;
            }
        }

        public bool WindowAlign
        {
            get
            {
                return checkBoxAlignWindow.Checked;
            }
            set
            {
                checkBoxAlignWindow.Checked = value;
                timerWindowAlign.Enabled = value;
            }
        }

        private void checkBoxClipboardMode_CheckedChanged(object sender, EventArgs e)
        {
            ClipBoardMode = checkBoxClipboardMode.Checked;
        }
        private void timerGetClipboard_Tick(object sender, EventArgs e)
        {
            string newSentence = Clipboard.GetText(TextDataFormat.UnicodeText).Trim();
            if (newSentence != sentence)
            {
                sentence = newSentence;
                UpdateMecabResult(RunMecab());
            }
        }
        public bool ClipBoardMode
        {
            get
            {
                return checkBoxClipboardMode.Checked;
            }
            set
            {
                checkBoxClipboardMode.Checked = value;
                timerGetClipboard.Enabled = value;
                btnInputText.Enabled = !value;
            }
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
            sentence = Microsoft.VisualBasic.Interaction.InputBox("Prompt", "Title", "", 0, 0);
            string json = RunMecab();
            UpdateMecabResult(json);
        }

        private void timerSelectWindow_Tick(object sender, EventArgs e)
        {
            Point cursorPos = new Point();
            cursorPos.X = Cursor.Position.X;
            cursorPos.Y = Cursor.Position.Y;
            // ��ǰ���λ�����ڴ��ڵ���߸����ڵ�hwnd
            IntPtr hwnd = WindowFromPoint(cursorPos);
            IntPtr parentHwnd;
            while(true)
            {
                parentHwnd = GetParent(hwnd);
                if (parentHwnd == IntPtr.Zero) break;
                hwnd = parentHwnd;
            }
            textBoxHwnd.Text = hwnd.ToString();

            // �ж�����Ƿ���
            bool mouseDown = (Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left;

            if (mouseDown)
            {
                timerSelectWindow.Enabled = false;
            }
        }

        private void btnSelectWindow_Click(object sender, EventArgs e)
        {
            WindowAlign = false;
            timerSelectWindow.Enabled = true;
        }

        private void checkBoxDark_CheckedChanged(object sender, EventArgs e)
        {
            webView.CoreWebView2.Profile.PreferredColorScheme = checkBoxDark.Checked ? CoreWebView2PreferredColorScheme.Dark : CoreWebView2PreferredColorScheme.Light;
        }

        private void checkBoxShowDictForm_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxShowDictForm.Checked)
            {
                dictForm.Show();
            }
            else
            {
                dictForm.Hide();
            }
        }

        public static String GetWindowTitle(IntPtr handle)
        {
            int length = GetWindowTextLength(handle);
            StringBuilder text = new StringBuilder(length + 1);
            GetWindowText(handle, text, text.Capacity);
            return text.ToString();
        }
        private void textBoxHwnd_TextChanged(object sender, EventArgs e)
        {
            try
            {
                IntPtr hwnd = IntPtr.Parse(textBoxHwnd.Text);
                string windowTitle = GetWindowTitle(hwnd); 
                checkBoxAlignWindow.Text = "�롾" + windowTitle + "������";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}