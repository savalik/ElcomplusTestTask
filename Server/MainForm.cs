using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace Server
{
    public partial class MainForm : Form
    {
        public delegate void LoggingDelegate(string message);
        public delegate void UserConnectedDelegate(string nickname);
        public delegate void UserDisconnectedDelegate(string nickname);

        private readonly Logger _logger;
        private readonly BackgroundWorker _backgroundWorker;
        public MainForm(Logger logger, BackgroundWorker backgroundWorker)
        {
            InitializeComponent();

            _logger = logger;
            _backgroundWorker = backgroundWorker;
            
            LanSetting_Load();
        }

        private void LanSetting_Load()
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if ((nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    || (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)) //&& (nic.OperationalStatus == OperationalStatus.Up))
                {
                    comboBoxInterface.Items.Add(nic.Description);
                }
            }
        }

        private void ComboBoxInterface_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var ip in nic.GetIPProperties().UnicastAddresses)
                {
                    if (nic.Description == comboBoxInterface.SelectedItem.ToString()
                        && ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        ipAddressTextBox.Text = ip.Address.ToString();
                    }
                }
            }
        }

        private void AppendText(string message)
        {
            if (logTextBox.InvokeRequired)
            {
                var d = new LoggingDelegate(AppendText);
                logTextBox.Invoke(d, new object[] { message });
            }
            else
            {
                logTextBox.AppendText($"{message}{Environment.NewLine}");
            }
        }

        private void AppendUser(string nickname)
        {
            if (logTextBox.InvokeRequired)
            {
                var d = new UserConnectedDelegate(AppendUser);
                userListBox.Invoke(d, new object[] { nickname });
            }
            else
            {
                userListBox.Items.Add(nickname);
            }
        }

        private void RemoveUser(string nickname)
        {
            if (logTextBox.InvokeRequired)
            {
                var d = new UserDisconnectedDelegate(RemoveUser);
                userListBox.Invoke(d, new object[] { nickname });
            }
            else
            {
                for (var n = userListBox.Items.Count - 1; n >= 0; --n)
                {
                    var removeListItem = nickname;
                    if (userListBox.Items[n].ToString().Contains(removeListItem))
                    {
                        userListBox.Items.RemoveAt(n);
                    }
                }
            }
        }

        

        private void StartButton_Click(object sender, EventArgs e)
        {
            var loggingDelegate = new LoggingDelegate(AppendText);
            var userConnectedDelegate = new UserConnectedDelegate(AppendUser);
            var userDisconnectedDelegate = new UserDisconnectedDelegate(RemoveUser);

            int.TryParse(portTextBox.Text, out var port);
            IPAddress.TryParse(ipAddressTextBox.Text, out var ip);

            if(port == default || ip == null)
            {
                var incorrectFields =
                $"{(port == default ? "port, " : string.Empty)}{(ip == null ? "ip address" : string.Empty)}";

                MessageBox.Show($"Some fields are incorrect: {incorrectFields}");
            }
            else
            {
                _backgroundWorker.Start(ip, port, loggingDelegate, userConnectedDelegate, userDisconnectedDelegate);

                var startMessage = $"Server started at {ip}:{port} ...";
                _logger.Log(startMessage);
                AppendText(startMessage);

                portTextBox.Enabled = false;
                ipAddressTextBox.Enabled = false;
                comboBoxInterface.Enabled = false;
                startButton.Enabled = false;
            }
        }
    }
}
