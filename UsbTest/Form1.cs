using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading;//多线程空间
using System.IO;//文件流
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;//引用相关的命名空间
namespace UsbTest
{
    public partial class UsbSpeed : Form
    {
        private string iFilapath;//=new string();
        private StringBuilder UartReStr = new StringBuilder();    //为了避免在接收处理函数中反复调用，依然声明为一个全局变量
        private long receive_count = 0; //接收字节计数
        private long send_count = 0;    //发送字节计数
        UartCom iUartCom = new UartCom();
        //10 k款冲区
        byte[] received_buf = new byte[1024 * 50];    //一次文件大小
        int iReFileSum = 0;
        long iUsbLen = 0;//要接收的数据大小
        long iUsbLenPre = 0;//已经接收的数据大小
        bool iUsbReady = false;//是否已经接收完成
        int iUartSum = 0;//款冲区数据大小
        bool iSendFileFlag = false;
        // byte LastByte = 0;//最后一个字节
        //ThreadStart ts1;// = new ThreadStart(ShowUartDate);
        //Thread t1;//= new Thread(ts1);

        //发送命令线程
        ThreadStart ThSend;// = new ThreadStart(ShowUartDate);
        Thread ThSend1;//= new Thread(ts1);

        ThreadStart ThSend2;// = new ThreadStart(ShowUartDate);
        Thread ThThread2;//= new Thread(ts1);

        byte[] SendBuffer = new byte[1024 * 4];    //发送款冲区
                                                   // int SendLen = 0;//发送数据大小
        byte[] RecBuffer = new byte[1024 * 20];    //10k 命令接收款冲 够了
        int RecLen = 0;//接收数据大小
                       //  bool SendFlag = false;//是否正在发送命令


        //ThreadStart ThUart;//= new ThreadStart(SerialPortRead);
        //Thread ThUart1 = new Thread(ThUart);


        public UsbSpeed()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //获取电脑当前可用串口并添加到选项列表中
            comboBox1.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
            comboBox1.SelectedIndex = System.IO.Ports.SerialPort.GetPortNames().Length - 1;//设定选择项

            //创建 多线程 
            ThreadStart ThUart = new ThreadStart(SerialPortRead);
            Thread ThUart1 = new Thread(ThUart);
            ThUart1.IsBackground = true;//设置为后台线程  关闭主线程时同时也关闭
            ThUart1.Priority = ThreadPriority.Highest;
            ThUart1.Start();

            send_count = 0;
            receive_count = 0;
            labelRx.Text = "Rx:" + receive_count.ToString() + "";
            labelTx.Text = "Tx:" + send_count.ToString() + "";      //刷新界面

            radioButton1.Select();
        }


        //多线程 读取串口数据
        private void SerialPortRead()
        {
            bool UartValue = true;

            while (UartValue)
            {
                if (serialPort1.IsOpen == false)//未打开串口
                {
                    Thread.Sleep(1000);
                    continue;
                }
                try
                {

                    iUartSum = serialPort1.BytesToRead;      //获取接收缓冲区中的字节数
                    if (iUartSum <= 0)
                    {
                       // Thread.Sleep(1);
                        Thread.Sleep(1000);
                        continue;
                    }


                    if (iSendFileFlag == true)
                    {
                        Thread.Sleep(1000);
                        continue;//是发送的文件 
                    }


                    //iStartPos 是偏移量
                    serialPort1.Read(RecBuffer, 0, iUartSum);
                    UartReStr.Clear();
                    UartReStr.Clear();
                    receive_count += iUartSum;//加上单次的大小


                    //byte[] received_buf = new byte[1024 * 50];    //串口款冲区
                    //long iUsbLen = 0;//要接收的数据大小
                    //long iUsbLenPre = 0;//已经接收的数据大小
                    if (RecBuffer[0] == 0x55 && RecBuffer[1] == 0xAA)
                    {
                        iUsbLenPre = 0;
                        iUsbReady = false;
                        iUsbLen = RecBuffer[2] * 256 + RecBuffer[3];
                        for (int i = 4; i < iUartSum; i++)
                        {
                            received_buf[iReFileSum++] = RecBuffer[i];
                            if (radioButton2.Checked)//ASCII
                            {
                                UartReStr.Append((char)RecBuffer[i]);
                            }
                            else
                            {
                                UartReStr.Append(RecBuffer[i].ToString("X2") + ' ');
                            }
                        }

                    }//帧头
                    else
                    {
                        for (int i = 0; i < iUartSum; i++)
                        {
                            received_buf[iReFileSum++] = RecBuffer[i];
                            if (radioButton2.Checked)//ASCII
                            {
                                UartReStr.Append((char)RecBuffer[i]);
                            }
                            else
                            {
                                UartReStr.Append(RecBuffer[i].ToString("X2") + ' ');
                            }
                        }
                    }
                    iUsbLenPre += iUartSum;


                    if (iUsbLenPre >= iUsbLen)
                    {
                        iUsbReady = true;//接收完成
                    }

                    if (radioButton2.Checked)//ASCII
                    {
                        UartReStr.Append("\r\n");
                    }

                    ////实时刷新  效果还可以
                    Invoke((EventHandler)(delegate
                    // BeginInvoke(new Action(()=>
                    {

                        // textBox_receive.AppendText(AllUartReStr.ToString());
                        textBox_receive.AppendText(UartReStr.ToString());
                        labelRx.Text = "Rx:" + receive_count.ToString() + "";
                        // labelTx.Text = "Tx:" + send_count.ToString() + "";      //刷新界面
                    }));

                    Thread.Sleep(20);
                }
                catch (Exception ex)
                {
                    //响铃并显示异常给用户
                    System.Media.SystemSounds.Beep.Play();
                    MessageBox.Show(ex.Message);

                }

            }


            return;
        }

