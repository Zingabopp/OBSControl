using OBSControl;
using OBSControl.OBSComponents;
using System;
using System.Threading.Tasks;

public class AudioDeviceDropdownEntry
{
    public string sourceKey;
    public string LongKeyName;
    public string deviceName = "default";
    public string HoverHint { get => this.IsAvailable ? string.Empty : "Device is disabled in OBS!"; }
    public string Color { get => this.IsAvailable ? "white" : "red"; }

    public bool IsAvailable = false;

    public static string notAvailableFormatter(string name)
    {
        return ($"<color=\"red\">{name} (N/A)</color>");
    }

    private AudioDevicesController? AudioDevicesController => OBSController.instance?.GetOBSComponent<AudioDevicesController>();

    public AudioDeviceDropdownEntry(string sourceKey, string longKeyName, bool available)
    {
        this.sourceKey = sourceKey;
        this.LongKeyName = longKeyName;
        this.IsAvailable = available;
    }
    public async Task TrySetDevice(string sourceKey, string shortDeviceName, bool isOutput)
    {
        try
        {
            var audioDevicesController = AudioDevicesController;
            if (audioDevicesController != null)
            {
                if (audioDevicesController.ActiveAndConnected)
                    await audioDevicesController.setSourceToDeviceByName(sourceKey, shortDeviceName, isOutput);
            }
            else
            {
                Logger.log?.Warn($"|ADC| Can't set device, we don't have an AudioDevicesController :( ");
            }
        }
        catch (Exception e)
        {
            Logger.log?.Warn($"|ADC| Something went very wrong while setting a device...");
            Logger.log?.Warn($"|ADC| {e}");
        }
    }

}
