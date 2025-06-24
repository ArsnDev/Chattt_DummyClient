using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq; // For simple user input parsing

namespace DummyClient
{
    class Program
    {
        private static TcpClient _client;
        private static NetworkStream _stream;
        private const string SERVER_IP = "127.0.0.1"; // Change if your server is on a different IP
        private const int SERVER_PORT = 12345;

        private static string _loggedInUserId = null; // To keep track of the logged-in user

        static async Task Main(string[] args)
        {
            Console.Title = "Chattt Test Client";
            Console.WriteLine("======================================");
            Console.WriteLine("       Chattt Test Client 시작        ");
            Console.WriteLine("======================================");

            try
            {
                // 1. 서버 연결 시도
                await ConnectToServer();

                if (_client != null && _client.Connected)
                {
                    Console.WriteLine("\n서버에 연결되었습니다. 명령을 입력하세요 (ex: LOGIN:id:pw, REGISTER:id:pw, CHAT:message):");
                    Console.WriteLine("--------------------------------------");

                    // 2. 서버로부터 메시지 지속적으로 수신 시작
                    // _ = Task.Run(async () => await ReceiveMessagesAsync());
                    // Use Task.Factory.StartNew for better exception handling in some scenarios
                    // Or just keep the fire-and-forget but be mindful of unhandled exceptions
                    _ = ReceiveMessagesAsync(); // Fire-and-forget for background listening

                    // 3. 사용자 입력 처리 루프
                    while (true)
                    {
                        Console.Write("> ");
                        string input = Console.ReadLine();

                        if (string.IsNullOrWhiteSpace(input)) continue;
                        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                        string[] parts = input.Split(new char[] { ':' }, 2); // Split only on first colon for commands like CHAT

                        string command = parts[0].ToUpper();
                        string data = parts.Length > 1 ? parts[1] : "";

                        string messageToSend = "";

                        switch (command)
                        {
                            case "REGISTER":
                                if (data.Contains(":"))
                                {
                                    messageToSend = $"REGISTER:{data}";
                                    Console.WriteLine($"[Sending] Register request for {data.Split(':')[0]}");
                                }
                                else
                                {
                                    Console.WriteLine("잘못된 REGISTER 형식입니다. REGISTER:id:pw");
                                }
                                break;
                            case "LOGIN":
                                if (data.Contains(":"))
                                {
                                    messageToSend = $"LOGIN:{data}";
                                    Console.WriteLine($"[Sending] Login request for {data.Split(':')[0]}");
                                }
                                else
                                {
                                    Console.WriteLine("잘못된 LOGIN 형식입니다. LOGIN:id:pw");
                                }
                                break;
                            case "CHAT":
                                if (_loggedInUserId != null && !string.IsNullOrWhiteSpace(data))
                                {
                                    // Our server expects CHAT_MESSAGE:SenderID:Content
                                    messageToSend = $"CHAT_MESSAGE:{_loggedInUserId}:{data}";
                                    Console.WriteLine($"[Sending] Chat message: {data}");
                                }
                                else if (_loggedInUserId == null)
                                {
                                    Console.WriteLine("채팅을 보내려면 먼저 로그인해야 합니다.");
                                }
                                else
                                {
                                    Console.WriteLine("보낼 채팅 메시지를 입력해주세요.");
                                }
                                break;
                            case "REQUEST_PARTICIPANTS": // New command to request participants
                            case "REQ_PARTICIPANTS":
                                if (_loggedInUserId != null)
                                {
                                    messageToSend = "REQUEST_PARTICIPANTS";
                                    Console.WriteLine("[Sending] Requesting participant list.");
                                }
                                else
                                {
                                    Console.WriteLine("참가자 목록을 요청하려면 먼저 로그인해야 합니다.");
                                }
                                break;
                            default:
                                Console.WriteLine($"알 수 없는 명령어: {command}");
                                break;
                        }

                        if (!string.IsNullOrEmpty(messageToSend))
                        {
                            await SendMessageAsync(messageToSend);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[오류] 클라이언트 애플리케이션 오류: {ex.Message}");
            }
            finally
            {
                _stream?.Close();
                _client?.Close();
                Console.WriteLine("======================================");
                Console.WriteLine("       Chattt Test Client 종료        ");
                Console.WriteLine("======================================");
                Console.ReadKey(); // Prevent console from closing immediately
            }
        }

        // ===============================================
        // 네트워크 통신 메서드
        // ===============================================

        static async Task ConnectToServer()
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(SERVER_IP, SERVER_PORT);
                _stream = _client.GetStream();
                Console.WriteLine($"[성공] 서버에 연결되었습니다: {SERVER_IP}:{SERVER_PORT}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[오류] 서버 연결 실패: {ex.Message}\n서버가 실행 중인지 확인하세요.");
                _client?.Close();
                _client = null;
                _stream = null;
            }
        }

        static async Task SendMessageAsync(string message)
        {
            if (_client == null || !_client.Connected || _stream == null)
            {
                Console.WriteLine("[오류] 서버에 연결되어 있지 않아 메시지를 보낼 수 없습니다.");
                return;
            }
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[오류] 메시지 전송 실패: {ex.Message}");
                // Handle potential disconnection more robustly
            }
        }

