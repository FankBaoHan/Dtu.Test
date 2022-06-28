using DTU.Test.IO.Impl;
using DTU.Test.Models;
using DTU.Test.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DTU.Test.Models.DTUDevice;

namespace DTU.Test
{
    public partial class FormMain : Form
    {
        /// <summary>
        /// Modbus询问等待时间
        /// </summary>
        const int REPEAT_ASK_TIME = 1;

        /// <summary>
        /// 服务连接标识
        /// </summary>
        bool bConnected = false;

        /// <summary>
        /// 主连接socket
        /// </summary>
        Socket socketWatch;

        /// <summary>
        /// DTU单独连接socket集合,close用
        /// </summary>
        List<Socket> socketDtus = new List<Socket>();

        /// <summary>
        /// 已配置的DTU集合
        /// </summary>
        Dictionary<string, DTUDevice> DTUs = new Dictionary<string, DTUDevice>();

        public FormMain()
        {
            InitializeComponent();
            new ConsoleUtils(rtbLog);
        }

        /// <summary>
        /// 连接按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>   
        private void button3_Click(object sender, EventArgs e)
        {
            if (!bConnected)
            {
                socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                IPEndPoint iPEnd = new IPEndPoint(IPAddress.Parse("0.0.0.0"), (int)nPort.Value);

                try
                {
                    socketWatch.Bind(iPEnd);
                    socketWatch.Listen(10);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now.ToString() + " : " + ex.Message);
                    return;
                }

                CommonUtils.AddLog("服务已开启");

                Thread thread = new Thread(Listen);
                thread.IsBackground = true;
                thread.Start();
                bConnected = true;
                button3.Text = "断开";
            }
            else
            {
                try
                {
                    socketWatch.Close();
                    CommonUtils.AddLog("服务已断开");
                }
                catch(Exception ex)
                {
                    CommonUtils.AddLog(ex.Message);
                }

                foreach (var s in socketDtus)
                {
                    try
                    {
                        s.Close();
                    }
                    catch (Exception e1)
                    {
                        CommonUtils.AddLog(e1.Message);
                        continue;
                    }
                }

                bConnected = false;
                button3.Text = "连接";
            }
            
        }

        /// <summary>
        /// 监听DTU连接线程
        /// </summary>
        private void Listen()
        {
            Socket socketDtu = null;

            try
            {
                while (true)
                {
                    socketDtu = socketWatch.Accept();

                    socketDtus.Add(socketDtu);
                    CommonUtils.AddLog("有TCP设备连接");

                    Thread reciveThread = new Thread(new ParameterizedThreadStart(Receive));
                    reciveThread.IsBackground = true;
                    reciveThread.Start(socketDtu);
                }
            }
            catch(SocketException ex)
            {
                if (ex.ErrorCode != 10004)
                    CommonUtils.AddLog(ex.Message);

                if (socketDtu != null)
                {
                    socketDtu.Close();
                }
            }
            
        }

        /// <summary>
        /// 处理DTU报文
        /// </summary>
        private void Receive(Object socket)
        {
            var socketDtu = (Socket)socket;
            socketDtu.SetSocketOption(SocketOptionLevel.Socket, 
                SocketOptionName.ReceiveTimeout, 
                REPEAT_ASK_TIME * 1000);

            bool bFirst = true;
            string sNoReceive = "";
            DTUDevice dtu = null;
            byte[] buffer = new byte[1024];

            //rtu可以看作socket的modbus rtu包装
            ModbusRTU rtu = new ModbusRTU(socketDtu);

            while (true)
            {
                if (!socketDtu.Connected)
                {
                    CommonUtils.AddLog("客户端已断开连接");
                    socketDtus.Remove(socketDtu);
                    return;
                }

                //连接
                if (bFirst)
                {
                    Array.Clear(buffer, 0, buffer.Length);

                    int rLen = 0;

                    //阻塞等待
                    try
                    {
                        rLen = socketDtu.Receive(buffer);
                    }
                    catch (SocketException se)
                    {
                        //超时
                        if (se.ErrorCode == 10060)
                            continue;
                    }

                    sNoReceive = Encoding.UTF8.GetString(buffer, 0, rLen).Trim();

                    if (!DTUs.ContainsKey(sNoReceive))
                    {
                        CommonUtils.AddLog("使用未注册序列号或序列号错误，断开连接");
                        socketDtu.Close();
                        socketDtus.Remove(socketDtu);
                        return;
                    }

                    bFirst = false;
                    dtu = DTUs[sNoReceive];
                    dtu.whenRefreshHoldingRegister += SetText;

                    CommonUtils.AddLog("DTU连接成功 SN->" + sNoReceive);
                    continue;
                }
                else
                {
                    //Modbus IO
                    lock(socketDtu)
                    {
                        dtu.ReadData(rtu);
                    }
                }

                Thread.Sleep(dtu.QueryTime * 1000);
            }
        }

