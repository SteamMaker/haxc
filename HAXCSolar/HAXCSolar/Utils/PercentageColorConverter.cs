using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace HAXCSolar
{
  class PercentageColorConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, string language)
    {

      bool notInverted = true;
      if (parameter != null)
      {
        bool.TryParse(parameter.ToString(), out notInverted);
      }

      //Debug.WriteLine("value type: ", value.GetType().Name);

      double percentage = 0;
      double.TryParse(value.ToString(), out percentage);

      byte lowColorByte = (byte)(((100 - percentage) / 100) * 255);
      byte highColorByte = (byte)((percentage / 100) * 255);

      byte red = notInverted ? lowColorByte : highColorByte;
      byte green = notInverted ? highColorByte : lowColorByte;
      byte blue = 0;
      return new SolidColorBrush(Color.FromArgb(255, red, green, blue));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      throw new NotImplementedException();
    }
  }
}
