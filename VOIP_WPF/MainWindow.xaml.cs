using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

using System.IO;
using System.Linq;
using System.Threading;
using System.Media;
using System.Text;
using NAudio.Wave;
using NAudio.Codecs;

namespace VOIP_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //UDP
        UdpClient updSender;
        UdpClient udpReceiver;

        //Naudio
        WaveIn waveSource = null;
        WaveFileWriter waveFile = null;
        WaveFileWriter waveFileR = null;
        private WaveOut waveOut;
        private BufferedWaveProvider waveProvider;

        Thread r;

        public MainWindow()
        {
            InitializeComponent();
        }

        //# 1 #
        private void Connect_Btn(object sender, RoutedEventArgs e)
        {
            updSender   = new UdpClient("192.168.1.105", 1550);

            ///////

            waveSource = new WaveIn();
            waveSource.WaveFormat = new WaveFormat(44100, 1);
            waveSource.BufferMilliseconds = 50;

            waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);
            waveSource.RecordingStopped += new EventHandler<StoppedEventArgs>(waveSource_RecordingStopped);

            waveFile = new WaveFileWriter(@"0_send.wav", waveSource.WaveFormat);

            waveSource.StartRecording();
        }

        void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveFile != null)
            {
                //Save to file:
                waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                waveFile.Flush();

                //Send via UDP:
                
                byte[] encoded =  Encode(e.Buffer, 0, e.BytesRecorded);
                updSender.Send(encoded, encoded.Length);
            }
        }
        void waveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (waveSource != null)
            {
                waveSource.Dispose();
                waveSource = null;
            }

            if (waveFile != null)
            {
                waveFile.Dispose();
                waveFile = null;
            }
        }


        //# 2 #
        private void Disconnect(object sender, RoutedEventArgs e)
        {

            var star = new ThreadStart(() =>
            {
                try
                {
                    var ip = new IPEndPoint(IPAddress.Parse("192.168.1.105"), 1550);
                    udpReceiver = new UdpClient(ip);
                    waveOut = new WaveOut();
                    waveProvider = new BufferedWaveProvider(new WaveFormat(44100, 1));
                    waveOut.Init(waveProvider);
                    waveOut.Play();
                    waveFileR = new WaveFileWriter(@"1_receive.wav", waveSource.WaveFormat);

                    while (true)
                    {
                        byte[] data = udpReceiver.Receive(ref ip);
                        byte[] decoded = Decode(data, 0, data.Length);

                        waveFileR.Write(decoded, 0, decoded.Length);
                        waveFileR.Flush();

                        //waveProvider = new BufferedWaveProvider(new WaveFormat(44100, 1));
                        waveProvider.AddSamples(decoded, 0, decoded.Length);
                        //waveOut.Init(waveProvider);
                        //waveOut.Play();
                    }
                }
                catch (ThreadAbortException ex)
                {
                    Console.WriteLine("Abort Exception raised ! "+ ex);
                }
                finally
                {
                    Console.WriteLine("Abort Exception not raised -_- ");
                    waveFileR.Dispose();
                    waveOut.Stop();
                }
            });
            r = new Thread(star);
            r.Start();
        }

        //# 3 #
        private void test_Send(object sender, RoutedEventArgs e)
        {
            waveSource.StopRecording();
            r.Abort("Stop Nowwww");
            r.Interrupt();
            if (r.IsAlive) Console.WriteLine("ERROR here");
            waveFileR.Dispose();
            waveOut.Stop();
        }



        public byte[] Encode(byte[] data, int offset, int length)
        {
            var encoded = new byte[length / 2];
            int outIndex = 0;
            for (int n = 0; n < length; n += 2)
            {
                encoded[outIndex++] = MuLawEncoder.LinearToMuLawSample(BitConverter.ToInt16(data, offset + n));
            }
            return encoded;
        }

        public byte[] Decode(byte[] data, int offset, int length)
        {
            var decoded = new byte[length * 2];
            int outIndex = 0;
            for (int n = 0; n < length; n++)
            {
                short decodedSample = MuLawDecoder.MuLawToLinearSample(data[n + offset]);
                decoded[outIndex++] = (byte)(decodedSample & 0xFF);
                decoded[outIndex++] = (byte)(decodedSample >> 8);
            }
            return decoded;
        }
    }
}
