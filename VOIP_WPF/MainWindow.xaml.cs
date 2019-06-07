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

namespace VOIP_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IPEndPoint IP_HP = new IPEndPoint(IPAddress.Parse("192.168.1.105"), 1550);
        IPEndPoint IP_TOSHIBA = new IPEndPoint(IPAddress.Parse("192.168.1.107"), 1550);
        UdpClient udpClient_Send;
        UdpClient udpClient_Rec;

        Thread senderThread;
        Thread receiverThread;
        
        //Record
        WasapiCapture capture_mic;

        //?
        WaveWriter w_sender;

        //
        SoundPlayer player = new SoundPlayer();

        public MainWindow()
        {
            InitializeComponent();
            //Task.Factory.StartNew(new Action(tt));
        }

        //# 1 #
        private void Connect_Btn(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine(IP_TOSHIBA.Address.ToString());
                //Start listening on port 1500.
                udpClient_Send = new UdpClient(IP_TOSHIBA.Address.ToString(), 1550);

                senderThread = new Thread(new ThreadStart(Send));
                senderThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,
                    "VoiceChat-InitializeCall ()", MessageBoxButton.OK,
                MessageBoxImage.Error);
            }
        }
        private void Send()
        {
            try
            {
                capture_mic = new WasapiCapture();
                capture_mic.Initialize();
                w_sender = new WaveWriter("0send.wav", capture_mic.WaveFormat);

                capture_mic.DataAvailable += (s, e) =>
                {
                    //save the recorded audio
                    w_sender.Write(e.Data, e.Offset, e.ByteCount);

                    //Send into Udp
                    udpClient_Send.Send(e.Data, e.Data.Length);

                    //log
                    //Console.WriteLine("Length " + e.ByteCount + "   ByteCounts "+e.ByteCount);
                };

                //start recording
                capture_mic.Start();
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine("Sender Thread stops");
                return;
            }
        }


        //# 2 #
        private void Disconnect(object sender, RoutedEventArgs e)
        {
            try
            {
                //Start listening on port 1500.
                udpClient_Rec = new UdpClient(IP_HP.Address.ToString(), 1550);

                receiverThread = new Thread(new ThreadStart(Receive));
                receiverThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,
                    "VoiceChat-InitializeCall Rec()", MessageBoxButton.OK,
                MessageBoxImage.Error);
            }
        }
        private void Receive()
        {
            try
            {
                //capture_mic = new WasapiCapture();
                //capture_mic.Initialize();
                //w = new WaveWriter("Rec.wav", capture_mic.WaveFormat);
                
                while (true)
                {
                    byte[] byteData = udpClient_Rec.Receive(ref IP_HP);
                    Console.WriteLine("==> Received : " + byteData.Length);

                    PlayASound2(new MemoryStream(byteData));
                    
                    //Write received voice to a file
                    //w.Write(byteData, 0, byteData.Length);
                }
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine("Receiver Thread stops");
                return;
            }
        }



        #region CSCore


        public void PlayASound2(Stream s)
        {
            player.Stream = s;
            player.PlaySync();
            player.Dispose();
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

                if (senderThread != null && senderThread.IsAlive) { senderThread.Abort(); Console.WriteLine("Sender Thread Stopped"); }
                if (receiverThread != null && receiverThread.IsAlive) { receiverThread.Abort(); Console.WriteLine("Receiver Thread Stopped"); }
            }
            catch (Exception ex)
            {
                Console.WriteLine("#ERROR : " + ex.StackTrace);
            }
            finally
            {
                if(w_sender!= null && !w_sender.IsDisposed) w_sender.Dispose();

                if (capture_mic != null)
                {
                    Console.WriteLine("Capture stopped.");
                    capture_mic.Stop();
                }
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
