using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

using CSCore;
using CSCore.SoundIn;
using CSCore.Codecs.WAV;
using System.IO;
using CSCore.SoundOut;
using CSCore.Codecs.MP3;
using CSCore.CoreAudioAPI;
using System.Linq;
using System.Threading;
using System.Media;
using System.Text;

namespace VOIP_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //IPEndPoint IP_HP = new IPEndPoint(IPAddress.Parse("192.168.1.105"), 1550);
        //IPEndPoint IP_TOSHIBA = new IPEndPoint(IPAddress.Parse("192.168.1.107"), 1550);

        TcpClient Client1;
        TcpClient Client2;

        Thread senderThread;
        Thread receiverThread;
        
        //Record
        WasapiCapture capture_mic;

        //Wave Writer to save recorded sound
        WaveWriter w_sender;
        WaveWriter w_receiver;

        public MainWindow()
        {
            InitializeComponent();

            //Init Sockets Mic and Files:
            //Client1 = new TcpClient("192.168.1.105", 1550);
            //Client2 = new TcpClient("192.168.1.105", 1550);

            capture_mic = new WasapiCapture();
            capture_mic.Initialize();

            WaveFormat f = capture_mic.WaveFormat;

            w_sender = new WaveWriter("0send.wav", f);
            w_receiver = new WaveWriter("1rec.wav", f);
        }

        //# 1 #
        private void Connect_Btn(object sender, RoutedEventArgs e)
        {
            senderThread = new Thread(new ThreadStart(Sen)); senderThread.Start();
        }
        private void Disconnect(object sender, RoutedEventArgs e)
        {
            receiverThread = new Thread(new ThreadStart(Rec)); receiverThread.Start();
        }

        private void Sen()
        {
            try
            {
                //---create a TCPClient object at the IP and port no.---
                TcpClient client = new TcpClient("127.0.0.1", 1550);
                NetworkStream nwStream = client.GetStream();

                capture_mic.DataAvailable += (s, e) =>
                {
                    //save the recorded audio
                    w_sender.Write(e.Data, e.Offset, e.ByteCount);

                    //Send into TCP
                    nwStream.Write(e.Data,0 , e.Data.Length);
                };

                //start recording
                capture_mic.Start();
            }
            catch (ThreadAbortException)
            {
                capture_mic.Stop();
                w_sender.Dispose();
                capture_mic.Dispose();
                Console.WriteLine("Sender Aborted");
            }
        }
        private void Send()
        {
            try
            {
                capture_mic.DataAvailable += (s, e) =>
                {
                    //save the recorded audio
                    w_sender.Write(e.Data, e.Offset, e.ByteCount);

                    //Send into Udp
                    //Client1.Send(e.Data, e.Data.Length);
                };

                //start recording
                capture_mic.Start();
            }
            catch (ThreadAbortException)
            {
                capture_mic.Stop();
                w_sender.Dispose();
                capture_mic.Dispose();
                Console.WriteLine("Sender Thread stops");
                return;
            }
        }


        //# 2 #
        private void Rec()
        {
            try
            {
                //---listen at the specified IP and port no.---
                IPAddress localAdd = IPAddress.Parse("127.0.0.1");
                TcpListener listener = new TcpListener(localAdd, 1550);
                Console.WriteLine("Listening...");
                listener.Start();

                //---incoming client connected---
                TcpClient client = listener.AcceptTcpClient();
                //---get the incoming data through a network stream---
                NetworkStream nwStream = client.GetStream();
                byte[] buffer = new byte[client.ReceiveBufferSize];

                while(true)
                {
                    //---read incoming stream---
                    int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

                    //---convert the data received into a string---
                    byte[] data = buffer.Take(bytesRead).ToArray();
                    Console.WriteLine("==> Received : " + data.Length);

                    w_receiver.Write(data, 0, data.Length);
                }
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine("REC Aborted");
            }
        }
        private void Receive()
        {
            try
            {
                var ip = new IPEndPoint(IPAddress.Parse("192.168.1.105"), 1550);
                while (true)
                {
                    //byte[] byteData = Client2.ReceiveBufferSize(ref ip);
                    //Console.WriteLine("==> Received data : " + byteData.Length);
                    
                    //Write received voice to a file
                    //w_receiver.Write(byteData, 0, byteData.Length);
                }
            }
            catch (ThreadAbortException)
            {
                w_receiver.Dispose();
                Console.WriteLine("Receiver Thread stops");
                return;
            }
        }



        #region CSCore


        public void PlayASound2(Stream s)
        {
            //player.Stream = s;
            //player.PlaySync();
            //player.Dispose();
        }

        public void PlayASound(Stream stream)
        {
            //Contains the sound to play
            using (IWaveSource soundSource = GetSoundSource(stream))
            {
                //SoundOut implementation which plays the sound
                using (ISoundOut soundOut = GetSoundOut())
                {
                    //Tell the SoundOut which sound it has to play
                    soundOut.Initialize(soundSource);
                    //Play the sound
                    soundOut.Play();

                    while (soundOut.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(100);
                    }

                    //Stop the playback
                    soundOut.Stop();
                }
            }
        }
        //public void PlayASound(Stream stream)
        //{
        //    //Contains the sound to play
        //    IWaveSource soundSource = GetSoundSource(stream);
        //    //SoundOut implementation which plays the sound
        //    soundOut = GetSoundOut();

        //    //Tell the SoundOut which sound it has to play
        //    soundOut.Initialize(soundSource);
        //    //Play the sound
        //    soundOut.Play();

        //    while (soundOut.PlaybackState == PlaybackState.Playing)
        //    {
        //        Thread.Sleep(100);
        //    }
        //    soundOut.Stop();

        //}

        private ISoundOut GetSoundOut()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
                return new WasapiOut
                {
                    Device = GetDevice()
                };
            else
                return new DirectSoundOut();
        }
        private IWaveSource GetSoundSource(Stream stream)
        {
            // Instead of using the CodecFactory as helper, you specify the decoder directly:
            return new DmoMp3Decoder(stream);

        }
        public static MMDevice GetDevice()
        {
            using (var mmdeviceEnumerator = new MMDeviceEnumerator())
            {
                using (var mmdeviceCollection = mmdeviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active))
                {
                    // This uses the first device, but that isn't what you necessarily want
                    return mmdeviceCollection.First();
                }
            }
        }
#endregion

        

        //# 3 #
        private void test_Send(object sender, RoutedEventArgs e)
        {
            //StopThreads:
            try
            {
                if (senderThread == null && receiverThread == null) { Console.WriteLine("No Threads to stop !"); return; }

                if (senderThread   != null && senderThread.IsAlive  ) senderThread.Abort();
                if (receiverThread != null && receiverThread.IsAlive) receiverThread.Abort();
            }
            catch (Exception ex)
            {
                Console.WriteLine("#ERROR : " + ex.StackTrace);
            }
        }


        //private byte[] Combine(params byte[][] arrays)
        //{
        //    byte[] rv = new byte[arrays.Sum(a => a.Length)];
        //    int offset = 0;
        //    foreach (byte[] array in arrays)
        //    {
        //        System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
        //        offset += array.Length;
        //    }
        //    return rv;
        //}

    }
}
