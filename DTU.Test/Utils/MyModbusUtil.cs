using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTU.Test.Utils
{
    public class MyModbusUtil
    {

        /// <summary>
        /// 读功能码
        /// </summary>
        public enum FunctionCode
        {
            //功能码01
            Read01 = 0x01,
            //功能码02
            Read02 = 0x02,
            //功能码03
            Read03 = 0x03,
            //功能码04
            Read04 = 0x04,
            //功能码05
            Write01 = 0x05,
            //功能码06
            Write03 = 0x06,
            //功能码0F
            Write01s = 0x0F,
            //功能码10
            Write03s = 0x10
        }

        /// <summary>
        /// 生成CRC校验码
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] CRC16(byte[] data)
        {
            int len = data.Length;
            if (len > 0)
            {
                ushort crc = 0xFFFF;

                for (int i = 0; i < len; i++)
                {
                    crc = (ushort)(crc ^ (data[i]));
                    for (int j = 0; j < 8; j++)
                    {
                        crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
                    }
                }
                byte hi = (byte)((crc & 0xFF00) >> 8); //高位置
                byte lo = (byte)(crc & 0x00FF); //低位置

                return BitConverter.IsLittleEndian ? new byte[] { lo, hi } : new byte[] { hi, lo };
            }
            return new byte[] { 0, 0 };
        }

        /// <summary>
        /// 获取Read RTU报文
        /// </summary>
        /// <param name="slaveStation"></param>
        /// <param name="readType"></param>
        /// <param name="startAdr"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] GetReadCmd(int slaveStation, FunctionCode readType, ushort startAdr, ushort length)
        {
            //定义临时字节列表
            List<byte> temp = new List<byte>();

            //依次放入头两位字节（站地址和读取模式）
            temp.Add((byte)slaveStation);
            temp.Add((byte)readType);

            //获取起始地址及读取长度
            byte[] start = BitConverter.GetBytes(startAdr);
            byte[] count = BitConverter.GetBytes(length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(start);
                Array.Reverse(count);
            }

            //依次放入起始地址和读取长度
            temp.AddRange(start);
            temp.AddRange(count);

            //获取校验码并在最后放入
            temp.AddRange(CRC16(temp.ToArray()));

            return temp.ToArray();
        }

        /// <summary>
        /// 获取Write单个寄存器RTU报文
        /// </summary>
        /// <param name="slaveStation"></param>
        /// <param name="startAdr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] GetWriteRegisterCmd(int slaveStation, ushort startAdr, ushort value)
        {
            //从站地址
            byte station = (byte)slaveStation;

            //功能码
            byte type = 0x06;

            //寄存器地址
            byte[] start = BitConverter.GetBytes(startAdr);

            //值
            byte[] valueBytes = BitConverter.GetBytes(value);

            //根据计算机大小端存储方式进行高低字节转换
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(start);
                Array.Reverse(valueBytes);
            }

            //拼接报文
            byte[] result = new byte[] { station, type };
            result = result.Concat(start.Concat(valueBytes).ToArray()).ToArray();

            //计算校验码并拼接，返回最后的报文结果
            return result.Concat(CRC16(result)).ToArray();
        }

        /// <summary>
        /// 获取读报文内的寄存器数据,附带简单校验
        /// </summary>
        /// <param name="receiveMsg"></param>
        /// <returns></returns>
        public static byte[] GetData(byte[] receiveMsg, int length, MyModbusUtil.FunctionCode functionCode)
        {
            if (receiveMsg.Length == 0)
                return null;

            byte[] msgchecksum = CRC16(receiveMsg.Skip(0).Take(length - 2).ToArray());

            //校验码
            if (!Enumerable.SequenceEqual(receiveMsg.Skip(length - 2).Take(2).ToArray(), msgchecksum))
            {
                CommonUtils.AddLog("反馈报文校验错误,正确校验: " + CommonUtils.Array2Hex(msgchecksum, 2));
                return null;
            }

            if (receiveMsg.Skip(1).Take(1).ToArray()[0] != (byte)functionCode)
            {
                CommonUtils.AddLog("反馈报文功能码错误");
                return null;
            }
            
            return receiveMsg.Skip(3).Take(length - 5).ToArray();
        }

        public static List<ushort> ByteArray2UShortArray(byte[] byteArr, int len)
        {
            if (byteArr == null || len == 0 || byteArr.Length == 0)
            {
                //CommonUtils.AddLog("ByteArray2UShortArray->Modbus数据为空");
                return null;
            }
                
            if ((len-3)%2 != 0)
            {
                CommonUtils.AddLog("ByteArray2UShortArray->Modbus寄存器数据数量错误");
                return null;
            }

            List<ushort> regList = new List<ushort>();

            for (int i = 0; i < byteArr.Length; i = i+2)
            {
                regList.Add(BitConverter.ToUInt16(new byte[] { byteArr[i + 1], byteArr[i] }, 0));//高位在前
            }

            return regList;
        }
    }
}
