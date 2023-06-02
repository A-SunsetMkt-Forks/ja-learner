using System.Diagnostics;
using System.Runtime.InteropServices;

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



        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_SizeChanged(object sender, EventArgs e) {
            if(timerWindowAlign.Enabled)
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
    }
}