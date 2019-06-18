using System.Windows;

namespace VOIP_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        VoipCall voiceCall = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        //# 1 #
        private void Connect_Btn(object sender, RoutedEventArgs e)
        {
            voiceCall = new VoipCall("192.168.1.105", 1550);
            voiceCall.InitVoiceCall();
        }
        
        //# 2 #
        private void Disconnect(object sender, RoutedEventArgs e)
        {
            voiceCall.DropCall();
        }

        //# 3 #
        private void test_Send(object sender, RoutedEventArgs e)
        {
        }
    }
}
