using BPUtil;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlueIrisRegistryReader
{
	public class BlueIrisConfiguration
	{
		public byte[] BiVersionBytes = new byte[4];
		public string BiVersionFromRegistry;
		public string OS;
		public string AdvisorVersion;
		public BIGlobalConfig global;
		public SortedList<string, Camera> cameras = new SortedList<string, Camera>();
		public List<GpuInfo> gpus;
		public CpuInfo cpu;
		public RamInfo mem;
		public BIActiveStats activeStats;
		public BlueIrisConfiguration()
		{
		}

		public void Load()
		{
			int version = RegistryUtil.GetHKLMValue<int>(@"SOFTWARE\Perspective Software\Blue Iris", "version", 0);
			ByteUtil.WriteInt32(version, BiVersionBytes, 0);
			BiVersionFromRegistry = string.Join(".", BiVersionBytes);
			OS = GetOsVersion();
			AdvisorVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

			RegistryKey camerasKey = RegistryUtil.GetHKLMKey(@"SOFTWARE\Perspective Software\Blue Iris\Cameras");
			if (camerasKey != null)
			{
				global = new BIGlobalConfig();
				global.Load();


				SortedList<string, Camera> camerasByShortname = new SortedList<string, Camera>();
				foreach (string camName in camerasKey.GetSubKeyNames())
				{
					Camera cam = new Camera(camName, camerasKey.OpenSubKey(camName));
					cameras.Add(camName, cam);
					// Make a copy of [cam] to put into the second SortedList, because later we are going to be modifying Camera objects in the first SortedList.
					camerasByShortname.Add(cam.shortname, JsonConvert.DeserializeObject<Camera>(JsonConvert.SerializeObject(cam)));
				}

				{
					// Implement "sync" functionality where some cameras and some profiles can point at other cameras and/or profiles.
					// Blue Iris totally allows circular references when syncing between cameras, with undefined and unexplored behavior, so we'll only be implementing one iteration of syncing.

					// First sync the recordSettings
					foreach (Camera cam in cameras.Values)
					{
						Camera syncFrom = cam;
						if (cam.recordSettings[1].sync)
						{
							// Profile 1 says to sync.  This means every profile with the sync flag set will be synced from a different camera.
							if (cam.recordSettings[1].camsync != null)
							{
								if (camerasByShortname.TryGetValue(cam.recordSettings[1].camsync, out Camera tmp))
								{
									syncFrom = tmp;
								}
							}
						}
						for (int i = 1; i <= 7; i++)
						{
							if (cam.recordSettings[i].sync)
								cam.recordSettings[1] = syncFrom.recordSettings[i];
						}
					}
					// Then sync the triggerSettings
					foreach (Camera cam in cameras.Values)
					{
						Camera syncFrom = cam;
						if (cam.triggerSettings[1].sync)
						{
							// Profile 1 says to sync.  This means every profile with the sync flag set will be synced from a different camera.
							if (cam.triggerSettings[1].camsync != null)
							{
								if (camerasByShortname.TryGetValue(cam.triggerSettings[1].camsync, out Camera tmp))
								{
									syncFrom = tmp;
								}
							}
						}
						for (int i = 1; i <= 7; i++)
						{
							if (cam.triggerSettings[i].sync)
								cam.triggerSettings[1] = syncFrom.triggerSettings[i];
						}
					}
				}

				cpu = CpuInfo.GetCpuInfo();
				gpus = GpuInfo.GetGpuInfo();
				mem = RamInfo.GetRamInfo();

				activeStats = new BIActiveStats();
				activeStats.Load();
			}
		}

		public static string GetOsVersion()
		{
			StringBuilder sb = new StringBuilder();
			string prodName = RegistryUtil.GetHKLMValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "Unknown");
			sb.Append(prodName);

			string release = RegistryUtil.GetHKLMValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "");
			if (string.IsNullOrWhiteSpace(release))
				sb.Append(" v" + RegistryUtil.GetHKLMValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentVersion", "Unknown"));
			else
			{
				sb.Append(" v" + release);
				string build = RegistryUtil.GetHKLMValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber", "");
				if (string.IsNullOrWhiteSpace(build))
					build = RegistryUtil.GetHKLMValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber", "");
				if (!string.IsNullOrWhiteSpace(build))
					sb.Append(" b" + build);
			}
			if (Environment.Is64BitOperatingSystem)
				sb.Append(" (64 bit)");
			else
				sb.Append(" (32 bit)");
			return sb.ToString();
		}
	}
}