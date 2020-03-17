using System;

namespace NetworkProtocol
{
    [Serializable]
    public class Message : IData
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Text { get; set; }
        public string DataType { get => nameof(Message); set { } }

        public Message(string from, string to, string text)
        {
            From = from;
            To = to;
            Text = text;
        }

        public Message()
        { }
    }
}
