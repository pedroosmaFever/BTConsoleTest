using System;
using System.Linq;
using System.Threading.Tasks;
using InTheHand.Bluetooth;
using System.Text;
using System.Diagnostics;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;

namespace BluetoothFTMS
{
    class Program
    {
        static bool isIndoorBike = false;

        /*
        monitor eliptica => "B01_89CFE" : "C01_89CFE"; 
        exercycle        => "B01_0F288"
        bici matrix      => "C230100054"
            
         * */

        static async Task Main(string[] args)
        {
            Console.WriteLine("Seleccione el tipo de máquina:");
            Console.WriteLine("1. Indoor Bike");
            Console.WriteLine("2. Cross Trainer");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    isIndoorBike = true;
                    break;
                case "2":
                    isIndoorBike = false;
                    break;
                default:
                    Console.WriteLine("Selección no válida. Saliendo del programa.");
                    return;
            }

            Console.WriteLine("Buscando dispositivos Bluetooth...");

            // Escanea dispositivos Bluetooth
            var devices = await Bluetooth.ScanForDevicesAsync();

            string deviceName = isIndoorBike ? "B01_89CFE" : "C01_89CFE";

            // Filtra dispositivos compatibles con FTMS
            var ftmsDevice = devices.FirstOrDefault(d => d.Name.Contains(deviceName));

            if (ftmsDevice == null)
            {
                Console.WriteLine("No se encontró ningún dispositivo FTMS.");
                return;
            }

            Console.WriteLine($"Conectando a {ftmsDevice.Name}...");

            // Conectar al dispositivo
            var deviceGatt = ftmsDevice.Gatt;
            await deviceGatt.ConnectAsync();
            // Obtener servicios del dispositivo
            var services = await deviceGatt.GetPrimaryServicesAsync();
            var ftmsService = services.FirstOrDefault(s => s.Uuid == GattServiceUuids.FitnessMachine);

            if (ftmsService == null)
            {
                Console.WriteLine("No se encontró el servicio FTMS en el dispositivo.");
                return;
            }
            // Obtener características del servicio FTMS
            var characteristics = await ftmsService.GetCharacteristicsAsync();
            GattCharacteristic characteristic;
            if (isIndoorBike)
            {
                characteristic = characteristics.FirstOrDefault(c => c.Uuid == GattCharacteristicUuids.IndoorBikeData);
            }
            else
            {
                characteristic = characteristics.FirstOrDefault(c => c.Uuid == GattCharacteristicUuids.CrossTrainerData);
            }

            if (characteristic == null)
            {
                Console.WriteLine("No se encontró la característica de datos FTMS en el servicio.");
                return;
            }


            // Subscribirse para recibir notificaciones
            characteristic.CharacteristicValueChanged += Characteristic_ValueChanged;
            await characteristic.StartNotificationsAsync();

            Console.WriteLine("Conectado y recibiendo datos. Presiona cualquier tecla para salir...");
            //Console.ReadKey();
            while (true) ;

            // Desuscribirse y desconectar
            await characteristic.StopNotificationsAsync();
            deviceGatt.Disconnect();
        }

        private static void Characteristic_ValueChanged(object sender, GattCharacteristicValueChangedEventArgs e)
        {
            // Procesar y mostrar datos recibidos
            var data = e.Value;

            if (isIndoorBike)
            {
                ProcessIndoorBikeData(data);
            }
            else
            {
                ProcessCrossTrainerData(data);
                //ProcessCrossTrainerDataT(data);
            }
        }

