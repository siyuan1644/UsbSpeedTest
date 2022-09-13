using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;//多线程空间
using System.Collections;

namespace UsbTest
{

    //全局变量
    public class ComValue
    {
        //CAN SET 
        public static string iCanBaud;//波特率
        public static string iCanType;//帧类型
        public static string iTimeType;//时间参数
        public static string iFiterStr;//过滤内容
        public static byte iFiterType;//过滤类型
    }

    class UartCom
    {
        public System.IO.Ports.SerialPort serialPort1;
        //ThreadStart ts1;// = new ThreadStart(ShowUartDate);
        //Thread t1;//= new Thread(ts1);

        /***************************************
         * 参数:
         *      date  发送款冲区
         *      iLen  发送长度
         * 返回值: 成功>1
         *         失败=-1  
         * ************************************/
        public int UartSendDate(byte[] date, int iLen)
        {
            int iRet = -1;
            //ts1 = new ThreadStart(SendDate);
            //t1 = new Thread(ts1);
            //t1.IsBackground = true;//设置为后台线程  关闭主线程时同时也关闭
            //t1.Priority = ThreadPriority.Lowest;
            //t1.Start();//启动线程


            return iRet;
        }

        //public int SendDate(byte[] date, int iLen)
        //{

        //}

       /***************************************
             * 参数:
             *      date  发送款冲区
             *      iLen  发送长度
             * 返回值: 成功>1
             *         失败=-1  
        * ************************************/
        public int  SendDate(byte []date,int iLen)
        {
            int iRet = -1;
            if (serialPort1.IsOpen == false) return -1;//检查串口是否打开
            serialPort1.Write(date,0, iLen);
            //加入 计时器
           // int iCount = 0;
            //for(; ; )
            //{
            //    Thread.Sleep(100);
            //    iCount++;
            //    if (iCount > 10) break;//2s 超时
            //}
            return iRet;
        }


        public byte GetCanFiter(string iFiterStr,ArrayList iCanIdVe)
        {
            iCanIdVe.Clear();

            if (iFiterStr.Length == 0)
            {
                return 0;
            }
            byte iFiterSum = 0;
            int iLen = iFiterStr.Length;
            if (iFiterStr[iLen - 1] != ',')
            {
                iFiterStr += ',';
                iLen += 1;
            }


 
            string iTemp="";
            for(int i=0;i< iLen;i++)
            {
                if (iFiterStr[i] == ',')
                {
                    // unsigned int iValue = (unsigned int)strtoul(iTemp, 0, 16);
                    
                    uint iId=Convert.ToUInt16(iTemp,16);
                    iCanIdVe.Add(iId);
                    //iCanIdVe.push_back(iValue);
                    //memset(iTemp, 0, sizeof(iTemp));
                    //iCmt = 0;
                    iTemp ="";
                    continue;
                }
                iTemp += iFiterStr[i];
              //  iTemp[iCmt++]=iFiterStr.at(i);
            }

            return iFiterSum;
        }



    }
}
