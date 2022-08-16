using DTU.Test.IO.Impl;
using DTU.Test.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTU.Test.Models
{
    public  class DTUDevice
    {

        public DTUDevice(string serialNumer, byte slaveID, int queryTime)
        {
            SerialNumer = serialNumer;
            SlaveID = slaveID;
            QueryTime = queryTime;

            Coils = new SortedDictionary<ushort, bool>();
            InputCoils = new SortedDictionary<ushort, bool>();
            HoldingRegisters = new SortedDictionary<ushort, ushort>();
            InputRegisters = new SortedDictionary<ushort, ushort>();
        }

        /// <summary>
        /// 从站地址
        /// </summary>
        public byte SlaveID { get; set; }

        /// <summary>
        /// 轮询时间，秒
        /// </summary>
        public int QueryTime{ get; set; }

        /// <summary>
        /// 序列号
        /// </summary>
        public string SerialNumer { get; set; }

        /// <summary>
        /// 是否在线
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// 线圈
        /// </summary>
        public SortedDictionary<ushort, bool> Coils { get; set; }

        /// <summary>
        /// 输入线圈
        /// </summary>
        public SortedDictionary<ushort, bool> InputCoils { get; set; }

        /// <summary>
        /// 保持寄存器
        /// </summary>
        public SortedDictionary<ushort, ushort> HoldingRegisters { get; set; }

        /// <summary>
        /// 输入寄存器
        /// </summary>
        public SortedDictionary<ushort, ushort> InputRegisters { get; set; }

        /// <summary>
        /// 根据DTU的配置进行数据读取，demo实现保持寄存器
        /// </summary>
        /// <param name="rtu"></param>
        public void ReadData(ModbusRTU rtu)
        {
            #region 输入寄存器
            if (InputRegisters.Count > 0)
            {
                //访问包参数 <startAddress,numberOfPoints>
                var cmdPara = new Dictionary<ushort, ushort>();

                //组包规则
                var hAdds = InputRegisters.Keys.ToList();
                ushort startAddress = hAdds[0], numberOfPoints = 1;

                for (ushort i = 0; i < hAdds.Count; i++)
                {
                    if (i == hAdds.Count - 1)
                    {
                        cmdPara.Add(startAddress, numberOfPoints);
                    }
                    else if (hAdds[i+1] == hAdds[i] + 1)
                    {
                        numberOfPoints++;
                    }
                    else 
                    {
                        cmdPara.Add(startAddress, numberOfPoints);
                        startAddress = hAdds[(ushort)(i + 1)];
                        numberOfPoints = 1;
                    }
                }

                //IO赋值
                foreach (var key in cmdPara.Keys)
                {
                    var data = rtu.ReadInputRegisters(SlaveID, key, cmdPara[key]);

                    if (data != null)
                    {
                        for (ushort i = 0; i < data.Length; i++)
                        {
                            var pos = i + key;
                            InputRegisters[(ushort)pos] = data[i];
                        }

                        RefreshInputRegister();
                        CommonUtils.AddLog("读输入寄存器->解析报文完成");
                    }
                }

            }
            #endregion
        }

        /// <summary>
        /// 更新保持寄存器的回调
        /// </summary>
        public delegate void RefreshRegisterDelegate(SortedDictionary<ushort,ushort> hd);
        public RefreshRegisterDelegate whenRefreshHoldingRegister;
        public RefreshRegisterDelegate whenRefreshInputRegister;

        /// <summary>
        /// 更新保持寄存器
        /// </summary>
        /// <param name="add"></param>
        /// <param name="data"></param>
        private void RefreshHoldingRegister()
        {
            if (whenRefreshHoldingRegister != null)
            {
                whenRefreshHoldingRegister.Invoke(HoldingRegisters);
            }
        }


        /// <summary>
        /// 更新输入寄存器
        /// </summary>
        /// <param name="add"></param>
        /// <param name="data"></param>
        private void RefreshInputRegister()
        {
            if (whenRefreshInputRegister != null)
            {
                whenRefreshInputRegister.Invoke(InputRegisters);
            }
        }

    }
}
