using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.DirectX.DirectInput;
using System.Windows.Threading;
using Sharp7;
using System.Net;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;

namespace WheelWpf
{
    public partial class MainWindow : Window 
    {
        public byte[] buttons;
        public bool[] boolButtons;
        public short XAxis { get; set; }   //Right & Left
        public short YAxis { get; set; }   //Front & back
        public short ZAxis { get; set; }   //Velocity
        public short RzAxis { get; set; }  //Reversal                       

        public bool WheelsEn { get; set; }

        public bool Wheel1Dir { get; set; }
        public bool Wheel2Dir { get; set; }
        public bool Wheel3Dir { get; set; }
        public bool Wheel4Dir { get; set; }

        public short Wheel1Speed { get; set; }
        public short Wheel2Speed { get; set; }
        public short Wheel3Speed { get; set; }
        public short Wheel4Speed { get; set; }

        public bool InputLifeBit { get; set; }
        public bool OutputLifeBit { get; set; }
        public bool IsJoystickControl { get; set; }

        Device device;
        S7Client client = new S7Client();
        DispatcherTimer timer = new DispatcherTimer();
        
        
        private string PlcIp;      //default 192.168.49.2
        private int PlcRack;    //default 0
        private int PlcSlot;    //default 1
        private string IpRegex = @"^(?:[0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(?:\.(?:[0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])?){0,3}$";
        private string IpRegexCompl = @"^(?:[0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(?:\.(?:[0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])){3}$";
        private string RackAndSlotRegex = @"^(\d{1,2}$)";
        public int connectionResult;
        
        public MainWindow()
        {
            InitializeComponent();
        }
        

        private void timer_Tick(object sender, EventArgs e)
        {
            UpdateJoystickState();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshConnectionData();
                CheckConnection();
                timer.Tick += new EventHandler(timer_Tick);
                timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
                timer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("PLC Connection abborded" + ex.Message);
            }
        }

        //full ip validation
        private void IpTextBox_Deactive(object sender, EventArgs e)
        {
            string input = (sender as TextBox).Text;
            if (!Regex.IsMatch(input, IpRegexCompl))
            {    
                MessageBox.Show("Error!, check IP format and try again");
            }
            else
            {               
                WriteToFile(IptextBox.Text, "Ip");
            }
        }

