using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace NebulaLauncher.Modules
{
    public class ChatMessage
    {
        public string Sender { get; set; } = "Sistema";
        public string Content { get; set; } = "";
        public string Time { get; set; } = "";
        public string Type { get; set; } = "info"; // chat, info, system
    }

    public static class ChatBridgeService
    {
        public static ObservableCollection<ChatMessage> Messages { get; } = new();
        public static event Action<string>? OnMessageReceived;
        public static event Action<string>? OnCommandRequest;

        static ChatBridgeService()
        {
            // Initial welcoming message
            AddMessage("Sistema", "Bienvenido al Bridge de Chat de Nebula. Conecta tu servidor local para empezar.", "sys");
        }

        public static void AddMessage(string sender, string content, string type = "chat")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(new ChatMessage
                {
                    Sender = sender,
                    Content = content,
                    Time = DateTime.Now.ToString("HH:mm"),
                    Type = type
                });

                if (Messages.Count > 100) Messages.RemoveAt(0);
                OnMessageReceived?.Invoke(content);
            });
        }

        public static void RequestCommand(string command)
        {
            OnCommandRequest?.Invoke(command);
        }
    }
}
