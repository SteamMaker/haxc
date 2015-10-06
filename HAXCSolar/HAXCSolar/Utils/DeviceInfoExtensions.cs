using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

static class DeviceInfoExtensions
{
  public static string GetVID(this DeviceInformation DeviceInfo) {
    return ExtractStringBetween(DeviceInfo.Id,"VID", "&");
  }

  public static string GetPID(this DeviceInformation DeviceInfo)
  {
    return ExtractStringBetween(DeviceInfo.Id, "PID", "#");
  }

  private static string ExtractStringBetween(string Source, string Begin, string End)
  {
    int beginPos= Source.IndexOf(Begin);
    int endPos = Source.IndexOf(End, beginPos);

    return Source.Substring(beginPos, endPos - beginPos);

  }
}