        public static void ProcessCrossTrainerData(Span<byte> data)
        {
            var result = data.ToArray();

            var flags1 = result[0];
            var flags2 = result[1];
            var flags3 = result[2];
            BitArray flags = new BitArray(new byte[] { flags1, flags2, flags3 });

            int currentPos = 3;

            var stb = new StringBuilder();

            var date = DateTime.Now;

            stb.Append(date.ToString("HH:mm:ss"));
            stb.Append(" | ");

            if (!flags.Get(0))
            {
                var InstantSpeed = BitConverter.ToUInt16(result, currentPos) * 0.01f;
                var InstantSpeedTest = ToUInt16(result, currentPos) * 0.01f;
                currentPos += 2;

                stb.Append("InstantSpeed: " + InstantSpeed);
                stb.Append(" | ");
            }

            if (flags.Get(1))
            {
                var avgSpeedM = BitConverter.ToUInt16(result, currentPos) * 0.01f;
                var avgSpeedT = ToUInt16(result, currentPos) * 0.01f;
                currentPos += 2;

                stb.Append("Average Speed: " + avgSpeedM);
                stb.Append(" | ");
            }

            if (flags.Get(2))
            {
                uint totalDistanceM = ToUInt24(result, currentPos, true);
                uint totalDistanceTest = ToUInt24(result, currentPos, false);
                currentPos += 3;

                stb.Append("Total distance: " + totalDistanceM);
                stb.Append(" | ");
            }

            if (flags.Get(3))
            {
                ushort stepPerMinute = BitConverter.ToUInt16(result, currentPos);
                ushort stepPerMinuteT = ToUInt16(result, currentPos);
                var StepPerMinuteFinal = (ushort)(stepPerMinute == 65535 ? 0 : stepPerMinute);
                currentPos += 2;

                stb.Append("SPM: " + stepPerMinute);
                stb.Append(" | ");

                ushort averageStepRate = BitConverter.ToUInt16(result, currentPos);
                ushort averageStepRateT = ToUInt16(result, currentPos);
                ushort AverageStepRateFinal = (ushort)(averageStepRate == 65535 ? 0 : averageStepRate);
                currentPos += 2;

                stb.Append("Avg SP: " + averageStepRate);
                stb.Append(" | ");
            }

            if (flags.Get(4))
            {
                var StrideCount = BitConverter.ToUInt16(result, currentPos);
                var StrideCountT = ToUInt16(result, currentPos);
                currentPos += 2;

                stb.Append("Stride count: " + StrideCount);
                stb.Append(" | ");
            }

            if (flags.Get(5))
            {
                var PositiveElevationGain = BitConverter.ToUInt16(result, currentPos);
                var PositiveElevationGainT = ToUInt16(result, currentPos);
                currentPos += 2;

                stb.Append("Positive elevation: " + PositiveElevationGain);
                stb.Append(" | ");


                var NegativeElevationGain = BitConverter.ToUInt16(result, currentPos);
                var NegativeElevationGainT = ToUInt16(result, currentPos);
                currentPos += 2;


                stb.Append("Negative elevation: " + NegativeElevationGain);
                stb.Append(" | ");
            }


            if (flags.Get(6))
            {
                float inclination = BitConverter.ToInt16(result, currentPos) * 0.1f;
                float inclinationT = ToInt16(result, currentPos) * 0.1f;
                var Inclination = inclination == 32767 ? 0 : inclination;
                currentPos += 2;

                stb.Append("Inclination: " + inclination);
                stb.Append(" | ");

                var rampAngle = BitConverter.ToInt16(result, currentPos) * 0.1f;
                var rampAngleT = ToInt16(result, currentPos) * 0.1f;
                var RampAngle = rampAngle == 32767 ? 0 : rampAngle;
                currentPos += 2;

                stb.Append("Ramp angle: " + rampAngle);
                stb.Append(" | ");
            }

            if (flags.Get(7))
            {
                var ResistanceLevel = ToByte(result, currentPos);
                var ResistanceLevelT = result[currentPos];
                currentPos += 1;


                stb.Append("Resistance level: " + ResistanceLevel);
                stb.Append(" | ");
            }

            if (flags.Get(8))
            {
                var InstantaneousPower = BitConverter.ToInt16(result, currentPos);
                var InstantaneousPowerTest = ToInt16(result, currentPos);


                stb.Append("Instantaneous power: " + InstantaneousPower);
                stb.Append(" | ");

                currentPos += 2;
            }

            if (flags.Get(9))
            {
                var AveragePower = BitConverter.ToInt16(result, currentPos);
                var AveragePowerT = ToInt16(result, currentPos);

                stb.Append("Average power: " + AveragePower);
                stb.Append(" | ");

                currentPos += 2;
            }

            if (flags.Get(10))
            {
                var totalEnergyM = BitConverter.ToUInt16(result, currentPos);
                var totalEnergyTest = ToUInt16(result, currentPos);
                currentPos += 2;

                stb.Append("Total energy: " + totalEnergyM);
                stb.Append(" | ");

                var energyHourM = BitConverter.ToUInt16(result, currentPos);
                var energyHourTest = ToUInt16(result, currentPos);
                currentPos += 2;


                stb.Append("Energy hour: " + energyHourM);
                stb.Append(" | ");

                var energyMinuteM = result[currentPos];
                var energyMinuteT = ToByte(result, currentPos);
                currentPos += 1;


                stb.Append("Energy minute: " + energyMinuteM);
                stb.Append(" | ");
            }


            if (flags.Get(11))
            {
                var HeartRate = result[currentPos];
                var HeartRateT = ToByte(result, currentPos);
                currentPos += 1;

                stb.Append("Heart rate: " + HeartRate);
                stb.Append(" | ");
            }

            if (flags.Get(12))
            {
                var MetabolicEquivalent = ToByte(result, currentPos) * 0.1f;
                var MetabolicEquivalentT = result[currentPos] * 0.1f;
                currentPos += 1;


                stb.Append("Metabolic equivalent:" + MetabolicEquivalent);
                stb.Append(" | ");
            }

            if (flags.Get(13))
            {
                var ElapsedTime = BitConverter.ToUInt16(result, currentPos);
                var ElapsedTimeT = ToUInt16(result, currentPos);
                currentPos += 2;

                stb.Append("Elapsed time:" + ElapsedTime);
                stb.Append(" | ");
            }

            if (flags.Get(14))
            {
                var RemainingTime = BitConverter.ToUInt16(result, currentPos);
                var RemainingTimeT = ToUInt16(result, currentPos);
                currentPos += 2;

                stb.Append("Remaining time:" + RemainingTime);
                stb.Append(" | ");
            }

            var direction = flags.Get(15) ? MovementDirection.Forward : MovementDirection.Backward;

            stb.Append("Direction:" + direction.ToString());

            Console.WriteLine(stb.ToString());

        }

