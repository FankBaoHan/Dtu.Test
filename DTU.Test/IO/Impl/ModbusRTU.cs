using DTU.Test.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DTU.Test.IO.Impl
{
    public class ModbusRTU : IModbusRTU
    {
        Socket socket;

        public ModbusRTU(Socket socket)
        {
            this.socket = socket;
        }

        public void Dispose()
        {
            try
            {
                socket?.Dispose();
            }
            catch(Exception e)
            {
                
            }
        }

        public bool[] ReadCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            throw new NotImplementedException();
        }

        public ushort[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            int len = 0;

            ValidateNumberOfPoints("访问寄存器数量错误", numberOfPoints, 2000);

            var buffer = ModbusTransfer(slaveAddress, startAddress, numberOfPoints, MyModbusUtil.ReadType.Read03, out len);

            return MyModbusUtil.ByteArray2UShortArray(buffer, len)?.ToArray();
        }

        public ushort[] ReadInputRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            throw new NotImplementedException();
        }

        public bool[] ReadInputs(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            throw new NotImplementedException();
        }

        public ushort[] ReadWriteMultipleRegisters(byte slaveAddress, ushort startReadAddress, ushort numberOfPointsToRead, ushort startWriteAddress, ushort[] writeData)
        {
            throw new NotImplementedException();
        }

        public void WriteMultipleCoils(byte slaveAddress, ushort startAddress, bool[] data)
        {
            throw new NotImplementedException();
        }

        public void WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] data)
        {
            throw new NotImplementedException();
        }

        public void WriteSingleCoil(byte slaveAddress, ushort coilAddress, bool value)
        {
            throw new NotImplementedException();
        }

        public void WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Modbus 数据交换过程
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfPoints"></param>
        /// <param name="readType"></param>
        /// <returns></returns>
        private byte[] ModbusTransfer(byte slaveAddress, ushort startAddress, ushort numberOfPoints, MyModbusUtil.ReadType readType, out int len)
        {
            byte[] buffer = new byte[1024];
            len = 0;

            Byte[] cmd = MyModbusUtil.GetReadCmd(
                slaveAddress,
                readType,
                startAddress,
                numberOfPoints);

            try
            {
                socket.Send(cmd);
                CommonUtils.AddLog("发送报文->" + CommonUtils.Array2Hex(cmd, cmd.Length));

                len = socket.Receive(buffer);

                if (buffer != null && buffer.Length != 0 && len != 0)
                {
                    CommonUtils.AddLog("收到报文->" + CommonUtils.Array2Hex(buffer, len));
                }
                else
                {
                    return null;
                }
                    
            }
            catch (SocketException se)
            {
                if (se.ErrorCode == 10060)
                    //CommonUtils.AddLog("未收到回应报文，重新询问");
                return null;
            }
            catch (Exception e)
            {
                CommonUtils.AddLog("IO失败 " + e.Message);
                return null;
            }

            return MyModbusUtil.GetData(buffer, len, readType);
        }

        /// <summary>
        /// 访问寄存器合法性判断
        /// </summary>
        /// <param name="argumentName"></param>
        /// <param name="numberOfPoints"></param>
        /// <param name="maxNumberOfPoints"></param>
        /// <exception cref="ArgumentException"></exception>
        private static void ValidateNumberOfPoints(string argumentName, ushort numberOfPoints, ushort maxNumberOfPoints)
        {
            if (numberOfPoints < 1 || numberOfPoints > maxNumberOfPoints)
            {
                string msg = $"Argument {argumentName} must be between 1 and {maxNumberOfPoints} inclusive.";
                throw new ArgumentException(msg);
            }
        }
    }
}
