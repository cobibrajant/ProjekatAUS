using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read coil functions/requests.
    /// </summary>
    public class ReadCoilsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadCoilsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadCoilsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc/>
        public override byte[] PackRequest()
        {
            //TO DO: IMPLEMENT
            ModbusReadCommandParameters p = (ModbusReadCommandParameters)CommandParameters;

            byte[] request = new byte[12];

            // MBAP
            request[0] = (byte)(p.TransactionId >> 8);
            request[1] = (byte)(p.TransactionId & 0xFF);

            request[2] = (byte)(p.ProtocolId >> 8);
            request[3] = (byte)(p.ProtocolId & 0xFF);

            request[4] = (byte)(p.Length >> 8);   // usually 0x00
            request[5] = (byte)(p.Length & 0xFF); // usually 0x06

            request[6] = p.UnitId;

            // PDU
            request[7]  = p.FunctionCode; // 0x01
            request[8]  = (byte)(p.StartAddress >> 8);
            request[9]  = (byte)(p.StartAddress & 0xFF);
            request[10] = (byte)(p.Quantity >> 8);
            request[11] = (byte)(p.Quantity & 0xFF);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            ModbusReadCommandParameters p = (ModbusReadCommandParameters)CommandParameters;
            var result = new Dictionary<Tuple<PointType, ushort>, ushort>();

            // MBAP(7) + FC(1) + ByteCount(1) + Data(...)
            if (response == null || response.Length < 9)
            {
                throw new ArgumentException("Invalid Modbus response length.");
            }

            byte functionCode = response[7];

            // Exception response (FC | 0x80), exception code at index 8
            if ((functionCode & 0x80) != 0)
            {
                HandeException(response[8]);
            }

            byte byteCount = response[8];
            int expectedBytes = (p.Quantity + 7) / 8;

            if (byteCount < expectedBytes || response.Length < 9 + expectedBytes)
            {
                throw new ArgumentException("Invalid Modbus coil payload length.");
            }

            for (int i = 0; i < p.Quantity; i++)
            {
                int dataByteIndex = 9 + (i / 8);
                int bitIndex = i % 8; // LSB first per Modbus spec

                ushort value = (ushort)((response[dataByteIndex] >> bitIndex) & 0x01);
                ushort address = (ushort)(p.StartAddress + i);

                result.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, address), value);
            }

            return result;
        }
    }
}