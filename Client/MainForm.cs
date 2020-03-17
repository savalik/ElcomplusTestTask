using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace Client
{
    public partial class MainForm : Form
    {
        public delegate void FillChatDelegate(string message);
        public delegate void UpdateUserListDelegate(ICollection<string> users);

        private readonly Dictionary<string, string> _conversations = new Dictionary<string, string>();
        private string _currentUser;

        private readonly BackgroundWorker _backgroundWorker;

        public MainForm(BackgroundWorker backgroundWorker)
        {
            InitializeComponent();

            _backgroundWorker = backgroundWorker;
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            int.TryParse(portTextBox.Text, out var port);
            IPAddress.TryParse(ipAddressTextBox.Text, out var ip);

            if (port == default || ip == null || string.IsNullOrEmpty(nicknameBox.Text))
            {
                var incorrectFields =
                $"{(port == default ? "port, " : string.Empty)}" +
                $"{(ip == null ? "ip address, " : string.Empty)}" +
                $"{(string.IsNullOrEmpty(nicknameBox.Text) ? "nickname" : string.Empty)}";

                MessageBox.Show($"Some fields are incorrect: {incorrectFields.TrimEnd(new []{' ', ','})}");
            }
            else
            {
                var fillChatDelegate = new FillChatDelegate(AppendText);
                var updateUserListDelegate = new UpdateUserListDelegate(UpdateUserList);

                try
                {
                    _backgroundWorker.Connect(
                                ip.ToString(),
                                port,
                                nicknameBox.Text,
                                fillChatDelegate,
                                updateUserListDelegate);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }

                connectButton.Enabled = false;
                nicknameBox.Enabled = false;
                portTextBox.Enabled = false;
                ipAddressTextBox.Enabled = false;
            }
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            if (userListComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("You should select addressee for sending message.");
                return;
            }

            var message = new NetworkProtocol.Message(
                from: nicknameBox.Text, 
                to: userListComboBox.SelectedItem.ToString(),
                text: messageTextBox.Text);
            
            var sentSuccessfully = _backgroundWorker.SendMessage(message);

            if(sentSuccessfully)
            {
                chatTextBox.Text += $"You: {messageTextBox.Text}{Environment.NewLine}";
                messageTextBox.Text = string.Empty;
            }
            else
            {
                messageTextBox.BackColor = Color.MediumVioletRed;
            }
        }

        private void AppendText(string message)
        {
            if (chatTextBox.InvokeRequired)
            {
                var d = new FillChatDelegate(AppendText);
                chatTextBox.Invoke(d, new object[] { message });
            }
            else
            {
                chatTextBox.AppendText($"{message}{Environment.NewLine}");
            }
        }

        private void UpdateUserList(ICollection<string> users)
        {
            if (chatTextBox.InvokeRequired)
            {
                var d = new UpdateUserListDelegate(UpdateUserList);
                userListComboBox.Invoke(d, new object[] { users });
            }
            else
            {
                userListComboBox.Items.Clear();
                userListComboBox.Items.AddRange(users.Select(x => (object)x).ToArray());
            }
        }

        private void UserListComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedUser = userListComboBox.SelectedItem.ToString();

            if (string.IsNullOrEmpty(_currentUser))
            {
                _currentUser = selectedUser;
                return;
            }

            if (selectedUser == _currentUser) return;
            
            _conversations[_currentUser] = chatTextBox.Text;
            _currentUser = selectedUser;
            chatTextBox.Text = _conversations.TryGetValue(selectedUser, out var conversation) 
                ? conversation 
                : string.Empty;
        }
    }
}
