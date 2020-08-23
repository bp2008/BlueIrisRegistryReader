using BPUtil;
using Microsoft.Win32;

namespace BlueIrisRegistryReader
{
	public class RecordTabSettings
	{
		/// <summary>
		/// If not null or empty, these settings come from another camera.  In that case, advice related to settings in this tab should not be shown, and fixes should not be performed on these settings.
		/// </summary>
		public string camsync;
		public bool DirectToDisc;
		public RecordingFormat recordingFormat;
		/// <summary>
		/// Codec used for video clips.  May be inaccurate or unset if DirectToDisc is true.
		/// </summary>
		public string VCodec;
		public RecordingTriggerType triggerType;
		/// <summary>
		/// The profile number which these settings came from. May not match the profile index where you got this object instance from.
		/// </summary>
		public int profile;
		public bool sync;

		public RecordTabSettings(RegistryKey cameraKey, int profile)
		{
			this.profile = profile;
			RegistryKey clipsKey = cameraKey.OpenSubKey("Clips");
			// Read settings from record tab.
			RegEdit record = new RegEdit(clipsKey.OpenSubKey(profile.ToString()));

			this.sync = record.DWord("sync") > 0; // If > 0, most of the other settings in this object should be ignored.
			this.camsync = record.String("camsync");
			this.DirectToDisc = record.DWord("transcode") == 0;
			this.recordingFormat = (RecordingFormat)record.DWord("movieformat");
			this.VCodec = record.String("vcodec");
			this.triggerType = (RecordingTriggerType)record.DWord("continuous");
		}
	}
	public enum RecordingFormat : byte
	{
		AVI = 0,
		BVR = 1,
		WMV = 2,
		MP4 = 3
	}
}