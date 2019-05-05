using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MidiWatcher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();//创建任何控件前调用（允许显示），通常在main的第一行
            Application.SetCompatibleTextRenderingDefault(false);//创建任何窗口前调用（设置控件中文本默认显示方式，true为GDI+，false为GDI
            Application.Run(new Form1());
        }
    }
}