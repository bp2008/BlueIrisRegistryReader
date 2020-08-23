using BPUtil;
using Microsoft.Win32;

namespace BlueIrisRegistryReader
{
	public class TriggerTabSettings
	{
		/// <summary>
		/// If not null or empty, these settings come from another camera.  In that case, advice related to settings in this tab should not be shown, and fixes should not be performed on these settings.
		/// </summary>
		public string camsync;
		public bool motionDetectionEnabled;
		/// <summary>
		/// The profile number which these settings came from. May not match the profile index where you got this object instance from.
		/// </summary>
		public int profile;
		public bool sync;

		public TriggerTabSettings(RegistryKey cameraKey, int profile)
		{
			this.profile = profile;
			RegistryKey motionKey = cameraKey.OpenSubKey("Motion");

			// Read settings from trigger tab.
			// As of BI 5.3.1.6 there appears to be a quirk for the "Motion" key where profile 1 is stored at the root and the other profiles are stored in the subkeys named by profile - 1.
			RegEdit trigger;
			if (profile == 1)
				trigger = new RegEdit(motionKey);
			else
				trigger = new RegEdit(motionKey.OpenSubKey((profile - 1).ToString()));


			this.sync = trigger.DWord("sync") > 0; // If > 0, most of the other settings in this object should be ignored.
			this.camsync = trigger.String("camsync");
			this.motionDetectionEnabled = trigger.DWord("enabled") == 1;
		}
	}
	public enum RecordingTriggerType : byte
	{
		Motion = 0,
		Continuous = 1,
		NoRecording = 2,
		Periodic = 3,
		TriggeredAndPeriodic = 4,
		TriggeredAndContinuous = 5
	}
}