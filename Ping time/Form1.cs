using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace Ping_time
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            ipss();
        }

        private void ipss()
        {
            if (!File.Exists("Directios.txt"))
            {
                string[] ips = new string[3];

                ips[0] = "8.8.8.8";
                ips[1] = "1.1.1.1";
                ips[2] = "127.0.0.1";

                File.WriteAllLines("Directios.txt", ips);
            }

            string[] ip2 = File.ReadAllLines("Directios.txt");

            foreach (string c in ip2)
            {
                textBox1.Items.Add(c);
            }

            textBox1.Text = textBox1.Items[0].ToString();
        }

        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        int time = 0;

        int minuto = 0;

        int timeOut = 2000;

        string s;

        bool notify = false;

        int promedio = 100;

        long nP = 100;

        long regular = 0;

        int sucess = 0;
        int lost = 0;

        bool chart = false;

        double totaltime = 0;

        Queue<long> cola = new Queue<long>();

        public delegate void Miliseg(long a);

        private void button1_Click(object sender, EventArgs e)
        {
            if(button1.Text == "Start")
            {
                button1.Text = "Stop";
                enable.ForeColor = Color.Lime;
                textBox1.Enabled = false;
            }
            else
            {
                button1.Text = "Start";
                enable.ForeColor = Color.FromArgb(224, 224, 224);
                textBox1.Enabled = true;
            }

            sucess = 0;

            lost = 0;

            time = 0;

            s = textBox1.Text;

            timer1.Enabled = !timer1.Enabled;
            timer2.Enabled = timer1.Enabled;
        }
        
        private void timer1_Tick(object sender, EventArgs e)
        {
            Thread t = new Thread(task);
            t.Start();
            totaltime += timer1.Interval / 1000;
        }

        private void task()
        {
            Ping p = new Ping();
            PingReply r;

            try
            {
                r = p.Send(s, timeOut);

                if (r.Status == IPStatus.Success)
                {
                    this.Invoke(new Miliseg(miliseg), r.RoundtripTime);
                }
                else
                {
                    this.Invoke(new Miliseg(miliseg), -1);
                }
            }
            catch (Exception)
            {
                this.Invoke(new Miliseg(miliseg), -2);
            }
        }

        private void miliseg(long c)
        {
            if (c == -1)
            {
                time = 0;

                lost += 1;

                if (chart)
                {
                    c = timeOut;
                }
                else
                {
                    LOG.AppendText("Lost" + Environment.NewLine);
                }

                Plost();
            }
            else if (c == -2)
            {
                time = 0;

                if (chart)
                {
                    c = timeOut;
                }
                else
                {
                    LOG.AppendText("Error" + Environment.NewLine);
                }
            }
            else
            {
                if (!chart)
                {
                    LOG.AppendText(string.Format("Ping to " + s.ToString() + " Successful"
                       + " Response delay = " + c + " ms" + "\n") + Environment.NewLine);
                }
            }

            regular = Promedio(c);

            AVG.Text = regular + "ms";

            if (time >= 60)
            {
                minuto = time / 60;
                
                label1.Text = minuto + ":" + (time%60);
            }
            else
            {
                label1.Text = "0:" + time.ToString();
            }

            sucess += 1;
            Plost();

            if (checkBox1.Checked)
            {
                if (notify == false && regular <= nP)
                {
                    notify = true;
                    notifyIcon1.BalloonTipTitle = "Ping alert:";
                    notifyIcon1.BalloonTipText = "Ping average is less than " + nP + "ms";
                    notifyIcon1.ShowBalloonTip(400);
                }
                else if (notify == true && regular > nP)
                {
                    notify = false;
                    notifyIcon1.BalloonTipTitle = "Ping alert:";
                    notifyIcon1.BalloonTipText = "Ping average is greater than " + nP + "ms";
                    notifyIcon1.ShowBalloonTip(400);
                }
            }
            
        }

        private void Grafica(long a)
        {

        }

        private long Promedio(long a)
        {
            if(cola.Count < promedio)
            {
                if(chart)
                    chart1.Series["ChartData"].Points.AddXY(totaltime, a);

                if (a < timeOut)
                    cola.Enqueue(a);
            }
            else
            {
                if (a < timeOut)
                {
                    cola.Dequeue();
                    cola.Enqueue(a);
                }

                if (chart)
                {
                    chart1.Series["ChartData"].Points.RemoveAt(0);
                    chart1.Series["ChartData"].Points.AddXY(totaltime, a);
                }
            }

            long b = 0;

            foreach (int c in cola)
            {
                b += c;
            }

            if(b > 0)
                b = b / cola.Count();

            return b;
        }

        private void Plost()
        {
            int h = 0;

            if (lost > 0)
            {
                h = lost + sucess;
                h = (lost * 100) / h;
            }


            if (h > 100)
            {
                label5.Text = "100% Lost";
            }
            else
            {
                label5.Text = h.ToString() + "% Lost";
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            time += 1;
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            LOG.Text = "";
        }

        private void applyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int a;

            try
            {
                a = int.Parse(toolStripTextBox1.Text);
            }
            catch (Exception)
            {
                return;
            }

            timeOut = int.Parse(toolStripTextBox1.Text);
        }

        private void applyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            int a;

            try
            {
                a = int.Parse(delay.Text);
            }
            catch (Exception)
            {
                return;
            }
            
            timer1.Interval = int.Parse(delay.Text);
        }

        private void applyToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            int a;

            try
            {
                a = int.Parse(toolStripTextBox2.Text);
            }
            catch (Exception)
            {
                return;
            }

            promedio = int.Parse(toolStripTextBox2.Text);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            cola.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            sucess = 0;
            lost = 0;
        }

        private void pictureBox2_MouseEnter(object sender, EventArgs e)
        {
            Exit.BackgroundImage = Ping_time.Properties.Resources.rojo1__2_;
        }

        private void pictureBox2_MouseLeave(object sender, EventArgs e)
        {
            Exit.BackgroundImage = Ping_time.Properties.Resources.rojo1__1_;
        }

        private void minimize_MouseEnter(object sender, EventArgs e)
        {
            minimize.BackgroundImage = Ping_time.Properties.Resources.Minimizar1;
        }

        private void minimize_MouseLeave(object sender, EventArgs e)
        {
            minimize.BackgroundImage = Ping_time.Properties.Resources.Minimizar;
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void minimize_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void applyToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            int a;

            try
            {
                a = int.Parse(NotifiPoint.Text);
            }
            catch (Exception)
            {
                return;
            }

            nP = int.Parse(NotifiPoint.Text);
            checkBox1.Text = "Notify when ping is less than " + nP + "ms";
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (chart)
            {
                if (cola.Count > 0)
                {
                    chart1.Series["ChartData"].Points.Clear();

                    foreach (long c in cola)
                    {
                        chart1.Series["ChartData"].Points.AddXY(totaltime, c);
                    }
                }

                LOG.BringToFront();
                chart = false;
            }
            else
            {
                chart1.BringToFront();
                chart = true;
            }
        }
    }
}
