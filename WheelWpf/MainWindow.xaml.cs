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

            
        public short velocityC { get; set; }
        public short velocityTurn { get; set; }

        public bool WheelsEn { get; set; }


        public bool Wheel1Dir { get; set; }
        public bool Wheel2Dir { get; set; }
        public bool Wheel3Dir { get; set; }
        public bool Wheel4Dir { get; set; }

        public short Wheel1Speed { get; set; }
        public short Wheel2Speed { get; set; }
        public short Wheel3Speed { get; set; }
        public short Wheel4Speed { get; set; }


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

                    var writeBuffer = new byte[10];

                    S7.SetBitAt(ref writeBuffer, 0,0, WheelsEn);
                    S7.SetBitAt(ref writeBuffer, 0,1, Wheel1Dir);
                    S7.SetBitAt(ref writeBuffer, 0,2, Wheel2Dir);
                    S7.SetBitAt(ref writeBuffer, 0,3, Wheel3Dir);
                    S7.SetBitAt(ref writeBuffer, 0,4, Wheel4Dir);
                    S7.SetIntAt(writeBuffer, 2, Wheel1Speed);
                    S7.SetIntAt(writeBuffer, 4, Wheel2Speed);
                    S7.SetIntAt(writeBuffer, 6, Wheel3Speed);
                    S7.SetIntAt(writeBuffer, 8, Wheel4Speed);
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
                textBlock4.Text = "PLC FOUND !";
            }
            else
            {
                textBlock4.FontWeight = FontWeights.UltraBold;
                textBlock4.Foreground = Brushes.Red;
                textBlock4.Text = "PLC NOT FOUND !";
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
            //YAxis = ForwardBack;
            if (ForwardBack <= 35)  //move forward case
            {
                WheelsEn = true;

                Wheel1Dir = true;
                Wheel2Dir = true;
                Wheel3Dir = true;
                Wheel4Dir = true;
                
                if (LeftRight <= 35)  //move forward-left case 
                {
                    Wheel1Speed = Convert.ToInt16((((100 - velocityCoef) * (40 - ForwardBack)) / 40) * (((LeftRight - 0) * (10-3) / (40 - 0) + 3))/10);
                    Wheel2Speed = Convert.ToInt16(((100 - velocityCoef) * (40 - ForwardBack)) / 40);
                    Wheel3Speed = Convert.ToInt16((((100 - velocityCoef) * (40 - ForwardBack)) / 40) * (((LeftRight - 0) * (10 - 3) / (40 - 0) + 3)) / 10);
                    Wheel4Speed = Convert.ToInt16(((100 - velocityCoef) * (40 - ForwardBack)) / 40);                   
                }
                if (LeftRight > 35 && LeftRight <65) //move forward-only case 
                {
                    Wheel1Speed = Convert.ToInt16(((100 - velocityCoef) * (40 - ForwardBack)) / 40);
                    Wheel2Speed = Convert.ToInt16(((100 - velocityCoef) * (40 - ForwardBack)) / 40);
                    Wheel3Speed = Convert.ToInt16(((100 - velocityCoef) * (40 - ForwardBack)) / 40);
                    Wheel4Speed = Convert.ToInt16(((100 - velocityCoef) * (40 - ForwardBack)) / 40);                 
                }
                if (LeftRight >= 65) //move forward-right case 
                {
                    Wheel1Speed = Convert.ToInt16(((100 - velocityCoef) * (40 - ForwardBack)) / 40);
                    Wheel2Speed = Convert.ToInt16((((100 - velocityCoef) * (40 - ForwardBack)) / 40) * (((LeftRight - 60) * (3 - 10) / (100 - 60) + 10)) / 10);
                    Wheel3Speed = Convert.ToInt16(((100 - velocityCoef) * (40 - ForwardBack)) / 40);
                    Wheel4Speed = Convert.ToInt16((((100 - velocityCoef) * (40 - ForwardBack)) / 40) * (((LeftRight - 60) * (3 - 10) / (100 - 60) + 10)) / 10);                    
                }                                          
            }

            if (ForwardBack < 65 && ForwardBack > 35)  //do not move back or forward case
            {
               
                if (LeftRight > 35 && LeftRight < 65) //stop!!! 
                {
                    WheelsEn = false;
                    Wheel1Speed = 0;
                    Wheel2Speed = 0;
                    Wheel3Speed = 0;
                    Wheel4Speed = 0;
                }
                if(LeftRight <= 35)      //olny left
                {
                    WheelsEn = true;

                    Wheel1Dir = true;
                    Wheel2Dir = true;
                    Wheel3Dir = false;
                    Wheel4Dir = false;

                    Wheel1Speed = Convert.ToInt16(((100 - velocityCoef) * (40 - LeftRight)) / 40);
                    Wheel2Speed = Convert.ToInt16(((100 - velocityCoef) * (40 - LeftRight)) / 40);
                    Wheel3Speed = Convert.ToInt16(((100 - velocityCoef) * (40 - LeftRight)) / 40);
                    Wheel4Speed = Convert.ToInt16(((100 - velocityCoef) * (40 - LeftRight)) / 40);
                }
                if(LeftRight >= 65)    //only right
                {
                    WheelsEn = true;

                    Wheel1Dir = false;
                    Wheel2Dir = false;
                    Wheel3Dir = true;
                    Wheel4Dir = true;
                    
                    Wheel1Speed = Convert.ToInt16(((100 - velocityCoef) * (LeftRight - 60)) / 40);
                    Wheel2Speed = Convert.ToInt16(((100 - velocityCoef) * (LeftRight - 60)) / 40);
                    Wheel3Speed = Convert.ToInt16(((100 - velocityCoef) * (LeftRight - 60)) / 40);
                    Wheel4Speed = Convert.ToInt16(((100 - velocityCoef) * (LeftRight - 60)) / 40);
                }
            }

            if (ForwardBack >= 65) //move back case
            {
                WheelsEn = true;

                Wheel1Dir = false;
                Wheel2Dir = false;
                Wheel3Dir = false;
                Wheel4Dir = false;

                if (LeftRight <= 35)  //move back-left case 
                {
                    Wheel1Speed = Convert.ToInt16((((100 - velocityCoef) * (ForwardBack - 60)) / 40) * (((LeftRight - 0) * (10 - 3) / (40 - 0) + 3)) / 10);
                    Wheel2Speed = Convert.ToInt16(((100 - velocityCoef) * (ForwardBack - 60)) / 40);
                    Wheel3Speed = Convert.ToInt16((((100 - velocityCoef) * (ForwardBack - 60)) / 40) * (((LeftRight - 0) * (10 - 3) / (40 - 0) + 3)) / 10);
                    Wheel4Speed = Convert.ToInt16(((100 - velocityCoef) * (ForwardBack - 60)) / 40);
                }
                if (LeftRight > 35 && LeftRight < 65) //move forward-only case 
                {
                    //WheelsEn = false;
                    Wheel1Speed = Convert.ToInt16(((100 - velocityCoef) * (ForwardBack - 60)) / 40);
                    Wheel2Speed = Convert.ToInt16(((100 - velocityCoef) * (ForwardBack - 60)) / 40);
                    Wheel3Speed = Convert.ToInt16(((100 - velocityCoef) * (ForwardBack - 60)) / 40);
                    Wheel4Speed = Convert.ToInt16(((100 - velocityCoef) * (ForwardBack - 60)) / 40);
                }
                if (LeftRight >= 65) //move forward-right case 
                {
                    Wheel1Speed = Convert.ToInt16(((100 - velocityCoef) * (ForwardBack - 60)) / 40);
                    Wheel2Speed = Convert.ToInt16((((100 - velocityCoef) * (ForwardBack - 60)) / 40) * (((LeftRight - 60) * (3 - 10) / (100 - 60) + 10)) / 10);
                    Wheel3Speed = Convert.ToInt16(((100 - velocityCoef) * (ForwardBack - 60)) / 40);
                    Wheel4Speed = Convert.ToInt16((((100 - velocityCoef) * (ForwardBack - 60)) / 40) * (((LeftRight - 60) * (3 - 10) / (100 - 60) + 10)) / 10);

                }

            }

        }
        
    }
}