namespace NetworkProtocol
{
    public class UserList : IData
    {
        public string[] Users { get; set; }
        public string DataType { get => nameof(UserList); set { } }
        
        public UserList(string[] users)
        {
            Users = users;
        }

        public UserList()
        { }
    }
}
