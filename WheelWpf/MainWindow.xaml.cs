using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Sharp7;

namespace WheelWpf
{
    public partial class MainWindow : Window
    {
        public int connectionResult;
        public MainWindow()
        {
            InitializeComponent();
        }

        Device device;
        S7Client client = new S7Client();                       //Check
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

            // Uses axises separate info         
            
            if (connectionResult == 0)
            {
                client.ConnectTo("192.168.49.2", 0, 1);
                var writeBuffer = new byte[2];
                short ydata = Convert.ToInt16(j.Y.ToString()); ;
                S7.SetIntAt(writeBuffer, 0, ydata);           
                client.DBWrite(15211, 0, writeBuffer.Length, writeBuffer);            
                client.Disconnect();
            }
            }
            catch (NullReferenceException)
            {              
                label1.Foreground = Brushes.Red;
                label1.FontWeight = FontWeights.UltraBold;
                label1.Content = "Device not found! Please check the connection and try again.";                
                //System.Diagnostics.Process.GetCurrentProcess().Kill();
                //throw new NullReferenceException("Joystick not found! Please check the connection and try again.");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Connection to the PLC
            //var client = new S7Client();
            connectionResult = client.ConnectTo("192.168.49.2", 0, 1);
            if (connectionResult == 0)
            {
                textBlock4.FontWeight = FontWeights.UltraBold;
                textBlock4.Foreground = Brushes.Green;
                textBlock4.Text = "PLC CONNECTED";
 //               var writeBuffer = new byte[2];
 //               S7.SetIntAt(writeBuffer, 0, 15);
 //               client.DBWrite(15211, 0, writeBuffer.Length, writeBuffer);
            }
            else
            {
                textBlock4.FontWeight = FontWeights.UltraBold;
                textBlock4.Foreground = Brushes.Red;
                textBlock4.Text = "PLC NOT FOUND";              
            }    
            
            //client.Disconnect();
            //////////////////////////////////////////////////////////////////////////
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
    }
}