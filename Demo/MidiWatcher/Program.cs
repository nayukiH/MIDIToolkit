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
            Application.EnableVisualStyles();//�����κοؼ�ǰ���ã�������ʾ����ͨ����main�ĵ�һ��
            Application.SetCompatibleTextRenderingDefault(false);//�����κδ���ǰ���ã����ÿؼ����ı�Ĭ����ʾ��ʽ��trueΪGDI+��falseΪGDI
            Application.Run(new Form1());
        }
    }
}