using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write coil functions/requests.
    /// </summary>
    public class WriteSingleCoilFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleCoilFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleCoilFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            ModbusWriteCommandParameters p = (ModbusWriteCommandParameters)CommandParameters;

            byte[] request = new byte[12];

            // MBAP
            request[0] = (byte)(p.TransactionId >> 8);
            request[1] = (byte)(p.TransactionId & 0xFF);
            request[2] = (byte)(p.ProtocolId >> 8);
            request[3] = (byte)(p.ProtocolId & 0xFF);
            request[4] = (byte)(p.Length >> 8);
            request[5] = (byte)(p.Length & 0xFF);
            request[6] = p.UnitId;

            // PDU (FC = 0x05)
            request[7] = p.FunctionCode;
            request[8] = (byte)(p.OutputAddress >> 8);
            request[9] = (byte)(p.OutputAddress & 0xFF);

            // Modbus coil write value: 0xFF00 = ON, 0x0000 = OFF
            ushort coilValue = (p.Value == 0) ? (ushort)0x0000 : (ushort)0xFF00;
            request[10] = (byte)(coilValue >> 8);
            request[11] = (byte)(coilValue & 0xFF);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            ModbusWriteCommandParameters p = (ModbusWriteCommandParameters)CommandParameters;
            var result = new Dictionary<Tuple<PointType, ushort>, ushort>();

            if (response == null || response.Length < 12)
            {
                throw new ArgumentException("Invalid Modbus response length.");
            }

            byte functionCode = response[7];
            if ((functionCode & 0x80) != 0)
            {
                if (response.Length < 9)
                {
                    throw new ArgumentException("Invalid Modbus exception response length.");
                }

                HandeException(response[8]);
            }

            ushort address = (ushort)((response[8] << 8) | response[9]);
            ushort echoedValue = (ushort)((response[10] << 8) | response[11]);

            if (address != p.OutputAddress)
            {
                throw new ArgumentException("Write coil response address mismatch.");
            }

            ushort normalizedValue;
            if (echoedValue == 0xFF00)
            {
                normalizedValue = 1;
            }
            else if (echoedValue == 0x0000)
            {
                normalizedValue = 0;
            }
            else
            {
                throw new ArgumentException("Invalid write coil response value.");
            }

            result.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, address), normalizedValue);
            return result;
        }
    }
}