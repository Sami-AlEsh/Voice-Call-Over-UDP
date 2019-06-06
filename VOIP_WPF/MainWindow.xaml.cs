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

namespace VOIP_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        UdpClient udpClient;

        Thread senderThread;
        Thread receiverThread;
        //Record
        WasapiCapture capture;
        //Listen
        ISoundOut soundOut;
        FileStream stream;
        WaveWriter w;

        //test 
        bool flag = true;

        public MainWindow()
        {
            InitializeComponent();
            //Task.Factory.StartNew(new Action(tt));
        }

        private void Connect_Btn(object sender, RoutedEventArgs e)
        {
            InitializeCall();
        }

        private void InitializeCall()
        {
            try
            {
                //Start listening on port 1500.
                udpClient = new UdpClient("192.168.1.107", 1550);

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
                //while (true)
                //{
                //    byte[] data = File.ReadAllBytes("dd.jpg");
                //    udpClient.Send(data, data.Length);
                //    break;//Thread.Sleep(1000);
                //}

                capture = new WasapiCapture();
                capture.Initialize();
                w = new WaveWriter("dump.wav", capture.WaveFormat);

                capture.DataAvailable += (s, e) =>
                {
                    //save the recorded audio
                    w.Write(e.Data, e.Offset, e.ByteCount);

                    //Send into Udp
                    udpClient.Send(e.Data, e.ByteCount);

                    //log
                    //Console.WriteLine("Length " + e.ByteCount + "   ByteCounts "+e.ByteCount);
                };

                //start recording
                capture.Start();
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine("Sender packets stops");
            }
        }


        private void Disconnect(object sender, RoutedEventArgs e)
        {
            try
            {
                //Start listening on port 1500.
                udpClient = new UdpClient("192.168.1.105", 1550);

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
                var IP = new IPEndPoint(IPAddress.Parse("192.168.1.105"), 1550);

                capture = new WasapiCapture();
                capture.Initialize();
                w = new WaveWriter("Rec.wav", capture.WaveFormat);

                while (true)
                {
                    byte[] byteData = udpClient.Receive(ref IP);
                    Console.WriteLine("==> Received : " + byteData.Length);
                    w.Write(byteData, 0, byteData.Length);
                    if (!flag) return;
                }
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine("Receiver packets stops");
            }
        }



        #region CSCore
        void StopPlaying()
        {
            Thread.Sleep(5000);

            //Stop the playback
            soundOut.Stop();
            stream.Dispose();
        }

        //Recording
        void tt()
        {
            capture = new WasapiCapture();
            
            //if nessesary, you can choose a device here
            //to do so, simply set the device property of the capture to any MMDevice
            //to choose a device, take a look at the sample here: http://cscore.codeplex.com/

            //initialize the selected device for recording
            capture.Initialize();

            //create a wavewriter to write the data to
            WaveWriter w = new WaveWriter("dump.wav", capture.WaveFormat);
                
            //setup an eventhandler to receive the recorded data
            capture.DataAvailable += (s, e) =>
            {
                //save the recorded audio
                w.Write(e.Data, e.Offset, e.ByteCount);
            };

            //start recording
            capture.Start();
            
        }

        
        public void PlayASound()
        {
            stream = File.Open(@"dump.mp3",FileMode.Open);
            //Contains the sound to play
            IWaveSource soundSource = GetSoundSource(stream);
            //SoundOut implementation which plays the sound
            soundOut = GetSoundOut();
            
            //Tell the SoundOut which sound it has to play
            soundOut.Initialize(soundSource);
            //Play the sound
            soundOut.Play();

        }

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

        private void test_Send(object sender, RoutedEventArgs e)
        {
            //stop recording
            capture.Stop();
            capture.Dispose();

            //Release File
            w.Dispose();

            //StopThreads:
            try
            {
                if(senderThread.IsAlive) senderThread.Abort();
                if(receiverThread.IsAlive) receiverThread.Abort();
            }
            catch (Exception ex) { Console.WriteLine("#ERROR : " + ex.StackTrace); }
        }
    }
}
