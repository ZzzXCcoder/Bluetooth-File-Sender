using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using InTheHand.Net;


namespace BluetoothFileSender
{
    class Program
    {
        static async Task Main(string[] args)
        {
            BluetoothClient client = new BluetoothClient();

            // Поиск устройств
            var bluetoothDevices = (await Task.Run(() => client.DiscoverDevices())).ToArray();

            if (bluetoothDevices.Length > 0)
            {
                Console.WriteLine($"Найдено устройств: {bluetoothDevices.Length}");
                for (int i = 0; i < bluetoothDevices.Length; i++)
                {
                    var device = bluetoothDevices[i];
                    Console.WriteLine($"{i}: Устройство: {device.DeviceName}, Адрес: {device.DeviceAddress}");
                }

                Console.WriteLine("Введите номер устройства для подключения:");
                int deviceIndex = Convert.ToInt32(Console.ReadLine());
                BluetoothAddress bluetoothAddress = bluetoothDevices[deviceIndex].DeviceAddress;

                // Проверка аутентификации
                if (!bluetoothDevices[deviceIndex].Authenticated)
                {
                    Console.WriteLine("Устройство требует спаривания. Введите PIN-код для устройства:");
                    string pinCode = Console.ReadLine();
                    BluetoothSecurity.PairRequest(bluetoothAddress, pinCode);
                }

                // Укажите путь к файлу
                Console.WriteLine("Введите путь к файлу:");
                string filePath = Console.ReadLine(); 
                string fileName = Path.GetFileName(filePath);

                Uri obexUri = new Uri($"obex://{bluetoothAddress}/{fileName}"); 
                ObexWebRequest request = new ObexWebRequest(obexUri);

                // Проверка возможности чтения файла
                byte[] fileData;
                try
                {
                    fileData = await File.ReadAllBytesAsync(filePath); 
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
                    return; 
                }

                // Настройка заголовков для отправки файла
                request.Headers.Add("Name", fileName);
                request.ContentLength = fileData.Length;

                try
                {
                    using (var requestStream = request.GetRequestStream()) 
                    {
                        await requestStream.WriteAsync(fileData, 0, fileData.Length);
                    }

                    using (var response = (ObexWebResponse)request.GetResponse()) 
                    {
                        Console.WriteLine($"Файл {fileName} успешно отправлен на устройство {bluetoothDevices[deviceIndex].DeviceName}.");
                        Console.WriteLine("Код ответа: " + response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при отправке файла: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Устройства не найдены.");
            }
        }
    }
}
