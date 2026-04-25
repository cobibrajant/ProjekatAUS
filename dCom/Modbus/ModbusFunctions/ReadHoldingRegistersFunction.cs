using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read holding registers functions/requests.
    /// </summary>
    public class ReadHoldingRegistersFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadHoldingRegistersFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadHoldingRegistersFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc />
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
            request[4] = (byte)(p.Length >> 8);
            request[5] = (byte)(p.Length & 0xFF);
            request[6] = p.UnitId;

            // PDU
            request[7] = p.FunctionCode; // 0x03
            request[8] = (byte)(p.StartAddress >> 8);
            request[9] = (byte)(p.StartAddress & 0xFF);
            request[10] = (byte)(p.Quantity >> 8);
            request[11] = (byte)(p.Quantity & 0xFF);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            //TO DO: IMPLEMENT
            ModbusReadCommandParameters p = (ModbusReadCommandParameters)CommandParameters;
            var result = new Dictionary<Tuple<PointType, ushort>, ushort>();

            if (response == null || response.Length < 9)
            {
                throw new ArgumentException("Invalid Modbus response length.");
            }

            byte functionCode = response[7];
            if ((functionCode & 0x80) != 0)
            {
                HandeException(response[8]);
            }

            byte byteCount = response[8];
            int expectedDataBytes = p.Quantity * 2;

            if (byteCount < expectedDataBytes || response.Length < 9 + expectedDataBytes)
            {
                throw new ArgumentException("Invalid Modbus holding-register payload length.");
            }

            for (int i = 0; i < p.Quantity; i++)
            {
                int idx = 9 + (i * 2);
                ushort value = (ushort)((response[idx] << 8) | response[idx + 1]);
                ushort address = (ushort)(p.StartAddress + i);

                result.Add(new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, address), value);
            }

            return result;
        }
    }
}