        public static byte ToByte(byte[] data, int position)
        {
            byte result = 0;
            if (data.Length > position + 1)
            {
                result = data[position];
            }
            return result;
        }

        public static ushort ToUInt16(byte[] data, int position)
        {
            ushort result = 0;
            if (data.Length > position + 2)
            {
                result = (ushort)((data[position] << 8) | (data[position + 1]));
            }
            return result;
        }

        public static short ToInt16(byte[] data, int position)
        {
            short result = 0;
            if (data.Length > position + 2)
            {
                result = (short)(
                    (data[position] << 8)
                    | (data[position + 1])
                );
            }
            return result;
        }

        public static uint ToUInt24(byte[] data, int position, bool littleEndian = true)
        {
            uint result = 0;
            if (data.Length > position + 3)
            {
                if (littleEndian)
                {
                    result = (uint)(
                        (data[position + 2] << 16)
                        | (data[position + 1] << 8)
                        | (data[position])
                    );
                }
                else
                {
                    result = (uint)(
                        (data[position] << 16)
                        | (data[position + 1] << 8)
                        | (data[position + 2])
                    );
                }

            }
            return result;
        }



        public static void ProcessIndoorBikeData(Span<byte> data)
        {
            var result = data.ToArray();

            var flags1 = result[0];
            var flags2 = result[1];
            BitArray flags = new BitArray(new byte[] { flags1, flags2 });

            var binaryFlags = "";
            for(int i = 0; i < flags.Length; i++)
            {
                binaryFlags += flags.Get(i) ? "1" : "0";
                if (i ==  7)
                {
                    binaryFlags += " ";
                }
            }

            //Console.WriteLine(binaryFlags);

            int currentPos = 2;

            var stb = new StringBuilder();
            var date = DateTime.Now;
            var timestampStr = date.ToString("HH:mm:ss");

            stb.Append(timestampStr);
            stb.Append(" | ");


            if (!flags.Get(0))
            {
                var InstantSpeed = BitConverter.ToUInt16(result, currentPos) * 0.01f;
                currentPos += 2;

                stb.Append("Instant Speed: " + InstantSpeed);
                stb.Append(" | ");
            }

            if (flags.Get(1))
            {
                var AvgSpeedM = BitConverter.ToUInt16(result, currentPos) * 0.01f;
                currentPos += 2;

                stb.Append("Avg Speed: " + AvgSpeedM);
                stb.Append(" | ");
            }


            if (flags.Get(2))
            {
                var InstantaneousCadence = BitConverter.ToUInt16(result, currentPos) * 0.5f;
                currentPos += 2;

                stb.Append("Instantaneous cadence: " + InstantaneousCadence);
                stb.Append(" | ");
            }
            if (flags.Get(3))
            {
                var AvgCadence = BitConverter.ToUInt16(result, currentPos) * 0.5f;
                currentPos += 2;

                stb.Append("Avg cadence: " + AvgCadence);
                stb.Append(" | ");
            }
            if (flags.Get(4))
            {

                uint totalDistanceM = ToUInt24(result, currentPos, true);
                uint totalDistanceTest = ToUInt24(result, currentPos, false);
                currentPos += 3;

                stb.Append("Total distance: " + totalDistanceM + " m " + (totalDistanceM/1000) + "km");
                stb.Append(" | ");
            }


            if (flags.Get(5))
            {
                var ResistanceLevel = BitConverter.ToInt16(result, currentPos);
                currentPos += 2;

                stb.Append("Resistance level: " + ResistanceLevel);
                stb.Append(" | ");
            }
            if (flags.Get(6))
            {
                var InstantaneousPower = BitConverter.ToInt16(result, currentPos);
                currentPos += 2;
                 
                stb.Append("Instantaneous power: " + InstantaneousPower);
                stb.Append(" | ");
            }
            if (flags.Get(7))
            {
                var AvgPowerM = BitConverter.ToInt16(result, currentPos);
                currentPos += 2;

                stb.Append("Avg power: " + AvgPowerM);
                stb.Append(" | ");
            }
            if (flags.Get(8))
            {
                var TotalEnergyM = BitConverter.ToUInt16(result, currentPos);
                currentPos += 2;

                var EnergyHourM = BitConverter.ToUInt16(result, currentPos);
                currentPos += 2;

                var EnergyMinuteM = result[currentPos];
                currentPos += 1;

                stb.Append("Total energy: " + TotalEnergyM);
                stb.Append(" | ");
                stb.Append("Energy hour: " + EnergyHourM);
                stb.Append(" | ");
                stb.Append("Energy min: " + EnergyMinuteM);
                stb.Append(" | ");

            }

            if (flags.Get(9))
            {
                var HeartRate = result[currentPos];
                currentPos += 1;

                stb.Append("Heart Rate: " + HeartRate);
                stb.Append(" | ");
            }

            if (flags.Get(10))
            {
                var MetabolicEquivalent = result[currentPos];
                currentPos += 1;
                stb.Append("Metabolic equivalent: " + MetabolicEquivalent);
                stb.Append(" | ");

            }
            if (flags.Get(11))
            {
                var ElapsedTime = BitConverter.ToUInt16(result, currentPos);
                currentPos += 2;
                stb.Append("Elapsed time: " + ElapsedTime);
                stb.Append(" | ");

            }
            if (flags.Get(12))
            {
                var RemainingTime = BitConverter.ToUInt16(result, currentPos);
                currentPos += 2;

                stb.Append("Remaining time: " + RemainingTime);

            }

            var bytesStr = BitConverter.ToString(result);
            Console.WriteLine(timestampStr + " | " + bytesStr);
            Console.WriteLine(stb.ToString());

        }
    }

    public enum MovementDirection { Forward, Backward }
}