        //多线程 发送数据
        private void SerialPortSend(byte[] iSendBuffer, int iSendLen)
        {
            bool UartValue = true;
            int iCount = 0;
            RecLen = 0;
            if (serialPort1.IsOpen == false) return;

            //SendFlag = true;
            //iUartCom.SendDate(SendBuffer, SendLen);//发送命令
            RecLen = serialPort1.BytesToRead;
            serialPort1.Read(received_buf, 0, RecLen);//清空款存


            //labelTx.Text = "Tx:" + send_count.ToString() + "";      //刷新界面

            // byte[] received_buf = new byte[1024 * 50];    //串口款冲区
            iUsbLen = 0;//要接收的数据大小
            iUsbLenPre = 0;//已经接收的数据大小
            iUsbReady = false;//是否已经接收完成
            iReFileSum = 0;
            iUartCom.SendDate(iSendBuffer, iSendLen);//发送命令
            DateTime t1 = DateTime.Now;
            while (UartValue)
            {
                iCount++;
                //Thread.Sleep(1);
                //if (iCount > 1000) break;//2s 后退出循环
                DateTime t2 = DateTime.Now;
                TimeSpan ts = t2 - t1;
                if (ts.Seconds > 20) break;//大于2s 超时 

                if (serialPort1.BytesToRead <= 0) continue;


                if (serialPort1.BytesToRead > 0)
                {
                    iCount = 0;
                    iUsbReady = false;
                    iUartSum = serialPort1.BytesToRead;      //获取接收缓冲区中的字节数
                    receive_count += iUartSum;
                    serialPort1.Read(RecBuffer, 0, iUartSum);
                    if (RecBuffer[0] == 0x55 && RecBuffer[1] == 0xAA)
                    {
                        iUsbLenPre = 0;
                        iUsbReady = false;
                        iUsbLen = RecBuffer[2] * 256 + RecBuffer[3];
                        for (int i = 4; i < iUartSum; i++)
                        {
                            received_buf[iReFileSum++] = RecBuffer[i];
                        }

                    }
                    else
                    {
                        for (int i = 0; i < iUartSum; i++)
                        {
                            received_buf[iReFileSum++] = RecBuffer[i];
                        }
                    }
                    iUsbLenPre += iUartSum;
                    if (iUsbLenPre >= iUsbLen)//接收完成
                    {
                        iUsbReady = true;//接收完成
                    }
                }

                if (iUsbReady == true)//接收到完整数据了
                {
                    //  receive_count += iReFileSum;
                    break;
                }
            }

            return;
        }


