using System;

public class AudioDeviceDropdownEntry
{
	public string sourceKey;
	public string deviceName = "default";
	public string HoverHint {
		get => this.IsAvailable ? null : "Device is disabled in OBS!";
	}

    public bool IsAvailable = false;

	public AudioDeviceDropdownEntry(string sourceKey, bool available)
	{
		this.sourceKey = sourceKey;
		this.IsAvailable = available;
	}
}
