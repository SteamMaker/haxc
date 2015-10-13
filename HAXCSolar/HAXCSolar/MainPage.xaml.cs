using Microsoft.Maker.RemoteWiring;
using Microsoft.Maker.Serial;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
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

    #region Fields

    //Used to connect to the arduino via the Windows-Remote-Wiring library
    IStream connection;
    RemoteDevice arduino;
    DispatcherTimer timer;
    bool arduinoReady = false;

    //Used to set the scale of the sliders and text readouts for the analog sensors
    //UInt16 minLight = 675;     //This should map to the min value based on ambient light.  
    //UInt16 maxLight = 1023;  //This should always be the max analog input value possible.  1023 on an Uno

    //Used for the fake graph at the bottom of the screen
    PointCollection points;
    int pointStep = 0;
    int numPoints = 0;
    int maxY = 0;

    //Used for the flow meter testing
    //UInt16 pulseCount = 0;
    //long lastMillis = 0;
    //long sampleMillis = 250;

    #endregion Fields

    #region Properties

    private ConnectedDevicePresenter connectedDevicePresenter;

    public ConnectedDevicePresenter ConnectedDevicePresenter
    {
      get { return connectedDevicePresenter; }
      set { SetProperty(ref connectedDevicePresenter, value); }
    }

    private int a0;

    public int A0
    {
      get { return a0; }
      set
      {
        int newValue = (a0 + value) / 2;
        SetProperty(ref a0, newValue);
      }
    }

    private double a0Min = 675;  //675 was about ambient light inside the workshop

    public double A0Min
    {
      get { return a0Min; }
      set { SetProperty(ref a0Min, value); }
    }

    private double a0Max = 1023;

    public double A0Max
    {
      get { return a0Max; }
      set { SetProperty(ref a0Max, value); }
    }

    private int a1;

    public int A1
    {
      get { return a1; }
      set
      {
        int newValue = (a1 + value) / 2;
        SetProperty(ref a1, newValue);
      }
    }

    private double a1Min = 675;  //675 was about ambient light inside the workshop

    public double A1Min
    {
      get { return a1Min; }
      set { SetProperty(ref a1Min, value); }
    }

    private double a1Max = 1023;

    public double A1Max
    {
      get { return a1Max; }
      set { SetProperty(ref a1Max, value); }
    }

    private int a2;

    public int A2
    {
      get { return a2; }
      set
      {
        int newValue = (a2 + value) / 2;
        SetProperty(ref a2, newValue);
      }
    }

    private double a2Min = 512;  //0 = 0A for Current Sensor

    public double A2Min
    {
      get { return a2Min; }
      set
      {
        SetProperty(ref a2Min, value);
      }
    }

    private double a2Max = 1023;  //5A for a 5A Current Sensor

    public double A2Max
    {
      get { return a2Max; }
      set
      {
        SetProperty(ref a2Max, value);
      }
    }

    private int a3;

    public int A3
    {
      get { return a3; }
      set
      {
        int newValue = (a3 + value) / 2;
        SetProperty(ref a3, newValue);
      }
    }

    private double a3Min = 0;  //0 = 0v = dead battery

    public double A3Min
    {
      get { return a3Min; }
      set { SetProperty(ref a3Min, value); }
    }

    private double a3Max = 555; //Should be about 13v for a 24v Sensor

    public double A3Max
    {
      get { return a3Max; }
      set { SetProperty(ref a3Max, value); }
    }

    private DeviceInformation arduinoInfo;

    public DeviceInformation ArduinoInfo
    {
      get { return arduinoInfo; }
      set { arduinoInfo = value; }
    }

    #endregion Properties

    public MainPage()
    {
      this.InitializeComponent();

      ConnectedDevicePresenter = new ConnectedDevicePresenter(Dispatcher);
      ConnectedDevicePresenter.DevicesUpdated += ConnectedDevicePresenter_DevicesUpdated;

      timer = new DispatcherTimer();
      timer.Interval = TimeSpan.FromMilliseconds(100);
      timer.Tick += Timer_Tick;
      timer.Start();

    }

    private void ConnectedDevicePresenter_DevicesUpdated(object sender, EventArgs e)
    {
      InitArduino();
    }

    private void InitArduino()
    {
      //First find the arduino in the devices collection
      if (ConnectedDevicePresenter.Devices != null)
      {
        //Was searching by device name...
        //this.ArduinoInfo = ConnectedDevicePresenter.Devices.FirstOrDefault((d) => d.Name.ToLower().StartsWith("arduino"));
        //However, found that the name doesn't always start with "Arduino" so changing to see if searching for the PID works better...
        this.ArduinoInfo = ConnectedDevicePresenter.Devices.FirstOrDefault((d) => d.GetPID().ToUpper() == "PID_0043");
        if (ArduinoInfo != null)
        {
          connection = new UsbSerial(ArduinoInfo.GetVID(), ArduinoInfo.GetPID());

          arduino = new RemoteDevice(connection);
          arduino.DeviceReady += Arduino_DeviceReady;
          arduino.DigitalPinUpdated += Arduino_DigitalPinUpdated;
          arduino.AnalogPinUpdated += Arduino_AnalogPinUpdated;

          connection.begin(57600, SerialConfig.SERIAL_8N1);

        }
      }
    }

    /// <summary>
    /// Used to calcuate the RPM on a connected flow meter
    /// </summary>
    //private async void CalcRpm()
    //{
    //  long millis = ((App)App.Current).Stopwatch.ElapsedMilliseconds;
    //  double elapsedMillis = millis - lastMillis;
    //  if (elapsedMillis >= sampleMillis)
    //  {
    //    lastMillis = millis;
    //    long rpm = (long)(pulseCount * (60000 / elapsedMillis));
    //    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
    //    {
    //      rpmText.Text = rpm.ToString();
    //    }));
    //    pulseCount = 0;

    //  }
    //}

    /// <summary>
    /// Handles the AnalogPinUpdated event for the connected arduino
    /// </summary>
    /// <param name="pin">The pin that was updated</param>
    /// <param name="value">the value on the pin</param>
    private async void Arduino_AnalogPinUpdated(byte pin, ushort value)
    {
      switch (pin)
      {
        case 0:
          await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
          {
            this.A0 = (int)Map(value, A0Min, A0Max, 0, 100);
              //Debug.WriteLine("Pin: {0}, Value: {1}, Mapped: {2}", pin, value, A0);
            }));
          break;
        case 1:
          await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
          {
            this.A1 = (int)Map(value, A1Min, A1Max, 0, 100);
          }));
          break;
        case 2:
          await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
          {
            this.A2 = (int)Map(value, A2Min, A2Max, 0, 100);
          }));
          break;
        case 3:
          await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
          {
            this.A3 = (int)Map(value, A3Min, A3Max, 0, 100);
          }));
          break;
        default:
          break;
      }
      //Debug.WriteLine("Pin: {0} Value: {1}", pin, value);

    }

    private void Arduino_DigitalPinUpdated(byte pin, PinState state)
    {
      ////Used for sample hall effect flow meter testing
      //if (pin == 2 && state == PinState.HIGH)
      //  {
      //    pulseCount++;
      //  }

      //Supports a "calibration" button to set the slider ranges to match current ambient light. 
      if (pin == 3)
      {
        arduino.digitalWrite(13, state);
        SetMinLight();
      }
    }

    private void SetMinLight()
    {
      //minLight = (ushort)Math.Min(Math.Min(Math.Min(A0, A1), A2), A3);
      //minLight = arduino.analogRead("A0");
    }


    /// <summary>
    /// The event handler for the dispatcher timer
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Timer_Tick(object sender, object e)
    {
      if (arduinoReady)
      {
        UpdateGraph();

        //Calc RPM is used for the hall effect flow meter testing. 
        //CalcRpm();
      }
    }

    /// <summary>
    /// Used to map an value from an input range to an output range
    /// </summary>
    /// <param name="Value">Value to be mapped</param>
    /// <param name="InMin">Input range minimum value</param>
    /// <param name="InMax">Input range maximum value</param>
    /// <param name="OutMin">Output range minimum value</param>
    /// <param name="OutMax">Output range maximum value</param>
    /// <returns></returns>
    private double Map(double Value, double InMin, double InMax, double OutMin, double OutMax)
    {
      double val = (Value - InMin) * (OutMax - OutMin) / (InMax - InMin) + OutMin;
      return Math.Min(Math.Max(val, OutMin), OutMax);
    }


    /// <summary>
    /// Updates the display of the graph at the bottom of the screen
    /// </summary>
    private void UpdateGraph()
    {
      if (points == null)
      {
        InitPoints();
      }
      else
      {
        ShiftPoints();
      }
    }


    /// <summary>
    /// Initializes the collection of points of the graph at the bottom of the screen
    /// </summary>
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
        points.Add(new Point(p * pointStep, maxY));
      }

      fakeGraph.Points = points;
    }


    /// <summary>
    /// Shifts the graph point values to the left, and adds a new value at the end
    /// </summary>
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
      lastPoint.Y = maxY - Map(A0, 0, 100, 0, maxY);
      points[lastPointNum] = lastPoint;

      fakeGraph.Points = points;
    }

    /// <summary>
    /// Sets up the connected arduino pins for use
    /// </summary>
    private void Arduino_DeviceReady()
    {
      //set digital pin 13 to OUTPUT
      arduino.pinMode(13, PinMode.OUTPUT);

      //set digital pin 13 to OUTPUT

      //Set digital pins 2 and 3 to INPUT
      arduino.pinMode(2, PinMode.INPUT);
      arduino.pinMode(3, PinMode.INPUT);

      //set analog pin A0-A3 to ANALOG INPUT
      arduino.pinMode("A0", PinMode.ANALOG);
      arduino.pinMode("A1", PinMode.ANALOG);
      arduino.pinMode("A2", PinMode.ANALOG);
      arduino.pinMode("A3", PinMode.ANALOG);

      //Set the internal flag to indicate that the arduino device is ready for use.
      arduinoReady = true;
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

    private void PopupCloseButton_Click(object sender, RoutedEventArgs e)
    {
      SettingsPopup.IsOpen = false;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
      SettingsPopup.IsOpen = true;
    }
  }
}
