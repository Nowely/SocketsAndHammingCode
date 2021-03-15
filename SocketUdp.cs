using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace SAHC
{
    public class SocketUdp
    {
        private int _localPort; // порт приема сообщений
        private int _remotePort; // порт для отправки сообщений
        private Socket? _listeningSocket;
        private IPAddress _localHost = IPAddress.Parse("127.0.0.1");
        private IPAddress _remoteHost = IPAddress.Parse("127.0.0.1");

        private SocketUdp() { }

        public static SocketUdp Create()
        {
            var socket = new SocketUdp();
            
            Console.Write("Введите порт для приема сообщений: ");
            int.TryParse(Console.ReadLine(), out socket._localPort);

            Console.Write("Введите порт для отправки сообщений: ");
            int.TryParse(Console.ReadLine(), out socket._remotePort);

            Console.WriteLine("Для отправки сообщений введите сообщение и нажмите Enter");
            Console.WriteLine();
            
            return socket;
        }
        
        public void Start()
        {
            try
            {
                _listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                var listeningTask = new Task(Listen);
                listeningTask.Start();

                // отправка сообщений на разные порты
                while (true)
                {
                    string? message;
                    do
                    {
                        message = Console.ReadLine();
                    } while (string.IsNullOrWhiteSpace(message));

                    var data = Encoding.Unicode.GetBytes(message);
                    EndPoint remotePoint = new IPEndPoint(_remoteHost, _remotePort);
                    _listeningSocket.SendTo(data, remotePoint);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Close();
            }
        }

        // поток для приема подключений
        private void Listen()
        {
            try
            {
                //Прослушиваем по адресу
                var localIp = new IPEndPoint(_localHost, _localPort);
                _listeningSocket.Bind(localIp);

                while (true)
                {
                    // получаем сообщение
                    var builder = new StringBuilder();
                    var bytes = 0; // количество полученных байтов
                    var data = new byte[256]; // буфер для получаемых данных

                    //адрес, с которого пришли данные
                    EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);

                    do
                    {
                        bytes = _listeningSocket.ReceiveFrom(data, ref remoteIp);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    } while (_listeningSocket.Available > 0);

                    // получаем данные о подключении
                    var remoteFullIp = (IPEndPoint)remoteIp;

                    // выводим сообщение
                    Console.WriteLine($"{remoteFullIp.Address}:{remoteFullIp.Port} - {builder}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Close();
            }
        }

        // закрытие сокета
        private void Close()
        {
            if (_listeningSocket == null) return;
            _listeningSocket.Shutdown(SocketShutdown.Both);
            _listeningSocket.Close();
            _listeningSocket = null;
        }
    }
}