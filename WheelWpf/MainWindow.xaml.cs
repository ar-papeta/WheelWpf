using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.DirectX.DirectInput;
using System.Windows.Threading;
using Sharp7;

namespace WheelWpf
{
    public partial class MainWindow : Window
    {
        int i = 0;
        public short XAxis { get; set; }   //Right & Left
        public short YAxis { get; set; }   //Front & back
        public short ZAxis { get; set; }   //Velocity
        public short RzAxis { get; set; }  //Reversal

        public bool back { get; set; }      
        public short velocityC { get; set; }  
        public bool front { get; set; }
        

        public int connectionResult;
        
        public MainWindow()
        {
            InitializeComponent();
        }
        //MainWindow mw = new MainWindow();
        Device device;
        S7Client client = new S7Client();                       
        DispatcherTimer timer = new DispatcherTimer();
        

        private void timer_Tick(object sender, EventArgs e)
        {
            UpdateJoystickState();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            timer.Start();
        }

        private void UpdateJoystickState()
        {
            try
            {
                JoystickState j = device.CurrentJoystickState;
                    
                string info = "";
                byte[] buttons = j.GetButtons();
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] != 0)
                    {
                        info += "Button: " + i + " ";
                    }
                }

                textBlock1.Text = j.ToString();
                textBlock2.Text = info;
                
                ProcessingJoyData(Convert.ToInt16(j.X), Convert.ToInt16(j.Y), Convert.ToInt16(j.Z), Convert.ToInt16(j.Rz));
                if (connectionResult == 0)
                {
                    client.ConnectTo("192.168.49.2", 0, 1);
                    ProcessingJoyData(Convert.ToInt16(j.X), Convert.ToInt16(j.Y),Convert.ToInt16(j.Z),Convert.ToInt16(j.Rz));
                    var writeBuffer = new byte[2];
                    //YAxis = Convert.ToInt16(j.Y);
                    textBlock5.Text = YAxis.ToString();
                    //short Y = Convert.ToInt16(j.Y.ToString());
                    S7.SetIntAt(writeBuffer, 0, YAxis);           
                    client.DBWrite(15211, 0, writeBuffer.Length, writeBuffer);
                    //client.Disconnect();
                }
            }
            catch (NullReferenceException)
            {              
                label1.Foreground = Brushes.Red;
                label1.FontWeight = FontWeights.UltraBold;
                label1.Content = "Device not found! Please check the connection and try again.";
                //client.Disconnect();
                //System.Diagnostics.Process.GetCurrentProcess().Kill();
                //throw new NullReferenceException("Joystick not found! Please check the connection and try again.");
            }
        }
        private void CheckConnection()
        {
            // Check connection to the PLC
            connectionResult = client.ConnectTo("192.168.49.2", 0, 1);
            if (connectionResult == 0)
            {
                textBlock4.FontWeight = FontWeights.UltraBold;
                textBlock4.Foreground = Brushes.Green;
                textBlock4.Text = "PLC CONNECTED";
            }
            else
            {
                textBlock4.FontWeight = FontWeights.UltraBold;
                textBlock4.Foreground = Brushes.Red;
                textBlock4.Text = "PLC NOT FOUND";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            //Check first connection
            // Check connection to the PLC
            connectionResult = client.ConnectTo("192.168.49.2", 0, 1);
            if (connectionResult == 0)
            {
                textBlock4.FontWeight = FontWeights.UltraBold;
                textBlock4.Foreground = Brushes.Green;
                textBlock4.Text = "PLC CONNECTED";
            }
            else
            {
                textBlock4.FontWeight = FontWeights.UltraBold;
                textBlock4.Foreground = Brushes.Red;
                textBlock4.Text = "PLC NOT FOUND";
            }
            //CheckConnection();
            foreach (DeviceInstance instance in Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly))
            {
                device = new Device(instance.ProductGuid);
                device.SetCooperativeLevel(null, CooperativeLevelFlags.Background | CooperativeLevelFlags.NonExclusive);
                foreach (DeviceObjectInstance doi in device.Objects)
                {
                    if ((doi.ObjectId & (int)DeviceObjectTypeFlags.Axis) != 0)
                    {
                        device.Properties.SetRange(
                                ParameterHow.ById,
                                doi.ObjectId,
                                new InputRange(0, 100));
                    }
                }
                device.Acquire();
            }
            try
            {
                label1.Foreground = Brushes.Green;
                label1.FontWeight = FontWeights.Bold;
                label1.Content = device.DeviceInformation.InstanceName;
            }
            catch (NullReferenceException)
            {
                label1.Foreground = Brushes.Red;
                label1.FontWeight = FontWeights.UltraBold; 
                label1.Content = "Device NOT found";
            }                     
        }

        void ProcessingJoyData(short LeftRight, short ForwardBack, short velocityCoef, short Reversal)
        {
            //Console.WriteLine("ss");           
            YAxis = ForwardBack;
            if (ForwardBack <= 40)
            {
                front = true;
                back = false;
                velocityC = Convert.ToInt16(((100 - velocityCoef) * (40 - ForwardBack))/40);               
            }
            if (ForwardBack < 60 && ForwardBack > 40) {
                velocityC = 0;
            }
            if (ForwardBack > 60 )
            {
                velocityC = Convert.ToInt16(((100 - velocityCoef) * (ForwardBack-60))/40);
            }
            textBlock5.Text = velocityC.ToString();
        }
        
    }
}