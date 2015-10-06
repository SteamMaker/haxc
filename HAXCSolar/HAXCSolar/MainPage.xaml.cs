using Microsoft.Maker.RemoteWiring;
using Microsoft.Maker.Serial;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
  public sealed partial class MainPage : Page, INotifyPropertyChanged
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

    private int a0;

    public int A0
    {
      get { return a0; }
      set { SetProperty(ref a0, value); }
    }

    private int a1;

    public int A1
    {
      get { return a1; }
      set { SetProperty(ref a1, value); }
    }

    private int a2;

    public int A2
    {
      get { return a2; }
      set { SetProperty(ref a2, value); }
    }

    private int a3;

    public int A3
    {
      get { return a3; }
      set { SetProperty(ref a3, value); }
    }




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
        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
        {
          rpmText.Text = rpm.ToString();
        }));
        pulseCount = 0;

      }
    }

    private async void Arduino_AnalogPinUpdated(byte pin, ushort value)
    {
      switch (pin)
      {
        case 0:
          await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
          {
            this.A0 = (int)Map(value, minLight, maxLight, 0, 100);
          }));
          break;
        case 1:
          await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
          {
            this.A1 = (int)Map(value, minLight, maxLight, 0, 100);
          }));
          break;
        case 2:
          await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
          {
            this.A2 = (int)Map(value, minLight, maxLight, 0, 100);
          }));
          break;
        case 3:
          await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
          {
            this.A3 = (int)Map(value, minLight, maxLight, 0, 100);
          }));
          break;
        default:
          break;
      }

    }

    private void Arduino_DigitalPinUpdated(byte pin, PinState state)
    {
      if (pin == 2 && state == PinState.HIGH)
      {
        pulseCount++;
      }
    }

    private void Timer_Tick(object sender, object e)
    {
      if (arduinoReady)
      {
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
        //points.Add(new Point(p * pointStep, rnd.Next(maxY)));
        points.Add(new Point(p * pointStep, 0));
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
      //lastPoint.Y = rnd.Next(maxY);
      lastPoint.Y = maxY - Map(A0,0,100,0, maxY);
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

    #region INotifyPropertyChanged Implementation

    /// <summary>
    ///     Multicast event for property change notifications.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    ///     Checks if a property already matches a desired value.  Sets the property and
    ///     notifies listeners only when necessary.
    /// </summary>
    /// <typeparam name="T">Type of the property.</typeparam>
    /// <param name="storage">Reference to a property with both getter and setter.</param>
    /// <param name="value">Desired value for the property.</param>
    /// <param name="propertyName">
    ///     Name of the property used to notify listeners.  This
    ///     value is optional and can be provided automatically when invoked from compilers that
    ///     support CallerMemberName.
    /// </param>
    /// <returns>
    ///     True if the value was changed, false if the existing value matched the
    ///     desired value.
    /// </returns>
    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
    {
      if (Equals(storage, value))
      {
        return false;
      }

      storage = value;
      this.OnPropertyChanged(propertyName);
      return true;
    }

    /// <summary>
    ///     Notifies listeners that a property value has changed.
    /// </summary>
    /// <param name="propertyName">
    ///     Name of the property used to notify listeners.  This
    ///     value is optional and can be provided automatically when invoked from compilers
    ///     that support <see cref="CallerMemberNameAttribute" />.
    /// </param>
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChangedEventHandler eventHandler = this.PropertyChanged;
      if (eventHandler != null)
      {
        eventHandler(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    #endregion INotifyPropertyChanged Implementation



  }
}
