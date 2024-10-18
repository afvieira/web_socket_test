using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Threading.Tasks;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace WebSocketExample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Step 1: Start the WebSocket server
            var wssv = new WebSocketServer("ws://localhost:9222");
            wssv.AddWebSocketService<BrowserSocket>("/Browser");
            wssv.Start();
            Console.WriteLine("WebSocket server started at ws://localhost:9222/Browser");

            // Step 2: Start the HTTP server in the background
            Task.Run(() => StartHttpServer());

            // Launch the browser with the HTML file served by the HTTP server
            string url = "http://localhost:8081/browser.html";
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    // On Windows, use 'Process.Start' with the default browser
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = $"/c start {url}",
                        CreateNoWindow = true
                    });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    // On macOS, use 'open' command
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = url,
                        UseShellExecute = true
                    });
                }
                else
                {
                    Console.WriteLine("Unsupported operating system.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error opening browser: " + ex.Message);
            }

            // Keep the WebSocket server running
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();

            // Step 3: Stop the WebSocket server
            wssv.Stop();
        }
        
        // Step 2: Simple HTTP Server to serve the HTML file
        private static void StartHttpServer()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8081/");
            listener.Start();
            Console.WriteLine("HTTP server started at http://localhost:8081");

            while (true)
            {
                try
                {
                    var context = listener.GetContext(); // Wait for a request
                    var request = context.Request;
                    var response = context.Response;

                    // Serve browser.html for any HTTP GET request
                    if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/browser.html")
                    {
                        string htmlPath = "browser.html";  // Path to your HTML file
                        if (File.Exists(htmlPath))
                        {
                            string responseString = File.ReadAllText(htmlPath);
                            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                            response.ContentLength64 = buffer.Length;
                            response.OutputStream.Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                            byte[] buffer = System.Text.Encoding.UTF8.GetBytes("404 - File not found");
                            response.OutputStream.Write(buffer, 0, buffer.Length);
                        }
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes("400 - Bad Request");
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                    }

                    response.OutputStream.Close(); // Close the response
                }
                catch (Exception ex)
                {
                    Console.WriteLine("HTTP server error: " + ex.Message);
                }
            }
        }
    }

    public class BrowserSocket : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            Console.WriteLine("Received message from browser: " + e.Data);
            Send("Message received: " + e.Data);  // Echo message back to the browser
        }

        protected override void OnOpen()
        {
            Console.WriteLine("WebSocket connection opened.");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine("WebSocket connection closed.");
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Console.WriteLine("WebSocket connection with error: " + e.Message);
            base.OnError(e);
        }
    }
}