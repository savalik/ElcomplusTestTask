using System;

namespace NetworkProtocol
{
    [Serializable]
    public class Event : IData
    {
        public EventType Type { get; set; }
        public string Nickname { get; set; }
        public string DataType { get => nameof(Event) ; set { } }

        public Event(EventType type, string nickname)
        {
            Type = type;
            Nickname = nickname;
        }

        public Event()
        { }
    }
}
