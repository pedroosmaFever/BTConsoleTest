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
using InTheHand.Bluetooth.GenericAttributeProfile;

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

            // Filtra dispositivos compatibles con FTMS
            var ftmsDevice = devices.FirstOrDefault(d => d.Name.Contains("FTMS"));
            
            if (ftmsDevice == null)
            {
                Console.WriteLine("No se encontró ningún dispositivo FTMS.");
                return;
            }

            Console.WriteLine($"Conectando a {ftmsDevice.Name}...");

            // Conectar al dispositivo
            await ftmsDevice.ConnectAsync();

            // Obtener servicios del dispositivo
            var services = await ftmsDevice.GetGattServicesAsync();
            var ftmsService = services.FirstOrDefault(s => s.Uuid == GattServiceUuids.FitnessMachine);

            if (ftmsService == null)
            {
                Console.WriteLine("No se encontró el servicio FTMS en el dispositivo.");
                return;
            }

            // Obtener características del servicio FTMS
            var characteristics = await ftmsService.GetCharacteristicsAsync();
            var characteristic = characteristics.FirstOrDefault(c => c.Uuid == GattCharacteristicUuids.FitnessMachineData);

            if (characteristic == null)
            {
                Console.WriteLine("No se encontró la característica de datos FTMS en el servicio.");
                return;
            }

            // Subscribirse para recibir notificaciones
            characteristic.ValueChanged += Characteristic_ValueChanged;
            await characteristic.StartNotificationsAsync();

            Console.WriteLine("Conectado y recibiendo datos. Presiona cualquier tecla para salir...");
            Console.ReadKey();

            // Desuscribirse y desconectar
            await characteristic.StopNotificationsAsync();
            await ftmsDevice.DisconnectAsync();
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
            }
        }

        private static void ProcessIndoorBikeData(Span<byte> data)
        {
            // Decodificar los datos según el protocolo FTMS para Indoor Bike
            if (data.Length >= 2)
            {
                var flags = data[0] | (data[1] << 8);

                int index = 2;
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

        private static void ProcessCrossTrainerData(Span<byte> data)
        {
            // Decodificar los datos según el protocolo FTMS para Cross Trainer (Elíptica)
            if (data.Length >= 2)
            {
                var flags = data[0] | (data[1] << 8);

                int index = 2;
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
                if ((flags & 0x04) != 0 && data.Length >= index + 2) // Distancia Total
                {
                    var totalDistance = BitConverter.ToUInt32(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
                    Console.WriteLine($"Distancia Total: {totalDistance} m");
                }
                if ((flags & 0x08) != 0 && data.Length >= index + 2) // Cuenta de Pasos
                {
                    var stepCount = BitConverter.ToUInt16(data.Slice(index, 2).ToArray(), 0);
                    index += 2;
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
