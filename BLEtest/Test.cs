using InTheHand.Bluetooth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLEtest
{
    public class Test
    {


        static async Task TestF()
        {
            Console.WriteLine("Buscando dispositivos Bluetooth...");

            // Escanea dispositivos Bluetooth
            var devices = await Bluetooth.ScanForDevicesAsync();

            // Filtra dispositivos compatibles con FTMS
            var ftmsDevice = devices.FirstOrDefault(d => d.Name.Contains("C01_89CFE"));

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
            var characteristic = characteristics.FirstOrDefault(c => c.Uuid == GattCharacteristicUuids.CrossTrainerData);

            if (characteristic == null)
            {
                Console.WriteLine("No se encontró la característica de datos FTMS en el servicio.");
                return;
            }

            // Subscribirse para recibir notificaciones
            characteristic.CharacteristicValueChanged += Characteristic_ValueChanged;
            await characteristic.StartNotificationsAsync();

            Console.WriteLine("Conectado y recibiendo datos. Presiona cualquier tecla para salir...");
            while (true) ;

            // Desuscribirse y desconectar
            await characteristic.StopNotificationsAsync();
            deviceGatt.Disconnect();
        }

        private static void Characteristic_ValueChanged(object sender, GattCharacteristicValueChangedEventArgs e)
        {
            // Procesar y mostrar datos recibidos

            /*
            var stb = new StringBuilder();

            var timestamp = DateTime.Now;

            stb.Append(timestamp.ToString("HH:mm:ss"));
            stb.Append(" | ");
            stb.Append(BitConverter.ToString(data.ToArray()));
            Console.WriteLine(stb.ToString());
            */

            // Procesar y mostrar datos recibidos
            byte[] data = e.Value ?? new byte[] { };

            try
            {
                // Decodificar los datos según el protocolo FTMS
                if (data.Length >= 2)
                {
                    var stb = new StringBuilder();
                    var timestamp = DateTime.Now;

                    stb.Append(timestamp.ToString("HH:mm:ss"));
                    stb.Append(" | ");

                    var flags = data[0] | (data[1] << 8);


                    int index = 2;
                    if ((flags & 0x01) != 0 && data.Length >= index + 2)
                    {
                        var instantaneousSpeed = BitConverter.ToUInt16(data[index..(index + 2)], 0) / 100.0;
                        index += 2;
                        stb.Append($"Velocidad Instantánea: {instantaneousSpeed} km/h");
                        stb.Append(" | ");
                    }
                    if ((flags & 0x02) != 0 && data.Length >= index + 2)
                    {
                        var averageSpeed = BitConverter.ToUInt16(data[index..(index + 2)], 0) / 100.0;
                        index += 2;
                        stb.Append($"Velocidad Promedio: {averageSpeed} km/h");
                        stb.Append(" | ");
                    }
                    if ((flags & 0x04) != 0 && data.Length >= index + 2)
                    {
                        var instantaneousCadence = BitConverter.ToUInt16(data[index..(index + 2)], 0) / 2.0;
                        index += 2;
                        stb.Append($"Cadencia Instantánea: {instantaneousCadence} RPM");
                        stb.Append(" | ");
                    }
                    if ((flags & 0x08) != 0 && data.Length >= index + 2)
                    {
                        var averageCadence = BitConverter.ToUInt16(data[index..(index + 2)], 0) / 2.0;
                        index += 2;
                        stb.Append($"Cadencia Promedio: {averageCadence} RPM");
                        stb.Append(" | ");
                    }
                    if ((flags & 0x10) != 0 && data.Length >= index + 2)
                    {
                        var totalDistance = BitConverter.ToUInt16(data[index..(index + 2)], 0);
                        index += 2;
                        stb.Append($"Distancia Total: {totalDistance} m");
                        stb.Append(" | ");
                    }
                    if ((flags & 0x20) != 0 && data.Length >= index + 2)
                    {
                        var resistanceLevel = BitConverter.ToUInt16(data[index..(index + 2)], 0);
                        index += 2;
                        stb.Append($"Nivel de Resistencia: {resistanceLevel}");
                        stb.Append(" | ");
                    }
                    if ((flags & 0x40) != 0 && data.Length >= index + 2)
                    {
                        var instantaneousPower = BitConverter.ToInt16(data[index..(index + 2)], 0);
                        index += 2;
                        stb.Append($"Potencia Instantánea: {instantaneousPower} W");
                        stb.Append(" | ");
                    }
                    if ((flags & 0x80) != 0 && data.Length >= index + 2)
                    {
                        var averagePower = BitConverter.ToInt16(data[index..(index + 2)], 0);
                        index += 2;
                        stb.Append($"Potencia Promedio: {averagePower} W");
                        stb.Append(" | ");
                    }
                    if ((flags & 0x100) != 0 && data.Length >= index + 2)
                    {
                        var totalEnergy = BitConverter.ToUInt16(data[index..(index + 2)], 0);
                        index += 2;
                        stb.Append($"Energía Total: {totalEnergy} kJ");
                        stb.Append(" | ");
                    }
                    if ((flags & 0x200) != 0 && data.Length >= index + 2)
                    {
                        var energyPerHour = BitConverter.ToUInt16(data[index..(index + 2)], 0);
                        index += 2;
                        stb.Append($"Energía por Hora: {energyPerHour} kJ/h");
                        stb.Append(" | ");
                    }
                    if ((flags & 0x400) != 0 && data.Length >= index + 2)
                    {
                        var energyPerMinute = BitConverter.ToUInt16(data[index..(index + 2)], 0);
                        index += 2;
                        stb.Append($"Energía por Minuto: {energyPerMinute} kJ/min");
                        stb.Append(" | ");
                    }
                    if ((flags & 0x800) != 0 && data.Length >= index + 2)
                    {
                        var heartRate = data[index];
                        index += 1;
                        stb.Append($"Frecuencia Cardíaca: {heartRate} BPM");
                        stb.Append(" | ");
                    }
                    if ((flags & 0x1000) != 0 && data.Length >= index + 2)
                    {
                        var metabolicEquivalent = data[index];
                        index += 1;
                        stb.Append($"Equivalente Metabólico: {metabolicEquivalent} METs");
                        stb.Append(" | ");
                    }
                    if ((flags & 0x2000) != 0 && data.Length >= index + 2)
                    {
                        var elapsedTime = BitConverter.ToUInt16(data[index..(index + 2)], 0);
                        index += 2;
                        stb.Append($"Tiempo Transcurrido: {elapsedTime} s");
                        stb.Append(" | ");
                    }
                    if ((flags & 0x4000) != 0 && data.Length >= index + 2)
                    {
                        var remainingTime = BitConverter.ToUInt16(data[index..(index + 2)], 0);
                        index += 2;
                        stb.Append($"Tiempo Restante: {remainingTime} s");

                    }

                    Console.WriteLine(stb.ToString());
                }
                else
                {
                    Console.WriteLine("Formato de datos no reconocido.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERR");
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
                    var totalDistance = BitConverter.ToInt32(data.Slice(index, 4).ToArray(), 0);
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


    }
}