        //打开串口
        private void button2_Click(object sender, EventArgs e)
        {
            //打开 关闭 串口
            try
            {
                //将可能产生异常的代码放置在try块中
                //根据当前串口属性来判断是否打开
                if (serialPort1.IsOpen)
                {


                    serialPort1.Close();    //关闭串口
                    button2.Text = "Open";
                    // button1.BackColor = Color.ForestGreen;
                    comboBox1.Enabled = true;
                    //label6.Text = "串口已关闭";
                    //label6.ForeColor = Color.Red;
                    button2.Enabled = true;        //失能发送按钮
                    

                    iUartCom.serialPort1 = serialPort1;

                }
                else
                {
                    //串口已经处于关闭状态，则设置好串口属性后打开
                    radioButton1.Select();//默认选中
                    comboBox1.Enabled = false;
                    serialPort1.PortName = comboBox1.Text;
                    serialPort1.BaudRate = Convert.ToInt32("460800");//波特率
                    serialPort1.DataBits = Convert.ToInt16("8");//数据位
                    serialPort1.Parity = System.IO.Ports.Parity.None;//校验位
                    serialPort1.StopBits = System.IO.Ports.StopBits.One;//停止位

                    serialPort1.Open();     //打开串口
                    button2.Text = "Close";
                    button2.Enabled = true;        //使能发送按钮
                    iUartCom.serialPort1 = serialPort1;
                }
            }
            catch (Exception ex)
            {
                //捕获可能发生的异常并进行处理

                //捕获到异常，创建一个新的对象，之前的不可以再用
                serialPort1 = new System.IO.Ports.SerialPort();
                //刷新COM口选项
                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
                //响铃并显示异常给用户
                System.Media.SystemSounds.Beep.Play();
                button1.Text = "Open";
                button1.BackColor = Color.ForestGreen;
                MessageBox.Show(ex.Message);
                comboBox1.Enabled = true;
            }
        }

        //发送数据
        private void button1_Click(object sender, EventArgs e)
        {

            //if (iSendFileFlag==true)//如果是点击了 文件发送之后的 情况 ，则需要重新启动线程
            //{
            //    ThreadStart ThUart = new ThreadStart(SerialPortRead);
            //    Thread ThUart1 = new Thread(ThUart);
            //    ThUart1.IsBackground = true;//设置为后台线程  关闭主线程时同时也关闭
            //    ThUart1.Priority = ThreadPriority.Highest;
            //    ThUart1.Start();
            //}

            iSendFileFlag = false;
            //textBoxSend.Text;
            if (serialPort1.IsOpen == false)//未打开串口
            {
                return;
            }

            // byte[] iNewByte = Encoding.ASCII.GetBytes(textBoxSend.Text);
            byte[] iNewByte = System.Text.Encoding.ASCII.GetBytes(textBoxSend.Text);

            byte[] iNewByteEx = new byte[iNewByte.Length + 4];
            int iCmt = 0;
            if (radioButton1.Checked)//HEX
            {
                iNewByteEx[iCmt++] = 0x55;
                iNewByteEx[iCmt++] = 0xAA;
                iNewByteEx[iCmt++] = (byte)(((iNewByte.Length + 4) & 0x0FF00) >> 8);
                iNewByteEx[iCmt++] = (byte)((iNewByte.Length + 4) & 0x0FF);
            }
            else
            {
                iNewByteEx[iCmt++] = 0x55;
                iNewByteEx[iCmt++] = 0xAA;
                iNewByteEx[iCmt++] = (byte)(((iNewByte.Length + 4) & 0x0FF00) >> 8);
                iNewByteEx[iCmt++] = (byte)((iNewByte.Length + 4) & 0x0FF);
            }
            for (int i = 0; i < iNewByte.Length; i++)
            {
                iNewByteEx[iCmt++] = iNewByte[i];
            }


            // byte[] iNewByte = Encoding.Default.GetBytes(textBoxSend.Text);

            // file.Write(iNewByte, 0, iNewByte.Length);
            send_count += iNewByteEx.Length;
            labelTx.Text = "Tx:" + send_count.ToString() + "";      //刷新界面

            iUartCom.SendDate(iNewByteEx, iNewByteEx.Length);//发送命令
            //while (UartValue)
            //{
            //    Thread.Sleep(20);
            //    if (iCount > 100) break;//2s 后退出循环
            //    iCount++;
            //    if (serialPort1.BytesToRead > 0)//接收到数据了
            //    {
            //        // serialPort1.Read(RecBuffer, 0, RecLen);
            //        break;
            //    }
            //}

        }


        //写log 内容
        private void WriteLogFile()
        {

            //FileStream file = new FileStream(@"Temp.txt", FileMode.Create, FileAccess.Write);

            //file.Write(received_buf, 0, 200);

            //file.Close();
        }

