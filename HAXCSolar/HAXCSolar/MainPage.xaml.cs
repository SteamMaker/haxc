using Microsoft.Maker.RemoteWiring;
using Microsoft.Maker.Serial;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    PointCollection points;

    UInt16 minLight = 675;
    UInt16 maxLight = 1023;

    int pointStep = 0;
    int numPoints = 0;
    int maxY = 0;

    UInt16 pulseCount = 0;
    long lastMillis = 0;
    long sampleMillis = 250;


    public MainPage()
    {
      this.InitializeComponent();

      //USB\VID_2A03&PID_0043
      //"VID_2341", "PID_0043"
      //"VID_2A03", "PID_0043"
      //connection = new UsbSerial("VID_2341", "PID_0043");
      connection = new UsbSerial("VID_2A03", "PID_0043");
      arduino = new RemoteDevice(connection);
      arduino.DeviceReady += Arduino_DeviceReady;
      arduino.DigitalPinUpdated += Arduino_DigitalPinUpdated;
      arduino.AnalogPinUpdated += Arduino_AnalogPinUpdated;


      connection.begin(57600, SerialConfig.SERIAL_8N1);

      timer = new DispatcherTimer();
      timer.Interval = TimeSpan.FromMilliseconds(100);
      timer.Tick += Timer_Tick;
      timer.Start();

    }

    private async void CalcRpm()
    {
      long millis = ((App)App.Current).Stopwatch.ElapsedMilliseconds;
      double elapsedMillis = millis - lastMillis;
      if (elapsedMillis >= sampleMillis)
      {
        lastMillis = millis;
        long rpm = (long)(pulseCount * (60000 / elapsedMillis));
        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler( () => {
          rpmText.Text = rpm.ToString();
        }));
        pulseCount = 0;

      }
    }

    private void Arduino_AnalogPinUpdated(byte pin, ushort value)
    {
      //if (pin == 0)
      //{
      //  Debug.WriteLine("Analog Pin: {0}  Value: {1}", pin, value);
      //}

    }

    private void Arduino_DigitalPinUpdated(byte pin, PinState state)
    {
      if (pin == 2 && state == PinState.HIGH) {
        pulseCount++;
      }
      //Debug.WriteLine("Digital Pin: {0}  State: {1}", pin, state);
    }

    private void Timer_Tick(object sender, object e)
    {
      if (arduinoReady)
      {
        UInt16 val01 = arduino.analogRead("A0");
        UInt16 val02 = arduino.analogRead("A1");
        UInt16 val03 = arduino.analogRead("A2");
        UInt16 val04 = arduino.analogRead("A3");

        var mappedVal01 = (int)Map(val01, minLight, maxLight, 0, 100);
        var mappedVal02 = (int)Map(val02, minLight, maxLight, 0, 100);
        var mappedVal03 = (int)Map(val03, minLight, maxLight, 0, 100);
        var mappedVal04 = (int)Map(val04, minLight, maxLight, 0, 100);

        textArray01.Text = mappedVal01.ToString();
        textArray02.Text = mappedVal02.ToString();
        textUseage.Text = mappedVal03.ToString();
        textReserve.Text = mappedVal04.ToString();

        sliderArray01.Value = mappedVal01;
        sliderArray02.Value = mappedVal02; ;
        sliderUseage.Value = mappedVal03; ;
        sliderReserve.Value = mappedVal04; ;

        PinState pin2State = arduino.digitalRead(2);
        arduino.digitalWrite(13, pin2State);

        if (points == null)
        {
          InitPoints();
        }
        else
        {
          ShiftPoints();
        }

        CalcRpm();
      }
    }

    private double Map(double Value, double InMin, double InMax, double OutMin, double OutMax)
    {
      //return (value - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
      double val = (Value - InMin) * (OutMax - OutMin) / (InMax - InMin) + OutMin;
      return Math.Min(Math.Max(val, OutMin), OutMax);

    }

    private void InitPoints()
    {
      Random rnd = new Random();
      points = new PointCollection();

      pointStep = 10;

      numPoints = (int)((fakeGraphCanvas.ActualWidth - 25) / pointStep);
      maxY = (int)fakeGraphCanvas.ActualHeight;

      for (int p = 0; p <= numPoints; p++)
      {
        points.Add(new Point(p * pointStep, rnd.Next(maxY)));
      }

      fakeGraph.Points = points;
    }


    private void ShiftPoints()
    {
      Random rnd = new Random();

      int lastPointNum = points.Count - 1;

      for (int p = 0; p < lastPointNum; p++)
      {
        Point point = points[p];
        Point nextPoint = points[p + 1];
        point.Y = nextPoint.Y;
        points[p] = point;
        points[p + 1] = nextPoint;
      }

      Point lastPoint = (Point)points[lastPointNum];
      lastPoint.Y = rnd.Next(maxY);
      points[lastPointNum] = lastPoint;

      fakeGraph.Points = points;

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
      arduino.pinMode(2, PinMode.INPUT);

      //set analog pin A0 to ANALOG INPUT
      arduino.pinMode("A0", PinMode.ANALOG);
      arduino.pinMode("A1", PinMode.ANALOG);
      arduino.pinMode("A2", PinMode.ANALOG);
      arduino.pinMode("A3", PinMode.ANALOG);


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