        //Ip validation
        private void IpTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string input = (sender as TextBox).Text;
            if (!Regex.IsMatch(input, IpRegex))
            {
                if (IptextBox.Text.Length != 0)
                    IptextBox.Text = IptextBox.Text.Remove(IptextBox.Text.Length - 1);
                IptextBox.Select(IptextBox.Text.Length, 0);
            }
            
        }
        
        //Validation of Rack
        private void RackTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string input = (sender as TextBox).Text;
            if (!Regex.IsMatch(input, RackAndSlotRegex))
            {
                if (RackTextBox.Text.Length != 0)
                    RackTextBox.Text = RackTextBox.Text.Remove(RackTextBox.Text.Length - 1);
                RackTextBox.Select(RackTextBox.Text.Length, 0);
            }
            else
            {
                WriteToFile(RackTextBox.Text, "Rack");
            }
        }

        //Slot validation
        private void SlotTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string input = (sender as TextBox).Text;
            if (!Regex.IsMatch(input, RackAndSlotRegex))
            {
                if (SlotTextBox.Text.Length != 0)
                    SlotTextBox.Text = SlotTextBox.Text.Remove(SlotTextBox.Text.Length - 1);
                SlotTextBox.Select(SlotTextBox.Text.Length, 0);               
            }
            else
            {                
                WriteToFile(SlotTextBox.Text, "Slot");
            }
        }
        
        private void UpdateJoystickState()
        {
            try
            {
                JoystickState j = device.CurrentJoystickState;
                    
                string info = "";
                buttons = j.GetButtons();
                boolButtons = new bool[buttons.Length];
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] != 0)
                    {
                        //info += "Button: " + i + " ";
                        boolButtons[i] = true;
                        info += boolButtons[i];
                    }
                    else
                        boolButtons[i] = false;
                }

                textBlock1.Text = j.ToString();
                textBlock2.Text = info;
                
                ProcessingJoyData(Convert.ToInt16(j.X), Convert.ToInt16(j.Y), Convert.ToInt16(j.Z), Convert.ToInt16(j.Rz));
                if (connectionResult == 0)
                {
                    client.ConnectTo(PlcIp, PlcRack, PlcSlot);
                    ProcessingJoyData(Convert.ToInt16(j.X), Convert.ToInt16(j.Y),Convert.ToInt16(j.Z),Convert.ToInt16(j.Rz));
                    var readBuffer = new byte[11];
                    client.DBRead(15211, 0, readBuffer.Length, readBuffer);
                    InputLifeBit = S7.GetBitAt(readBuffer, 10, 1);
                    OutputLifeBit = InputLifeBit;
                    if (InputLifeBit)
                    {
                        LifeBitBlock.Fill = Brushes.Green;
                    }
                    if (!InputLifeBit)
                    {
                        LifeBitBlock.Fill = Brushes.Red;
                    }
                    IsJoystickControl = S7.GetBitAt(readBuffer, 10, 2);
                    if (!IsJoystickControl)
                    {
                        //System.Diagnostics.Process.GetCurrentProcess().Kill();
                    }
                    SendInfoToPlc(boolButtons, Convert.ToInt16(j.X), Convert.ToInt16(j.Y), Convert.ToInt16(j.Z), Convert.ToInt16(j.Rz));

                }
            }
            catch (NullReferenceException)
            {              
                label1.Foreground = Brushes.Red;
                label1.FontWeight = FontWeights.UltraBold;
                label1.Content = "Joystick not found! Please check the connection and restart this programm.";
                client.Disconnect();
                //System.Diagnostics.Process.GetCurrentProcess().Kill();
                //throw new NullReferenceException("Joystick not found! Please check the connection and try again.");
            }
        }
        private void CheckConnection()
        {
            
                connectionResult = client.ConnectTo(PlcIp, PlcRack, PlcSlot);
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
        private void SendInfoToPlc(bool[] buttons,short Xaxis, short Yaxis, short Zaxis, short Rzaxis)
        {
            var writeBuffer = new byte[22];           
            S7.SetBitAt(ref writeBuffer, 0, 0, WheelsEn);
            S7.SetBitAt(ref writeBuffer, 0, 1, Wheel1Dir);
            S7.SetBitAt(ref writeBuffer, 0, 2, Wheel2Dir);
            S7.SetBitAt(ref writeBuffer, 0, 3, Wheel3Dir);
            S7.SetBitAt(ref writeBuffer, 0, 4, Wheel4Dir);
            S7.SetIntAt(writeBuffer, 2, Wheel1Speed);
            S7.SetIntAt(writeBuffer, 4, Wheel2Speed);
            S7.SetIntAt(writeBuffer, 6, Wheel3Speed);
            S7.SetIntAt(writeBuffer, 8, Wheel4Speed);
            S7.SetBitAt(ref writeBuffer, 10, 0, OutputLifeBit);
            S7.SetIntAt(writeBuffer, 12, Xaxis);
            S7.SetIntAt(writeBuffer, 14, Yaxis);
            S7.SetIntAt(writeBuffer, 16, Zaxis);
            S7.SetIntAt(writeBuffer, 18, Rzaxis);
            S7.SetBitAt(ref writeBuffer, 20, 0, boolButtons[0]);
            S7.SetBitAt(ref writeBuffer, 20, 1, boolButtons[1]);
            S7.SetBitAt(ref writeBuffer, 20, 2, boolButtons[2]);
            S7.SetBitAt(ref writeBuffer, 20, 3, boolButtons[3]);
            S7.SetBitAt(ref writeBuffer, 20, 4, boolButtons[4]);
            S7.SetBitAt(ref writeBuffer, 20, 5, boolButtons[5]);
            S7.SetBitAt(ref writeBuffer, 20, 6, boolButtons[6]);
            S7.SetBitAt(ref writeBuffer, 20, 7, boolButtons[7]);
            S7.SetBitAt(ref writeBuffer, 21, 0, boolButtons[8]);
            S7.SetBitAt(ref writeBuffer, 21, 1, boolButtons[9]);
            S7.SetBitAt(ref writeBuffer, 21, 2, boolButtons[10]);
            client.DBWrite(15211, 0, writeBuffer.Length, writeBuffer);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshConnectionData();
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
                label1.FontSize = 18;
                label1.Foreground = Brushes.Green;
                label1.FontWeight = FontWeights.Bold;
                label1.Content = device.DeviceInformation.InstanceName;
            }
            catch (NullReferenceException)
            {
                label1.FontSize = 14;
                label1.Foreground = Brushes.Red;
                label1.FontWeight = FontWeights.UltraBold; 
                label1.Content = "Joystick not found! Please check the connection and restart this programm";
            }                     
        }

        void ProcessingJoyData(short LeftRight, short ForwardBack, short velocityCoef, short Reversal)
        {
            //Reversal left
            
            if(Reversal <= 30)
            {
                WheelsEn = true;

                Wheel1Dir = false;
                Wheel2Dir = true;
                Wheel3Dir = false;
                Wheel4Dir = true;

                Wheel1Speed = Convert.ToInt16(100 - velocityCoef);
                Wheel2Speed = Convert.ToInt16(100 - velocityCoef);
                Wheel3Speed = Convert.ToInt16(100 - velocityCoef);
                Wheel4Speed = Convert.ToInt16(100 - velocityCoef);
            }

            //Reversal right
            if(Reversal >= 70)
            {
                WheelsEn = true;
 
                Wheel1Dir = true;
                Wheel2Dir = false;
                Wheel3Dir = true;
                Wheel4Dir = false;

                Wheel1Speed = Convert.ToInt16(100 - velocityCoef);
                Wheel2Speed = Convert.ToInt16(100 - velocityCoef);
                Wheel3Speed = Convert.ToInt16(100 - velocityCoef);
                Wheel4Speed = Convert.ToInt16(100 - velocityCoef);
            }

                       
            //YAxis = ForwardBack;
            if (ForwardBack <= 35 && (Reversal < 70 && Reversal > 30))  //move forward case
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

            if (ForwardBack < 65 && ForwardBack > 35 && (Reversal < 70 && Reversal > 30))  //do not move back or forward case
            {
               
                if (LeftRight > 35 && LeftRight < 65) //stop!!! 
                {
                    WheelsEn = false;
                    Wheel1Speed = 0;
                    Wheel2Speed = 0;
                    Wheel3Speed = 0;
                    Wheel4Speed = 0;
                }
                if(LeftRight >= 65)      //olny left
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
                if(LeftRight <= 35)    //only right
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
            }

            if (ForwardBack >= 65 && (Reversal < 70 && Reversal > 30)) //move back case
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
        private void RefreshConnectionData()
        {
            CurrentIpTextBlock.Text = ReadFromFile("Ip");
            CurrentRackTextBlock.Text = ReadFromFile("Rack");
            CurrentSlotTextBlock.Text = ReadFromFile("Slot");
            PlcIp = ReadFromFile("Ip");
            PlcRack = Convert.ToInt32(ReadFromFile("Rack"));
            PlcSlot = Convert.ToInt32(ReadFromFile("Slot"));
        }


        // file - Ip,Rack,Slot
        private void WriteToFile(string text,string file)
        {
            
            string writePath =string.Format(@"Resources\{0}.txt",file);
           
            try
            {
                using (StreamWriter sw = new StreamWriter(writePath, false, System.Text.Encoding.Default))
                {
                    sw.WriteLine(text);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private string ReadFromFile(string file)
        {
            string readPath = string.Format(@"Resources\{0}.txt", file);

            try
            {
                using (StreamReader sr = new StreamReader(readPath))
                {
                    return sr.ReadLine();
                }
                
            }
            catch (Exception e)
            {                      
                throw new Exception(e.Message);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(@"..\..\..\..\AForge.Wpf.IpCamera\bin\Release\AForge.Wpf.IpCamera.exe");
        }
    }
}