using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
//using System.Threading;
//using System.Math;

namespace WindowsFormsApplication1
{
  
    public partial class MainFrm : Form
    {
        public MainFrm()
        {
            InitializeComponent();

           

           // frm_width = this.Width;
         //   frm_height = this.Height;

        }



        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {

                string FileName;
                int datalength=0;
                float dataA_avg = 0;
                float[] dataA;

                FileName = openFileDialog1.FileName;

                //openFileDialog1.FileName = "*.txt";                                 

                if (openFileDialog1.FilterIndex == 1)
                {

                    StreamReader sr = new StreamReader(FileName, Encoding.ASCII);
                    //  int nextChar = sr.Read();
                    //  nextChar = sr.Read();
                    //  nextChar -= 0x30;
                    // 全部读完 
                    string restOfStream = sr.ReadToEnd();
                    datalength = (restOfStream.Length - 6) / 6;
                    //  label4.Text = Convert.ToString(nextChar);
                    //处理数据,计算平均值
                    byte[] tmp = new byte[4];
                    dataA = new float[datalength];

                    for (int i = 0; i < datalength; i++)
                    {
                        tmp[0] = (byte)restOfStream[3 + i * 6];
                        tmp[1] = (byte)restOfStream[3 + i * 6 + 1];
                        tmp[2] = (byte)restOfStream[3 + i * 6 + 3];
                        tmp[3] = (byte)restOfStream[3 + i * 6 + 4];

                        for (int j = 0; j < 4; j++)
                        {

                            if (tmp[j] > 96 && tmp[j] < 103)
                                tmp[j] = (byte)((int)tmp[j] - 87);
                            else if (tmp[j] > 64 && tmp[j] < 71)
                                tmp[j] = (byte)((int)tmp[j] - 55);
                            else if (tmp[j] > 47 && tmp[j] < 58)
                                tmp[j] = (byte)((int)tmp[j] - 48);
                        }

                        dataA[i] = 0;
                        for (int j = 0; j < 4; j++)
                            dataA[i] = (dataA[i] * 0x10 + tmp[j]);

                        dataA_avg += dataA[i] / datalength;
                    }

                    sr.Dispose();

                }
                else 
                {
                    FileStream fs = new FileStream(FileName, FileMode.Open);
                    datalength = (int)(fs.Length-2)/2;
                    dataA = new float[datalength];
                    byte[] data = new byte[datalength*2];



                    fs.Seek(1, SeekOrigin.Begin);//跳过第一个字符
                    fs.Read(data, 0, datalength*2);


                    fs.Dispose();

                    for (int i = 0; i < datalength; i++)
                        dataA[i] = (float)(data[2 * i] * 0x100 + data[2 * i + 1]);                
                   

                    dataA_avg = 0;
                    for(int i=0;i<datalength;i++)
                        dataA_avg += dataA[i] / datalength;
                
                }

                for (int i = 0; i < datalength; i++)//归一处理
                    dataA[i] = (dataA[i] - dataA_avg) / dataA_avg;
                
                //保存数据
                WaterLeak.set_DataLength(datalength);
                WaterLeak.set_DataA(dataA);
                WaterLeak.set_DataA_Avg(dataA_avg);



                //画图形
                Graphics g;
                g = panel1.CreateGraphics();
                g.Clear(panel1.BackColor);
                Pen blackPen = new Pen(Color.Blue);
                int display_interval = datalength / (1110 - 25);
                blackPen.EndCap = LineCap.NoAnchor;
                int x1, x2, y1, y2;
                for (int i = 0; i < (1110 - 25 - 20); i++)
                {
                    x1 = 25 + i;
                    x2 = 25 + i + 1;
                    y1 = (int)(panel1.Height / 2 + 10 + dataA[i * display_interval] * 5 * dataA_avg);
                    y2 = (int)(panel1.Height / 2 + 10 + dataA[(i + 1) * display_interval] * 5 * dataA_avg);

                    g.DrawLine(blackPen, x1, y1, x2, y2);

                }

                blackPen.EndCap = LineCap.ArrowAnchor;
                x1 = 25; y1 = panel1.Height - 10; x2 = 25; y2 = 25;
                g.DrawLine(blackPen, x1, y1, x2, y2);
                x1 = 25; y1 = panel1.Height / 2 + 10; x2 = 1110; y2 = panel1.Height / 2 + 10;
                g.DrawLine(blackPen, x1, y1, x2, y2);
                g.DrawString("Y轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 10));
                g.DrawString("X轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(1115, 90));
                // g.DrawString("(0,0)", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 200));
                g.Dispose();

                //保存参数
                WaterLeak.set_dataA_factor((int)(5 * dataA_avg));
                WaterLeak.set_dataA_shift((int)(panel1.Height / 2 + 10));

                //开启状态使能
                vScrollBar1.Enabled = true;
                vScrollBar2.Enabled = true;
                button2.Enabled = true;
            } 


        }



        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }

