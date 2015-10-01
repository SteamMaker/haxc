using Microsoft.Maker.RemoteWiring;
using Microsoft.Maker.Serial;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HAXCSolar
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class MainPage : Page
  {
    IStream connection;
    RemoteDevice arduino;
    DispatcherTimer timer;
    bool arduinoReady = false;

    public MainPage()
    {
      this.InitializeComponent();

      connection = new UsbSerial("VID_2341", "PID_0043");
      arduino = new RemoteDevice(connection);
      arduino.DeviceReady += Arduino_DeviceReady;


      connection.begin(57600, SerialConfig.SERIAL_8N1);

      timer = new DispatcherTimer();
      timer.Interval = TimeSpan.FromMilliseconds(100);
      timer.Tick += Timer_Tick;
      timer.Start();
    }

    private void Timer_Tick(object sender, object e)
    {
      if(arduinoReady)
      {
        UInt16 val = arduino.analogRead("A0");
        sliderArray01.Value = val;
      }
    }

    private void Arduino_DeviceReady()
    {
      //TODO
      var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
     {
       OnButton.IsEnabled = true;
       OffButton.IsEnabled = true;
     }));

      //set digital pin 13 to OUTPUT
      arduino.pinMode(13, PinMode.OUTPUT);

      //set analog pin A0 to ANALOG INPUT
      arduino.pinMode("A0", PinMode.ANALOG);

      arduinoReady = true;
    }

    private void OnButton_Click(object sender, RoutedEventArgs e)
    {
      arduino.digitalWrite(13, PinState.HIGH);
    }

    private void OffButton_Click(object sender, RoutedEventArgs e)
    {
      arduino.digitalWrite(13, PinState.LOW);
    }
  }
}
