using System;

namespace DTU.Test.IO
{
    public interface IModbusRTU : IDisposable
    {
        bool[] ReadCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints);

        bool[] ReadInputs(byte slaveAddress, ushort startAddress, ushort numberOfPoints);

        ushort[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints);

        ushort[] ReadInputRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints);

        void WriteSingleCoil(byte slaveAddress, ushort coilAddress, bool value);

        void WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort[] value);

        void WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] data);

        void WriteMultipleCoils(byte slaveAddress, ushort startAddress, bool[] data);

        ushort[] ReadWriteMultipleRegisters(
            byte slaveAddress,
            ushort startReadAddress,
            ushort numberOfPointsToRead,
            ushort startWriteAddress,
            ushort[] writeData);
    }
}
