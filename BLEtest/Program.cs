using System;
using System.Linq;
using System.Threading.Tasks;
using InTheHand.Net.Bluetooth;
using InTheHand.Bluetooth;
using InTheHand.Net.Sockets;
using System.Text;
using System.Diagnostics;
//using InTheHand.Net.Bluetooth.AttributeIds.GenericAttributeProfile;

using System;
using System.Linq;
using System.Threading.Tasks;
using InTheHand.Bluetooth;
using System.Collections;

namespace BluetoothFTMS
{
    class Program
    {
        static bool isIndoorBike = false;

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
            Console.WriteLine("Datos recibidos:");

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

        private static void ProcessIndoorBikeDataT(Span<byte> data)
        {
            // Decodificar los datos según el protocolo FTMS para Indoor Bike
            if (data.Length >= 2)
            {
                var flags = data[0] | (data[1] << 8);

                int index = 3;
                if ((flags & 0x01) != 0 && data.Length >= index + 2) // Velocidad Instantánea
                {
                    var instantaneousSpeed = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0) / 100.0;
                    index += 2;
                    Console.WriteLine($"Velocidad Instantánea: {instantaneousSpeed} km/h");
                }
                if ((flags & 0x02) != 0 && data.Length >= index + 2) // Velocidad Promedio
                {
                    var averageSpeed = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0) / 100.0;
                    index += 2;
                    Console.WriteLine($"Velocidad Promedio: {averageSpeed} km/h");
                }
                if ((flags & 0x04) != 0 && data.Length >= index + 2) // Cadencia Instantánea
                {
                    var instantaneousCadence = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0) / 2.0;
                    index += 2;
                    Console.WriteLine($"Cadencia Instantánea: {instantaneousCadence} RPM");
                }
                if ((flags & 0x08) != 0 && data.Length >= index + 2) // Cadencia Promedio
                {
                    var averageCadence = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0) / 2.0;
                    index += 2;
                    Console.WriteLine($"Cadencia Promedio: {averageCadence} RPM");
                }
                if ((flags & 0x10) != 0 && data.Length >= index + 2) // Distancia Total
                {
                    var totalDistance = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    Console.WriteLine($"Distancia Total: {totalDistance} m");
                }
                if ((flags & 0x20) != 0 && data.Length >= index + 2) // Nivel de Resistencia
                {
                    var resistanceLevel = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    Console.WriteLine($"Nivel de Resistencia: {resistanceLevel}");
                }
                if ((flags & 0x40) != 0 && data.Length >= index + 2) // Potencia Instantánea
                {
                    var instantaneousPower = BitConverter.ToInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    Console.WriteLine($"Potencia Instantánea: {instantaneousPower} W");
                }
                if ((flags & 0x80) != 0 && data.Length >= index + 2) // Potencia Promedio
                {
                    var averagePower = BitConverter.ToInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    Console.WriteLine($"Potencia Promedio: {averagePower} W");
                }
            }
            else
            {
                Console.WriteLine("Formato de datos no reconocido.");
            }
        }

        private static void ProcessCrossTrainerDataT(Span<byte> data)
        {
            // Decodificar los datos según el protocolo FTMS para Cross Trainer (Elíptica)
            if (data.Length >= 2)
            {
                /*
                var stb = new StringBuilder();

                var timestamp = DateTime.Now;

                stb.Append(timestamp.ToString("HH:mm:ss"));
                stb.Append(" | ");
                stb.Append(BitConverter.ToString(data.ToArray()));
                Console.WriteLine(stb.ToString());
                */

                var flags = data[0] | (data[1] << 8) | (data[1] << 16);

                int index = 3;
                if ((flags & 0x01) == 0 && data.Length >= index + 2) // Velocidad Instantánea
                {
                    var instantaneousSpeed = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0) / 100.0;
                    index += 2;
                    Console.WriteLine($"Velocidad Instantánea: {instantaneousSpeed} km/h");
                }
                if ((flags & 0x02) != 0 && data.Length >= index + 2) // Velocidad Promedio
                {
                    var averageSpeed = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0) / 100.0;
                    index += 2;
                    Console.WriteLine($"Velocidad Promedio: {averageSpeed} km/h");
                }
                if ((flags & 0x04) != 0 && data.Length >= index + 3) // Distancia Total
                {
                    var totalDistance = ToUInt24(data.Slice(index, 4).ToArray(), 0);
                    index += 3;
                    Console.WriteLine($"Distancia Total: {totalDistance} m");
                }
                if ((flags & 0x08) != 0 && data.Length >= index + 4) // Cuenta de Pasos
                {
                    var spm = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    var stepCount = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    Console.WriteLine($"Pasos minuto: {spm}");
                    Console.WriteLine($"Cuenta de Pasos: {stepCount}");
                }
                if ((flags & 0x10) != 0 && data.Length >= index + 2) // Cuenta de Zancadas
                {
                    var strideCount = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    Console.WriteLine($"Cuenta de Zancadas: {strideCount}");
                }
                if ((flags & 0x20) != 0 && data.Length >= index + 2) // Elevación Ganada
                {
                    var elevationGain = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    Console.WriteLine($"Elevación Ganada: {elevationGain} m");
                }
                if ((flags & 0x40) != 0 && data.Length >= index + 4) // Inclinación y Ángulo de Rampa
                {
                    var inclination = BitConverter.ToInt16(data.Slice(index, 2).ToArray(), 0) / 10.0;
                    index += 2;
                    var rampAngle = BitConverter.ToInt16(data.Slice(index, 2).ToArray(), 0) / 10.0;
                    index += 2;
                    Console.WriteLine($"Inclinación: {inclination}°");
                    Console.WriteLine($"Ángulo de Rampa: {rampAngle}°");
                }
                if ((flags & 0x80) != 0 && data.Length >= index + 2) // Nivel de Resistencia
                {
                    var resistanceLevel = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    Console.WriteLine($"Nivel de Resistencia: {resistanceLevel}");
                }
                if ((flags & 0x100) != 0 && data.Length >= index + 2) // Potencia Instantánea
                {
                    var instantaneousPower = BitConverter.ToInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    Console.WriteLine($"Potencia Instantánea: {instantaneousPower} W");
                }
                if ((flags & 0x200) != 0 && data.Length >= index + 2) // Potencia Promedio
                {
                    var averagePower = BitConverter.ToInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    Console.WriteLine($"Potencia Promedio: {averagePower} W");
                }
                if ((flags & 0x400) != 0 && data.Length >= index + 6) // Energía Consumida
                {
                    var totalEnergy = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    var energyPerHour = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    var energyPerMinute = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    Console.WriteLine($"Energía Total: {totalEnergy} kJ");
                    Console.WriteLine($"Energía por Hora: {energyPerHour} kJ/h");
                    Console.WriteLine($"Energía por Minuto: {energyPerMinute} kJ/min");
                }
                if ((flags & 0x800) != 0 && data.Length >= index + 1) // Frecuencia Cardíaca
                {
                    var heartRate = data[index];
                    index += 1;
                    Console.WriteLine($"Frecuencia Cardíaca: {heartRate} BPM");
                }
                if ((flags & 0x1000) != 0 && data.Length >= index + 1) // Equivalente Metabólico
                {
                    var metabolicEquivalent = data[index];
                    index += 1;
                    Console.WriteLine($"Equivalente Metabólico: {metabolicEquivalent} METs");
                }
                if ((flags & 0x2000) != 0 && data.Length >= index + 2) // Tiempo Transcurrido
                {
                    var elapsedTime = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    Console.WriteLine($"Tiempo Transcurrido: {elapsedTime} s");
                }
                if ((flags & 0x4000) != 0 && data.Length >= index + 2) // Tiempo Restante
                {
                    var remainingTime = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    Console.WriteLine($"Tiempo Restante: {remainingTime} s");
                }
                if ((flags & 0x8000) != 0) // Dirección del Movimiento
                {
                    var movementDirection = (flags & 0x8000) != 0 ? "Backward" : "Forward";
                    Console.WriteLine($"Dirección del Movimiento: {movementDirection}");
                }
            }
            else
            {
                Console.WriteLine("Formato de datos no reconocido.");
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

            if (!flags.Get(0))
            {
                var InstantSpeed = BitConverter.ToUInt16(result, currentPos) * 0.01f;
                var InstantSpeedTest = ToUInt16(result, currentPos) * 0.01f;
                currentPos += 2;

                Console.WriteLine("InstantSpeed: " + InstantSpeed);
            }

            if (flags.Get(1))
            {
                var avgSpeedM = BitConverter.ToUInt16(result, currentPos) * 0.01f;
                var avgSpeedT = ToUInt16(result, currentPos) * 0.01f;
                currentPos += 2;

                Console.WriteLine("Average Speed: " + avgSpeedM);
            }

            if (flags.Get(2))
            {
                uint totalDistanceM = ToUInt24(result, currentPos, true);
                uint totalDistanceTest = ToUInt24(result, currentPos, false);
                currentPos += 3;

                Console.WriteLine("Total distance: " + totalDistanceM);
            }

            if (flags.Get(3))
            {
                ushort stepPerMinute = BitConverter.ToUInt16(result, currentPos);
                ushort stepPerMinuteT = ToUInt16(result, currentPos);
                var StepPerMinuteFinal = (ushort)(stepPerMinute == 65535 ? 0 : stepPerMinute);
                currentPos += 2;

                Console.WriteLine("SPM: " + stepPerMinute);

                ushort averageStepRate = BitConverter.ToUInt16(result, currentPos);
                ushort averageStepRateT = ToUInt16(result, currentPos);
                ushort AverageStepRateFinal = (ushort)(averageStepRate == 65535 ? 0 : averageStepRate);
                currentPos += 2;

                Console.WriteLine("Avg SP: " + averageStepRate);
            }

            if (flags.Get(4))
            {
                var StrideCount = BitConverter.ToUInt16(result, currentPos);
                var StrideCountT = ToUInt16(result, currentPos);
                currentPos += 2;

                Console.WriteLine("Stride count: " + StrideCount);
            }

            if (flags.Get(5))
            {
                var PositiveElevationGain = BitConverter.ToUInt16(result, currentPos);
                var PositiveElevationGainT = ToUInt16(result, currentPos);
                currentPos += 2;

                Console.WriteLine("Positive elevation: " + PositiveElevationGain);


                var NegativeElevationGain = BitConverter.ToUInt16(result, currentPos);
                var NegativeElevationGainT = ToUInt16(result, currentPos);
                currentPos += 2;


                Console.WriteLine("Negative elevation: " + NegativeElevationGain);
            }


            if (flags.Get(6))
            {
                float inclination = BitConverter.ToInt16(result, currentPos) * 0.1f;
                float inclinationT = ToInt16(result, currentPos) * 0.1f;
                var Inclination = inclination == 32767 ? 0 : inclination;
                currentPos += 2;

                Console.WriteLine("Inclination: " + inclination);

                var rampAngle = BitConverter.ToInt16(result, currentPos) * 0.1f;
                var rampAngleT = ToInt16(result, currentPos) * 0.1f;
                var RampAngle = rampAngle == 32767 ? 0 : rampAngle;
                currentPos += 2;

                Console.WriteLine("Ramp angle: " + rampAngle);
            }

            if (flags.Get(7))
            {
                var ResistanceLevel = ToByte(result, currentPos);
                var ResistanceLevelT = result[currentPos];
                currentPos += 1;


                Console.WriteLine("Resistance level: " + ResistanceLevel);
            }

            if (flags.Get(8))
            {
                var InstantaneousPower = BitConverter.ToInt16(result, currentPos);
                var InstantaneousPowerTest = ToInt16(result, currentPos);


                Console.WriteLine("Instantaneous power: " + InstantaneousPower);

                currentPos += 2;
            }

            if (flags.Get(9))
            {
                var AveragePower = BitConverter.ToInt16(result, currentPos);
                var AveragePowerT = ToInt16(result, currentPos);

                Console.WriteLine("Average power: " + AveragePower);

                currentPos += 2;
            }

            if (flags.Get(10))
            {
                var totalEnergyM = BitConverter.ToUInt16(result, currentPos);
                var totalEnergyTest = ToUInt16(result, currentPos);
                currentPos += 2;

                Console.WriteLine("Total energy: " + totalEnergyM);

                var energyHourM = BitConverter.ToUInt16(result, currentPos);
                var energyHourTest = ToUInt16(result, currentPos);
                currentPos += 2;


                Console.WriteLine("Energy hour: " + energyHourM);

                var energyMinuteM = result[currentPos];
                var energyMinuteT = ToByte(result, currentPos);
                currentPos += 1;


                Console.WriteLine("Energy minute: " + energyMinuteM);
            }


            if (flags.Get(11))
            {
                var HeartRate = result[currentPos];
                var HeartRateT = ToByte(result, currentPos);
                currentPos += 1;

                Console.WriteLine("Heart rate: " + HeartRate);
            }

            if (flags.Get(12))
            {
                var MetabolicEquivalent = ToByte(result, currentPos) * 0.1f;
                var MetabolicEquivalentT = result[currentPos] * 0.1f;
                currentPos += 1;


                Console.WriteLine("Metabolic equivalent:" + MetabolicEquivalent);
            }

            if (flags.Get(13))
            {
                var ElapsedTime = BitConverter.ToUInt16(result, currentPos);
                var ElapsedTimeT = ToUInt16(result, currentPos);
                currentPos += 2;

                Console.WriteLine("Elapsed time:" + ElapsedTime);
            }

            if (flags.Get(14))
            {
                var RemainingTime = BitConverter.ToUInt16(result, currentPos);
                var RemainingTimeT = ToUInt16(result, currentPos);
                currentPos += 2;

                Console.WriteLine("Remaining time:" + RemainingTime);
            }

            var direction = flags.Get(15) ? MovementDirection.Forward : MovementDirection.Backward;

            Console.WriteLine("Direction:" + direction.ToString());

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
                        (data[position + 2])
                        | (data[position + 1] << 8)
                        | (data[position] << 16)
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
            int currentPos = 2;

            if (!flags.Get(0))
            {
                var InstantSpeed = BitConverter.ToUInt16(result, currentPos) * 0.01f;
                currentPos += 2;

                Console.WriteLine("Instant Speed: " + InstantSpeed);
            }

            if (flags.Get(1))
            {
                var AvgSpeedM = BitConverter.ToUInt16(result, currentPos) * 0.01f;
                currentPos += 2;

                Console.WriteLine("Avg Speed: " + AvgSpeedM);
            }


            if (flags.Get(2))
            {
                var InstantaneousCadence = BitConverter.ToUInt16(result, currentPos) * 0.5f;
                currentPos += 2;

                Console.WriteLine("Instantaneous cadence: " + InstantaneousCadence);
            }
            if (flags.Get(3))
            {
                var AvgCadence = BitConverter.ToUInt16(result, currentPos) * 0.5f;
                currentPos += 2;

                Console.WriteLine("Avg cadence: " + AvgCadence);
            }
            if (flags.Get(4))
            {

                uint totalDistanceM = ToUInt24(result, currentPos, true);
                uint totalDistanceTest = ToUInt24(result, currentPos, false);
                currentPos += 3;

                Console.WriteLine("Total distance: " + totalDistanceM);
            }


            if (flags.Get(5))
            {
                var ResistanceLevel = ToByte(result, currentPos);
                currentPos += 1;

                Console.WriteLine("Resistance level: " + ResistanceLevel);
            }
            if (flags.Get(6))
            {
                var InstantaneousPower = BitConverter.ToInt16(result, currentPos);
                currentPos += 2;
                 
                Console.WriteLine("Instantaneous power: " + InstantaneousPower);
            }
            if (flags.Get(7))
            {
                var AvgPowerM = BitConverter.ToInt16(result, currentPos);
                currentPos += 2;

                Console.WriteLine("Avg power: " + AvgPowerM);
            }
            if (flags.Get(8))
            {
                var TotalEnergyM = BitConverter.ToUInt16(result, currentPos);
                currentPos += 2;

                var EnergyHourM = BitConverter.ToUInt16(result, currentPos);
                currentPos += 2;

                var EnergyMinuteM = result[currentPos];
                currentPos += 1;

                Console.WriteLine("Total energy: " + TotalEnergyM);
                Console.WriteLine("Energy hour: " + EnergyHourM);
                Console.WriteLine("Energy min: " + EnergyMinuteM);

            }

            if (flags.Get(9))
            {
                var HeartRate = result[currentPos];
                currentPos += 1;

                Console.WriteLine("Heart Rate: " + HeartRate);
            }
            if (flags.Get(10))
            {
                var MetabolicEquivalent = result[currentPos];
                currentPos += 1;
                Console.WriteLine("Metabolic equivalent: " + MetabolicEquivalent);

            }
            if (flags.Get(11))
            {
                var ElapsedTime = BitConverter.ToUInt16(result, currentPos);
                currentPos += 2;
                Console.WriteLine("Elapsed time: " + ElapsedTime);

            }
            if (flags.Get(12))
            {
                var RemainingTime = BitConverter.ToUInt16(result, currentPos);
                currentPos += 2;

                Console.WriteLine("Remaining time: " + RemainingTime);

            }


        }
    }

    public enum MovementDirection { Forward, Backward }
}