        private void button2_Click(object sender, EventArgs e)
        {

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string FileName;
                int datalength = 0;
                float dataA_avg = 0;
                float[] dataA;



                    FileName = openFileDialog1.FileName;
                    //openFileDialog1.FileName = "*.txt"; 

                    if (openFileDialog1.FilterIndex == 1)
                    {

                    StreamReader sr = new StreamReader(FileName, Encoding.ASCII);
                    //  int nextChar = sr.Read();
                    //  nextChar = sr.Read();
                    //  nextChar -= 0x30;
                    // 全部读完 
                    string restOfStream = sr.ReadToEnd();
                    sr.Dispose();
                    datalength = (restOfStream.Length - 6) / 6;
                    //  label4.Text = Convert.ToString(nextChar);
                    //处理数据,计算平均值
                   // float dataA_avg = 0;
                    dataA = new float[datalength];
                    byte[] tmp = new byte[4];
                    for (int i = 0; i < datalength; i++)
                    {
                        tmp[0] = (byte)restOfStream[3 + i * 6];
                        tmp[1] = (byte)restOfStream[3 + i * 6 + 1];
                        tmp[2] = (byte)restOfStream[3 + i * 6 + 3];
                        tmp[3] = (byte)restOfStream[3 + i * 6 + 4];

                        for (int j = 0; j < 4; j++)
                        {
                            if (tmp[j] > 96 && tmp[j] < 103)
                                tmp[j] = (byte)((int)tmp[j] - 87);
                            else if (tmp[j] > 64 && tmp[j] < 71)
                                tmp[j] = (byte)((int)tmp[j] - 55);
                            else if (tmp[j] > 47 && tmp[j] < 58)
                                tmp[j] = (byte)((int)tmp[j] - 48);
                        }

                        dataA[i] = 0;
                        for (int j = 0; j < 4; j++)
                            dataA[i] = (dataA[i] * 0x10 + tmp[j]);

                        dataA_avg += dataA[i] / datalength;
                    }
                }
                else
                {

                    FileStream fs = new FileStream(FileName, FileMode.Open);
                    datalength = (int)(fs.Length - 2) / 2;
                    dataA = new float[datalength];
                    byte[] data = new byte[datalength * 2];



                    fs.Seek(1, SeekOrigin.Begin);//跳过第一个字符
                    fs.Read(data, 0, datalength * 2);


                    fs.Dispose();

                    for (int i = 0; i < datalength; i++)
                        dataA[i] = (float)(data[2 * i] * 0x100 + data[2 * i + 1]);


                    dataA_avg = 0;
                    for (int i = 0; i < datalength; i++)
                        dataA_avg += dataA[i] / datalength;
                
                }
                    
                 for (int i = 0; i < datalength; i++)
                    dataA[i] = (dataA[i] - dataA_avg) / dataA_avg;

                //保存数据
                WaterLeak.set_DataLength(datalength);
                WaterLeak.set_DataB(dataA);
                WaterLeak.set_DataB_Avg(dataA_avg);


                //画图形
                Graphics g;
                g = panel2.CreateGraphics();
                g.Clear(panel2.BackColor);
                Pen blackPen = new Pen(Color.Blue);
                int display_interval = datalength/(1110-25);
                blackPen.EndCap = LineCap.NoAnchor;
                int x1, x2, y1, y2;
                for (int i = 0; i < (1110 - 25-20); i++)
                {
                    x1 = 25 + i;
                    x2 = 25 + i+1;
                    y1 = (int)(panel2.Height/2+10 + dataA[i * display_interval] * 5 * dataA_avg);
                    y2 = (int)(panel2.Height/ 2+10 + dataA[(i + 1) * display_interval] * 5 * dataA_avg);                   
                    
                    g.DrawLine(blackPen, x1, y1, x2, y2);
                
                }

                blackPen.EndCap = LineCap.ArrowAnchor;
                x1 = 25; y1 = panel2.Height-10 ; x2 = 25; y2 = 25;
                g.DrawLine(blackPen, x1, y1, x2, y2);
                x1 = 25; y1 = panel2.Height / 2 + 10; x2 = 1110; y2 = panel2.Height/2+10;
                g.DrawLine(blackPen, x1, y1, x2, y2);
                g.DrawString("Y轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 10));
                g.DrawString("X轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(1115, 90));
                // g.DrawString("(0,0)", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 200));
                g.Dispose();

                //保存参数
                WaterLeak.set_dataB_factor((int)(5 * dataA_avg));
                WaterLeak.set_dataB_shift((int)(panel2.Height / 2 + 10));

                //开启状态使能
                vScrollBar3.Enabled = true;
                vScrollBar4.Enabled = true;
                button3.Enabled = true;
            } 
        }





        private void button3_Click(object sender, EventArgs e)
        {


            Graphics g;
            Pen blackPen = new Pen(Color.Black);
            blackPen.EndCap = LineCap.ArrowAnchor;

            //处理结果坐标
            g = panel3.CreateGraphics();
            g.Clear(panel3.BackColor);
            int x1 = 25, y1 = 200, x2 = 25, y2 = 25;
            g.DrawLine(blackPen, x1, y1, x2, y2);
            x1 = 25; y1 = 100; x2 = 1110; y2 = 100;
            g.DrawLine(blackPen, x1, y1, x2, y2);
            g.DrawString("Y轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 10));
            g.DrawString("X轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(1115, 90));
            //   g.DrawString("(0,0)", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 200));
            
            //读取数据
            int datalength = WaterLeak.get_DataLength();
            float[] dataA = WaterLeak.get_DataA();
            float[] dataB = WaterLeak.get_DataB();
            float dataA_avg = WaterLeak.get_DataA_Avg();
            float dataB_avg = WaterLeak.get_DataB_Avg();
            /*
            
            //处理数据，采用LMS算法
            //初始化
            int M=1000;
            float mu = (float)0.1;
            float[] en = new float[datalength];
            for (int i = 0; i < datalength; i++)
                en[i] = 0;
            double[] w = new double[M];
            for (int i = 0; i < M; i++)
                w[i] = 0;
            float y;
            //迭代运算
            for (int k = M - 1; k < datalength; k++)
            { 
                y=0;
                for (int i = 0; i < M; i++)
                    y += (float)(w[i]) * dataB[k - i];
                if (double.IsNaN(y)) y = 0;
                en[k] =dataA[k] - y;

                for (int i = 0; i < M; i++)
                {
                    w[i] = w[i] + 2 * mu * en[k] * dataB[k - i];
                    if (double.IsNaN(w[i])) w[i] = 0;
                    // w[i] = Math.Round(w[i], 6);
                }

                progressBar1.Value = (k-M) * 100 / (datalength - M-1);
               
                Application.DoEvents();

            }
             

           float w_avg;
           w_avg = 0;
            for (int i = 0; i < M; i++)
               w_avg += (float)w[i];

           w_avg /= M;
          // w_avg = Math.Round(w_avg, 6);

          // w[0] = Math.Round(w[0], 10);
           textBox1.Text = w[0].ToString("G");
            */
            //处理数据，采用均方差算法
            int j = 0;
            double MINC = 0;
            int bias = 5000;
            int w = 5000;
            double[] C = new double[2*bias];
            for (int i = -bias; i < bias; i++)
            {
                C[i + bias] = 0;
                for (int k = w; k < datalength - w; k++)
                    C[i + bias] += Math.Abs(dataA[k + i] - dataB[k]);
                if (i == -bias) MINC = C[0];
                else if (C[bias + i] < MINC)
                {
                    j = i;
                    MINC = C[bias + i];
                }
                Application.DoEvents();
                progressBar1.Value = (i + bias) * 50 / bias;
                         
            }
            progressBar1.Value += 1;

            double w_avg;
            w_avg = 0;
            for (int i = 0; i < 2*bias; i++)
                w_avg += C[i]/(2*bias);

            for (int i = 0; i < 2 * bias; i++)
                C[i] = (C[i]-w_avg)  / w_avg;

          
   

            int display_interval = 2*bias / (1110 - 25);
            blackPen.EndCap = LineCap.NoAnchor;
            blackPen.Color = Color.Blue;
            blackPen.DashStyle = DashStyle.Dot;
            g.DrawLine(blackPen, 25 + (1110 - 25) / 2, 0, 25 + (1110 - 25) / 2, panel3.Height);
            blackPen.Color = Color.Red;
            blackPen.DashStyle = DashStyle.Dot;
            g.DrawLine(blackPen, 25 + (1110 - 25) / 2 + j, 0, 25 + (1110 - 25) / 2 + j, panel3.Height);
            blackPen.Color = Color.Black;
            blackPen.DashStyle = DashStyle.Solid;

            //int x1, x2, y1, y2;
            for (int i = 0; i < (1110 - 25 - 20); i++)
            {
                x1 = 25 + i;
                x2 = 25 + i + 1;
                y1 = (int)(100+C[bias-(1110 - 25) / 2+i]*500 );
                y2 = (int)(100 + C[bias - (1110 - 25) / 2 + i+1] * 500);

                g.DrawLine(blackPen, x1, y1, x2, y2);

            }

            //计算漏水点位置
            float positon = (WaterLeak.get_PipelineLength() + WaterLeak.get_WaveNumber() * j / (1000 * WaterLeak.get_CaptureRate())) / 2;
            g.DrawString("漏水点距离A探头" + positon.ToString() + "米!", new Font("宋体 ", 14f), Brushes.Red, new PointF(900, 20));

            //显示标注数据
            if(j>0)
                g.DrawString("A探头滞后B探头" + j.ToString("G") + "个基点!", new Font("宋体 ", 14f), Brushes.Red, new PointF(100, 20));
            //显示标注数据
            if (j < 0)
                g.DrawString("A探头超前B探头" + (-j).ToString() + "个基点!", new Font("宋体 ", 14f), Brushes.Red, new PointF(100, 20));
            if(j==0)
            g.DrawString("A探头与B探头无偏移!" , new Font("宋体 ", 14f), Brushes.Red, new PointF(100, 20));
            

            //保存参数
            WaterLeak.set_dataC_factor(500);
            WaterLeak.set_dataC_shift(100);
            WaterLeak.set_Bias(bias);
            WaterLeak.set_Min_Position(j);
            WaterLeak.set_Min_Value(MINC);
            WaterLeak.set_DataC(C);

            //开启状态使能
            vScrollBar5.Enabled = true;
            vScrollBar6.Enabled = true;

        }

        private void vScrollBar1_ValueChanged(object sender, EventArgs e)
        {
            //读取数据
            int datalength = WaterLeak.get_DataLength();
            float[] dataA = WaterLeak.get_DataA();
            float dataA_avg = WaterLeak.get_DataA_Avg();
            int factor = WaterLeak.get_dataA_factor();
            int shift = WaterLeak.get_dataA_shift();

            //画图形
            Graphics g;
            g = panel1.CreateGraphics();
            g.Clear(panel1.BackColor);
            Pen blackPen = new Pen(Color.Blue);
            int display_interval = datalength / (1110 - 25);
            blackPen.EndCap = LineCap.NoAnchor;
            int x1, x2, y1, y2;
            for (int i = 0; i < (1110 - 25 - 20); i++)
            {
                x1 = 25 + i;
                x2 = 25 + i + 1;
                y1 = (int)(shift + (vScrollBar1.Value -50)*10 + dataA[i * display_interval] *(factor+(50-vScrollBar2.Value)*1000));
                y2 = (int)(shift + (vScrollBar1.Value -50)*10 + dataA[(i + 1) * display_interval] * (factor+(50-vScrollBar2.Value)*1000));

                g.DrawLine(blackPen, x1, y1, x2, y2);

            }

            blackPen.EndCap = LineCap.ArrowAnchor;
            x1 = 25; y1 = panel1.Height - 10; x2 = 25; y2 = 25;
            g.DrawLine(blackPen, x1, y1, x2, y2);
            x1 = 25; y1 = panel1.Height / 2 + 10; x2 = 1110; y2 = panel1.Height / 2 + 10;
            g.DrawLine(blackPen, x1, y1, x2, y2);
            g.DrawString("Y轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 10));
            g.DrawString("X轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(1115, 90));
            // g.DrawString("(0,0)", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 200));
            g.Dispose();


        }

        private void vScrollBar2_ValueChanged(object sender, EventArgs e)
        {
            //读取数据
            int datalength = WaterLeak.get_DataLength();
            float[] dataA = WaterLeak.get_DataA();
           // float dataA_avg = WaterLeak.get_DataA_Avg();
            int factor = WaterLeak.get_dataA_factor();
            int shift = WaterLeak.get_dataA_shift();

            //画图形
            Graphics g;
            g = panel1.CreateGraphics();
            g.Clear(panel1.BackColor);
            Pen blackPen = new Pen(Color.Blue);
            int display_interval = datalength / (1110 - 25);
            blackPen.EndCap = LineCap.NoAnchor;
            int x1, x2, y1, y2;
            for (int i = 0; i < (1110 - 25 - 20); i++)
            {
                x1 = 25 + i;
                x2 = 25 + i + 1;
                y1 = (int)(shift + (vScrollBar1.Value - 50) * 10 + dataA[i * display_interval] * (factor + (50-vScrollBar2.Value) * 1000));
                y2 = (int)(shift + (vScrollBar1.Value - 50) * 10 + dataA[(i + 1) * display_interval] * (factor + (50-vScrollBar2.Value) * 1000));

                g.DrawLine(blackPen, x1, y1, x2, y2);

            }

            blackPen.EndCap = LineCap.ArrowAnchor;
            x1 = 25; y1 = panel1.Height - 10; x2 = 25; y2 = 25;
            g.DrawLine(blackPen, x1, y1, x2, y2);
            x1 = 25; y1 = panel1.Height / 2 + 10; x2 = 1110; y2 = panel1.Height / 2 + 10;
            g.DrawLine(blackPen, x1, y1, x2, y2);
            g.DrawString("Y轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 10));
            g.DrawString("X轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(1115, 90));
            // g.DrawString("(0,0)", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 200));
            g.Dispose();
        }

        private void vScrollBar4_ValueChanged(object sender, EventArgs e)
        {

            //读取数据
            int datalength = WaterLeak.get_DataLength();
            float[] dataA = WaterLeak.get_DataB();
           // float dataA_avg = WaterLeak.get_DataB_Avg();
            int factor = WaterLeak.get_dataB_factor();
            int shift = WaterLeak.get_dataB_shift();

            //画图形
            Graphics g;
            g = panel2.CreateGraphics();
            g.Clear(panel2.BackColor);
            Pen blackPen = new Pen(Color.Blue);
            int display_interval = datalength / (1110 - 25);
            blackPen.EndCap = LineCap.NoAnchor;
            int x1, x2, y1, y2;
            for (int i = 0; i < (1110 - 25 - 20); i++)
            {
                x1 = 25 + i;
                x2 = 25 + i + 1;
                y1 = (int)(shift + (vScrollBar4.Value - 50) * 10 + dataA[i * display_interval] * (factor + (50 - vScrollBar3.Value) * 1000));
                y2 = (int)(shift + (vScrollBar4.Value - 50) * 10 + dataA[(i + 1) * display_interval] * (factor + (50 - vScrollBar3.Value) * 1000));

                g.DrawLine(blackPen, x1, y1, x2, y2);

            }

            blackPen.EndCap = LineCap.ArrowAnchor;
            x1 = 25; y1 = panel2.Height - 10; x2 = 25; y2 = 25;
            g.DrawLine(blackPen, x1, y1, x2, y2);
            x1 = 25; y1 = panel2.Height / 2 + 10; x2 = 1110; y2 = panel2.Height / 2 + 10;
            g.DrawLine(blackPen, x1, y1, x2, y2);
            g.DrawString("Y轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 10));
            g.DrawString("X轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(1115, 90));
            // g.DrawString("(0,0)", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 200));
            g.Dispose();


        }

        private void vScrollBar3_ValueChanged(object sender, EventArgs e)
        {
            //读取数据
            int datalength = WaterLeak.get_DataLength();
            float[] dataA = WaterLeak.get_DataB();
            float dataA_avg = WaterLeak.get_DataB_Avg();
            int factor = WaterLeak.get_dataB_factor();
            int shift = WaterLeak.get_dataB_shift();

            //画图形
            Graphics g;
            g = panel2.CreateGraphics();
            g.Clear(panel2.BackColor);
            Pen blackPen = new Pen(Color.Blue);
            int display_interval = datalength / (1110 - 25);
            blackPen.EndCap = LineCap.NoAnchor;
            int x1, x2, y1, y2;
            for (int i = 0; i < (1110 - 25 - 20); i++)
            {
                x1 = 25 + i;
                x2 = 25 + i + 1;
                y1 = (int)(shift + (vScrollBar4.Value - 50) * 10 + dataA[i * display_interval] * (factor + (50 - vScrollBar3.Value) * 1000));
                y2 = (int)(shift + (vScrollBar4.Value - 50) * 10 + dataA[(i + 1) * display_interval] * (factor + (50 - vScrollBar3.Value) * 1000));

                g.DrawLine(blackPen, x1, y1, x2, y2);

            }

            blackPen.EndCap = LineCap.ArrowAnchor;
            x1 = 25; y1 = panel2.Height - 10; x2 = 25; y2 = 25;
            g.DrawLine(blackPen, x1, y1, x2, y2);
            x1 = 25; y1 = panel2.Height / 2 + 10; x2 = 1110; y2 = panel2.Height / 2 + 10;
            g.DrawLine(blackPen, x1, y1, x2, y2);
            g.DrawString("Y轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 10));
            g.DrawString("X轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(1115, 90));
            // g.DrawString("(0,0)", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 200));
            g.Dispose();
        }

        private void vScrollBar6_ValueChanged(object sender, EventArgs e)
        {
            //读取数据
            int bias = WaterLeak.get_dataC_Bias();
            double[] C = new double[2*bias];
            C = WaterLeak.get_DataC();
            int factor = WaterLeak.get_dataC_factor();
            int shift = WaterLeak.get_dataC_shift();
            int j = WaterLeak.get_Min_Position();
            double MINC = WaterLeak.get_Min_Value();


            //画图形
            Graphics g;
            g = panel3.CreateGraphics();
            g.Clear(panel3.BackColor);
            Pen blackPen = new Pen(Color.Black);

            //int display_interval = 2 * bias / (1110 - 25);
            blackPen.EndCap = LineCap.NoAnchor;
            blackPen.Color = Color.Blue;
            blackPen.DashStyle = DashStyle.Dot;
            g.DrawLine(blackPen, 25 + (1110 - 25) / 2, 0, 25 + (1110 - 25) / 2, panel3.Height);
            blackPen.Color = Color.Red;
            blackPen.DashStyle = DashStyle.Dot;
            g.DrawLine(blackPen, 25 + (1110 - 25) / 2 + j, 0, 25 + (1110 - 25) / 2 + j, panel3.Height);
            blackPen.Color = Color.Black;
            blackPen.DashStyle = DashStyle.Solid;

            int x1, x2, y1, y2;
            for (int i = 0; i < (1110 - 25 - 20); i++)
            {
                x1 = 25 + i;
                x2 = 25 + i + 1;
                y1 = (int)(shift + (vScrollBar6.Value - 50) * 10 + C[bias - (1110 - 25) / 2 + i] * (factor + (50 - vScrollBar5.Value) * 100));
                y2 = (int)(shift + (vScrollBar6.Value - 50) * 10 + C[bias - (1110 - 25) / 2 + i + 1] * (factor + (50 - vScrollBar5.Value) * 100));
                // if (y1 < 0) y1 = 0;
                // if (y2 < 0) y2 = 0;           

                g.DrawLine(blackPen, x1, y1, x2, y2);

            }



            //处理结果坐标

            blackPen.EndCap = LineCap.ArrowAnchor;
            x1 = 25; y1 = 200; x2 = 25; y2 = 25;
            g.DrawLine(blackPen, x1, y1, x2, y2);
            x1 = 25; y1 = 100; x2 = 1110; y2 = 100;
            g.DrawLine(blackPen, x1, y1, x2, y2);
            g.DrawString("Y轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 10));
            g.DrawString("X轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(1115, 90));
            //   g.DrawString("(0,0)", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 200));

            //计算漏水点位置
            float positon = (WaterLeak.get_PipelineLength() + WaterLeak.get_WaveNumber() * j / (1000 * WaterLeak.get_CaptureRate())) / 2;
            g.DrawString("漏水点距离A探头" + positon.ToString() + "米!", new Font("宋体 ", 14f), Brushes.Red, new PointF(900, 20));

            //显示标注数据
            if (j > 0)
                g.DrawString("A探头滞后B探头" + j.ToString("G") + "个基点!", new Font("宋体 ", 14f), Brushes.Red, new PointF(100, 20));
            //显示标注数据
            if (j < 0)
                g.DrawString("A探头超前B探头" + (-j).ToString() + "个基点!", new Font("宋体 ", 14f), Brushes.Red, new PointF(100, 20));
            if (j == 0)
                g.DrawString("A探头与B探头无偏移!", new Font("宋体 ", 14f), Brushes.Red, new PointF(100, 20));

            g.Dispose(); 
        }

        private void vScrollBar5_ValueChanged(object sender, EventArgs e)
        {
            //读取数据
            int bias = WaterLeak.get_dataC_Bias();
            double[] C = new double[2 * bias];
            C = WaterLeak.get_DataC();
            int factor = WaterLeak.get_dataC_factor();
            int shift = WaterLeak.get_dataC_shift();
            int j = WaterLeak.get_Min_Position();
            double MINC = WaterLeak.get_Min_Value();


            //画图形
            Graphics g;
            g = panel3.CreateGraphics();
            g.Clear(panel3.BackColor);
            Pen blackPen = new Pen(Color.Black);

            //int display_interval = 2 * bias / (1110 - 25);
            blackPen.EndCap = LineCap.NoAnchor;
            blackPen.Color = Color.Blue;
            blackPen.DashStyle = DashStyle.Dot;
            g.DrawLine(blackPen, 25 + (1110 - 25) / 2, 0, 25 + (1110 - 25) / 2, panel3.Height);
            blackPen.Color = Color.Red;
            blackPen.DashStyle = DashStyle.Dot;
            g.DrawLine(blackPen, 25 + (1110 - 25) / 2 + j, 0, 25 + (1110 - 25) / 2 + j, panel3.Height);
            blackPen.Color = Color.Black;
            blackPen.DashStyle = DashStyle.Solid;

            int x1, x2, y1, y2;
            for (int i = 0; i < (1110 - 25 - 20); i++)
            {
                x1 = 25 + i;
                x2 = 25 + i + 1;
                y1 = (int)(shift + (vScrollBar6.Value - 50) * 10 + C[bias - (1110 - 25) / 2 + i] * (factor + (50 - vScrollBar5.Value) * 100));
                y2 = (int)(shift + (vScrollBar6.Value - 50) * 10 + C[bias - (1110 - 25) / 2 + i + 1] * (factor + (50 - vScrollBar5.Value) * 100));
                // if (y1 < 0) y1 = 0;
                // if (y2 < 0) y2 = 0;           

                g.DrawLine(blackPen, x1, y1, x2, y2);

            }



            //处理结果坐标

            blackPen.EndCap = LineCap.ArrowAnchor;
            x1 = 25; y1 = 200; x2 = 25; y2 = 25;
            g.DrawLine(blackPen, x1, y1, x2, y2);
            x1 = 25; y1 = 100; x2 = 1110; y2 = 100;
            g.DrawLine(blackPen, x1, y1, x2, y2);
            g.DrawString("Y轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 10));
            g.DrawString("X轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(1115, 90));
            //   g.DrawString("(0,0)", new Font("宋体 ", 10f), Brushes.Black, new PointF(10, 200));

            //计算漏水点位置
            float positon = (WaterLeak.get_PipelineLength() + WaterLeak.get_WaveNumber() * j / (1000 * WaterLeak.get_CaptureRate())) / 2;
            g.DrawString("漏水点距离A探头" + positon.ToString() + "米!", new Font("宋体 ", 14f), Brushes.Red, new PointF(900, 20));
            
            //显示标注数据
            if (j > 0)
                g.DrawString("A探头滞后B探头" + j.ToString("G") + "个基点!", new Font("宋体 ", 14f), Brushes.Red, new PointF(100, 20));
            //显示标注数据
            if (j < 0)
                g.DrawString("A探头超前B探头" + (-j).ToString() + "个基点!", new Font("宋体 ", 14f), Brushes.Red, new PointF(100, 20));
            if (j == 0)
                g.DrawString("A探头与B探头无偏移!", new Font("宋体 ", 14f), Brushes.Red, new PointF(100, 20));
            g.Dispose(); 
        }

        private void button4_Click(object sender, EventArgs e)
        {
            WaterLeak.set_PipelineLength(Convert.ToSingle(textBox1.Text));
            WaterLeak.set_WaveNumber(Convert.ToInt32(textBox2.Text));
            WaterLeak.set_CaptureRate(Convert.ToInt32(textBox3.Text));
            MessageBox.Show("参数保存成功！","消息");
        }

        private void 采集探头数据ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 frm = new Form2();
            frm.Show();
 
        }
        }

    public class WaterLeak
    {

        public WaterLeak()//int wn, int pll, int ABd, int length,short *dataA,short *dataB, float DAA,float DBA)
        {
         //TODO:添加构造函数逻辑
      
        
        }

          private static int _WaveNumber=1000;//管材波数
          private static float _PipelineLength=1000;//管线长度
          private static int _CaptureRate=5;//采样频率

        //   private static int _AtoB_Deviation;//AB两个设备的偏移系数
        private static int _DataLength;
        private static float[] _DataA;
        private static float[] _DataB; 
        private static float _DataA_Avg;
        private static float _DataB_Avg;

        private static int _dataA_shift;
        private static int _dataA_factor;

        private static int _dataB_shift;
        private static int _dataB_factor;

        private static int _Bias;
        private static int _Min_Position;
        private static double _Min_Value;
        private static int _dataC_shift;
        private static int _dataC_factor;
        private static double[] _DataC;

        public static void set_WaveNumber(int wn)
        {
            _WaveNumber = wn;
        }
        public static int get_WaveNumber()
        {
            return _WaveNumber;
        }
        public static void set_PipelineLength(float pll)
        {
            _PipelineLength = pll;
        }
        public static float get_PipelineLength()
        {
            return _PipelineLength;
        }
        public static void set_CaptureRate(int cr)
        {
            _CaptureRate = cr;
        }
        public static int get_CaptureRate()
        {
            return _CaptureRate;
        }


        public static void set_DataLength(int length)
        {
            _DataLength = length;
        }
        public static int get_DataLength()
        {
            return _DataLength;
        }
        public static void set_DataA(float[] dataA)
        {
            _DataA = new float[_DataLength];
            _DataA = dataA;

        }
        public static float[] get_DataA()
        {
            return _DataA;
        }

        public static void set_DataB(float[] dataB)
        {
            _DataB = new float[_DataLength];
            _DataB = dataB;

        }
        public static float[] get_DataB()
        {
            return _DataB;
        }
        public static void set_DataA_Avg(float DAA)
        {
            _DataA_Avg = DAA;
        }
        public static float get_DataA_Avg()
        {
            return _DataA_Avg;
        }
        public static void set_DataB_Avg(float DBA)
        {
            _DataB_Avg = DBA;
        }
        public static float get_DataB_Avg()
        {
            return _DataB_Avg;
        }
        /// <summary>
        /// /////////////////////////
        /// </summary>
        /// <param name="shift"></param>
        public static void set_dataA_shift(int shift)
        {
            _dataA_shift = shift;
        }
        public static int get_dataA_shift()
        {
            return _dataA_shift;
        }
        public static void set_dataA_factor(int factor)
        {
            _dataA_factor = factor;
        }
        public static int get_dataA_factor()
        {
            return _dataA_factor;
        }
        /// <summary>
        /// ///////
        /// </summary>
        /// <param name="shift"></param>
        public static void set_dataB_shift(int shift)
        {
            _dataB_shift = shift;
        }
        public static int get_dataB_shift()
        {
            return _dataB_shift;
        }
        public static void set_dataB_factor(int factor)
        {
            _dataB_factor = factor;
        }
        public static int get_dataB_factor()
        {
            return _dataB_factor;
        }

        /// <summary>
        /// ///////
        /// </summary>
        /// <param name="shift"></param>
        public static void set_dataC_shift(int shift)
        {
            _dataC_shift = shift;
        }
        public static int get_dataC_shift()
        {
            return _dataC_shift;
        }
        public static void set_dataC_factor(int factor)
        {
            _dataC_factor = factor;
        }
        public static int get_dataC_factor()
        {
            return _dataC_factor;
        }
        public static void set_Bias(int bias)
        {
           _Bias = bias;
        }
        public static int get_dataC_Bias()
        {
            return _Bias;
        }
        public static void set_Min_Position(int min_pos)
        {
            _Min_Position = min_pos;
        }
        public static int get_Min_Position()
        {
            return _Min_Position;
        }
        public static void set_Min_Value(double min_value)
        {
            _Min_Value = min_value;
        }
        public static double get_Min_Value()
        {
            return _Min_Value;
        }
        public static void set_DataC(double[] dataC)
        {
            _DataC = new double[2*_Bias];
            _DataC = dataC;

        }
        public static double[] get_DataC()
        {
            return _DataC;
        }

    
    }





}