        //多线程 读取串口数据
        private void UartReadFile()
        {
            //
            FileStream fileW = new FileStream(@"Temp.txt", FileMode.Create, FileAccess.Write);


            FileStream file = new FileStream(iFilapath, FileMode.Open);//追加
            byte[] byteData = new byte[1024 * 50];    //
            int IndexSum = 1024 * 5;//一包数据大小

            // IndexSum = 64;

            //progressBar1.Maximum=(int)(file.Length/ IndexSum);

            file.Seek(0, SeekOrigin.Begin);
            byteData[0] = 0x55;
            byteData[1] = 0xaa;
            int iSend = 0;
            long letf = file.Length, iCmt = 0;
            double iProValue = 0.0;


          //  ThThread2.Suspend
          //  ThSend1.Suspend();//终止线程

            Stopwatch st = new Stopwatch();//实例化类
            st.Start();//开始计时
                       //需要统计时间的代码段
                       // DateTime Bdt = DateTime.Now;
            DateTime t1 = DateTime.Now;



            Invoke((EventHandler)(delegate
            // BeginInvoke(new Action(()=>
            {
                button1.Visible = false;
                progressBar1.Value = 0;
                progressBar1.Minimum = 0;
                progressBar1.Maximum = 100;//进度条 范围
            }));

            while (letf > 0)
            {
                iSend = 0;
                int len = file.Read(byteData, 4, IndexSum);

                if (len > 0)
                {
                    iCmt += len;
                    iSend = len + 4;
                    byteData[2] = (byte)((iSend & 0x0FF00) >> 8);
                    byteData[3] = (byte)(iSend & 0x0FF);

                    send_count += iSend;
                    iProValue = iCmt * 100 / file.Length;
                   
                    Invoke((EventHandler)(delegate
                    // BeginInvoke(new Action(()=>
                    {
                        progressBar1.Value = (int)iProValue;//进度

                        labelRx.Text = "Rx:" + receive_count.ToString() + "";
                        label1.Text = "File=" + iCmt.ToString() + "/" + file.Length.ToString(); //刷新界面
                        label1.Text += "\r\nPro=" + iProValue.ToString() + "%";
                        labelTx.Text = "Tx:" + send_count.ToString() + "";      //刷新界面

                        DateTime t2 = DateTime.Now; ;
                        TimeSpan ts = t2 - t1;
                        label1.Text += "\r\nTime=" + ts.Minutes.ToString();
                        label1.Text += "m," + ts.Seconds.ToString() + "s," + ts.Milliseconds.ToString() + "ms";
                        
                    }));
                    SerialPortSend(byteData, len + 4);
                    fileW.Write(received_buf, 0, iReFileSum);
                }
                letf -= len;
                if (letf <= 0)
                {
                    break;
                }
            }
            double iSpeed = (file.Length * 2 / 1024) * 1000 / (st.ElapsedMilliseconds);
            st.Stop();//终止计时

            Invoke((EventHandler)(delegate
            {
                label1.Text += "\r\nSpeed=" + iSpeed.ToString() + "kb/s";
                button1.Visible = true;
            }));

            //  Debug.WriteLine(st.ElapsedMilliseconds.ToString());//输出时间。输出运行时间：Elapsed，带毫秒的时间：ElapsedMilliseconds

            fileW.Close();
            file.Close();
            
            MessageBox.Show("文件发送接收完成");
          //  ThSend1.Resume();
           // ThSend1.Abort();//终止线程
        }

        private void labelTx_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            radioButton1.Select();
            OpenFileDialog openFileDialog = new OpenFileDialog();

