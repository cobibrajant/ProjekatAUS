using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write single register functions/requests.
    /// </summary>
    public class WriteSingleRegisterFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleRegisterFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleRegisterFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            //TO DO: IMPLEMENT
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

            // PDU (FC = 0x06)
            request[7] = p.FunctionCode;
            request[8] = (byte)(p.OutputAddress >> 8);
            request[9] = (byte)(p.OutputAddress & 0xFF);
            request[10] = (byte)(p.Value >> 8);
            request[11] = (byte)(p.Value & 0xFF);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            //TO DO: IMPLEMENT
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
                throw new ArgumentException("Write register response address mismatch.");
            }

            result.Add(new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, address), echoedValue);
            return result;
        }
    }
}