        /// <summary>
        /// 更新textbox
        /// </summary>
        /// <param name="buffer"></param>
        private void SetText(SortedDictionary<ushort, ushort> hd)
        {
            if (this.InvokeRequired)
            {
                RefreshHoldingRegisterDelegate d = new RefreshHoldingRegisterDelegate(SetText);
                this.Invoke(d, new object[] { hd });
            }
            else
            {

                //演示4个保持寄存器
                tb401.Text = hd[0].ToString();
                tb402.Text = hd[1].ToString();
                tb403.Text = hd[2].ToString();
                tb404.Text = hd[3].ToString();

                lbTime.Text = DateTime.Now.ToString();
            }
        }

        /// <summary>
        /// 生成序列号按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            string sn = tbSerial.Text = CommonUtils.GetSn();

            //Demo中只演示1个DTU
            DTUs.Clear();

            var dtu = new DTUDevice(sn, (byte)nSlave.Value, (int)nTime.Value);

            //测试保持寄存器点,自动打包
            dtu.HoldingRegisters.Add(0, 0);
            dtu.HoldingRegisters.Add(1, 0);
            dtu.HoldingRegisters.Add(2, 0);
            dtu.HoldingRegisters.Add(3, 0);
            dtu.HoldingRegisters.Add(4, 0);
            dtu.HoldingRegisters.Add(9, 0);
            dtu.HoldingRegisters.Add(10, 0);
            //dtu.HoldingRegisters.Add(110, 0);

            DTUs.Add(sn, dtu);
        }

        /// <summary>
        /// 复制序列号按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbSerial.Text))
                return;

            Clipboard.SetText(tbSerial.Text.Trim());
            CommonUtils.AddLog("序列号已复制");
        }

        /// <summary>
        /// 清空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            rtbLog.Clear();
        }

        /// <summary>
        /// 401写入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            Socket socketDtu = socketDtus.Count>0?socketDtus[0]:null;
            DTUDevice dtu = DTUs.ContainsKey(tbSerial.Text)?DTUs[tbSerial.Text]:null;
            ushort[] data = new ushort[1];

            if (!string.IsNullOrEmpty(tb401.Text))
                data[0] = ushort.Parse(tb401.Text);
                

            if (socketDtu != null && dtu != null && data.Length>0)
            {
                new Thread(() => { 
                    lock(socketDtu)
                    {
                        try
                        {
                            new ModbusRTU(socketDtu).WriteSingleRegister(dtu.SlaveID, 0, data);
                        }
                        catch (Exception ex)
                        {
                            CommonUtils.AddLog(ex.Message);
                        }
                    }
                }).Start();
            }
            else
            {
                CommonUtils.AddLog("没有建立DTU连接,无法写入");
            }
            
        }

        /// <summary>
        /// 自动滚动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rtbLog_TextChanged(object sender, EventArgs e)
        {
            rtbLog.SelectionStart = rtbLog.Text.Length;
            rtbLog.ScrollToCaret();
        }
    }
}