            //FolderBrowserDialog dialog = new FolderBrowserDialog();
            //openFileDialog.Description = "请选择文件路径";
            iSendFileFlag = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                iFilapath = openFileDialog.FileName;
                //savePath = dialog.SelectedPath;
                //  textBox2.Text = savePath;
            }
            else return;

            textBox1.Text = iFilapath;

            ThSend2 = new ThreadStart(UartReadFile);
            ThThread2 = new Thread(ThSend2);
            

            receive_count = 0;
            send_count = 0;
            labelRx.Text = "Rx:" + receive_count.ToString() + "";
            labelTx.Text = "Tx:" + send_count.ToString() + "";      //刷新界面

            // byte[] received_buf = new byte[1024 * 50];    //串口款冲区
            iUsbLen = 0;//要接收的数据大小
            iUsbLenPre = 0;//已经接收的数据大小
            iUsbReady = false;//是否已经接收完成


            ThThread2.IsBackground = true;//设置为后台线程  关闭主线程时同时也关闭
            ThThread2.Priority = ThreadPriority.Highest;
            ThThread2.Start();

            

        }

        private void button4_Click(object sender, EventArgs e)
        {
            receive_count = 0;
            send_count = 0;
            labelRx.Text = "Rx:" + receive_count.ToString() + "";
            labelTx.Text = "Tx:" + send_count.ToString() + "";      //刷新界面

            textBox_receive.Text = "";


        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            // radioButton1.;
            //radioButton2.Select();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            //radioButton1.Select();
            //radioButton1.Select();
        }

        private void openFileDialog2_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void labelRx_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            radioButton2.Select();

            ThSend = new ThreadStart(SendCanTest);
            ThSend1 = new Thread(ThSend);

            receive_count = 0;
            send_count = 0;
            labelRx.Text = "Rx:" + receive_count.ToString() + "";
            labelTx.Text = "Tx:" + send_count.ToString() + "";      //刷新界面

            ThSend1.IsBackground = true;//设置为后台线程  关闭主线程时同时也关闭
            ThSend1.Priority = ThreadPriority.Highest;
            ThSend1.Start();
        }



        //多线程 发送数据
        private void SerialPortSendCan(byte[] iSendBuffer, int iSendLen)
        {
            bool UartValue = true;
            int iCount = 0;
            RecLen = 0;
            if (serialPort1.IsOpen == false) return;

            //SendFlag = true;
            //iUartCom.SendDate(SendBuffer, SendLen);//发送命令
            RecLen = serialPort1.BytesToRead;
            serialPort1.Read(received_buf, 0, RecLen);//清空款存


            //labelTx.Text = "Tx:" + send_count.ToString() + "";      //刷新界面

            // byte[] received_buf = new byte[1024 * 50];    //串口款冲区
            iUsbLen = 0;//要接收的数据大小
            iUsbLenPre = 0;//已经接收的数据大小
            iUsbReady = false;//是否已经接收完成
            iReFileSum = 0;
            iUartCom.SendDate(iSendBuffer, iSendLen);//发送命令
            DateTime t1 = DateTime.Now;
            while (UartValue)
            {
                iCount++;
                //Thread.Sleep(1);
                //if (iCount > 1000) break;//2s 后退出循环
                DateTime t2 = DateTime.Now;
                TimeSpan ts = t2 - t1;
                if (ts.Seconds > 20) break;//大于2s 超时 

                if (serialPort1.BytesToRead <= 0) continue;


                if (serialPort1.BytesToRead > 0)
                {
                    iUartSum = serialPort1.BytesToRead;      //获取接收缓冲区中的字节数
                    receive_count += iUartSum;
                    serialPort1.Read(RecBuffer, 0, iUartSum);
                   // if (RecBuffer[0] == 0x55 && RecBuffer[1] == 0xAA)
                    {
                        for (int i = 0; i < iUartSum; i++)
                        {
                            received_buf[iReFileSum++] = RecBuffer[i];
                            UartReStr.Append(RecBuffer[i].ToString("X2") + ' ');
                        }

                        break;
                    }
                }

            }

            return;
        }

        //多线程 读取串口数据
        //CAN 测试
        private void SendCanTest()
        {
            byte[] CanData = new byte[1024];    //
            int iSendLen = 0;
            ////55 BB 80 02 Len(BYTE=2)+帧数(2 BYTE)+DATA(08 FC 00 10 0A C0 ....08 FC 00 21 ....) 
            CanData[iSendLen++] = 0x55;
            CanData[iSendLen++] = 0xbb;
            CanData[iSendLen++] = 0x80;
            CanData[iSendLen++] = 0x02;
            //命令长度
            CanData[iSendLen++] = 0x00;
            CanData[iSendLen++] = 0x0D;
            //帧数
            CanData[iSendLen++] = 0x00;
            CanData[iSendLen++] = 0x01;

            CanData[iSendLen++] = 0x08;
            CanData[iSendLen++] = 0xFB;
            CanData[iSendLen++] = 0xE0;
            CanData[iSendLen++] = 0x02;
            CanData[iSendLen++] = 0x01;
            CanData[iSendLen++] = 0x0d;

            for (int i = 0; i < 5000; i++)
            {
                UartReStr.Clear();
                SerialPortSendCan(CanData,  iSendLen);
                send_count += iSendLen;
                //     received_buf[iReFileSum++]
                Invoke((EventHandler)(delegate
                // BeginInvoke(new Action(()=>
                {
                    textBox_receive.AppendText(UartReStr.ToString());
                    labelRx.Text = "Rx:" + receive_count.ToString() + "";
                    labelTx.Text = "Tx:" + send_count.ToString() + "";
                }));
            }


            ThSend1.Abort();//终止线程
            MessageBox.Show("CAN 测试完成");
        }


    }
}