        static async Task ReceiveMessagesAsync()
        {
            byte[] buffer = new byte[4096];
            try
            {
                while (_client != null && _client.Connected)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) // Server disconnected
                    {
                        Console.WriteLine("\n[알림] 서버 연결이 끊어졌습니다.");
                        _client?.Close();
                        break;
                    }

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    ProcessServerResponse(receivedMessage);
                }
            }
            catch (ObjectDisposedException)
            {
                // Stream/client disposed, often on normal shutdown
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[오류] 메시지 수신 중 오류: {ex.Message}");
            }
            finally
            {
                _stream?.Close();
                _client?.Close();
            }
        }

        static void ProcessServerResponse(string response)
        {
            Console.WriteLine($"\n[서버 응답] {response}"); // Print raw response for debugging

            string[] parts = response.Split(':');
            string command = parts[0];

            switch (command)
            {
                case "LOGIN_SUCCESS":
                    _loggedInUserId = parts[1];
                    Console.WriteLine($"-> 로그인 성공: {_loggedInUserId}님 환영합니다!");
                    break;
                case "LOGIN_FAIL":
                    _loggedInUserId = null; // Ensure ID is null on failure
                    Console.WriteLine($"-> 로그인 실패: {parts[1]}");
                    break;
                case "REGISTER_SUCCESS":
                    Console.WriteLine($"-> 회원가입 성공: {parts[1]}");
                    break;
                case "REGISTER_FAIL":
                    Console.WriteLine($"-> 회원가입 실패: {parts[1]}");
                    break;
                case "CHAT_MESSAGE":
                    // Server sends CHAT_MESSAGE:SenderID:Content
                    if (parts.Length >= 3)
                    {
                        Console.WriteLine($"-> [{parts[1]}]: {parts[2]}");
                    }
                    break;
                case "USER_JOINED":
                    Console.WriteLine($"-> [알림] {parts[1]}님이 입장했습니다.");
                    break;
                case "USER_LEFT":
                    Console.WriteLine($"-> [알림] {parts[1]}님이 나갔습니다.");
                    break;
                case "PARTICIPANTS_LIST":
                    if (parts.Length > 1)
                    {
                        string[] participants = parts[1].Split(',');
                        Console.WriteLine($"-> 현재 참가자 ({participants.Length}명): {string.Join(", ", participants)}");
                    }
                    else
                    {
                        Console.WriteLine($"-> 현재 참가자 (0명):");
                    }
                    break;
                case "ERROR":
                    Console.WriteLine($"-> [오류] 서버: {parts[1]}");
                    break;
                default:
                    Console.WriteLine("-> 알 수 없는 서버 응답 형식입니다.");
                    break;
            }
            Console.Write("> "); // Prompt for next input
        }
    }
}