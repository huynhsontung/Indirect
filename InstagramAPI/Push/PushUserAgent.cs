﻿using InstagramAPI.Classes.Android;
using System;
using System.Collections.Generic;
using InstagramAPI.Classes.Core;

// ReSharper disable StringLiteralTypo

namespace InstagramAPI.Push
{
    internal sealed class PushUserAgent
    {
        const string FBNS_APPLICATION_NAME = "MQTT";
        const string INSTAGRAM_APPLICATION_NAME = "Instagram";  // for Realtime features

        public static string BuildFbUserAgent(AndroidDevice device, string appName = FBNS_APPLICATION_NAME)
        {
            var fields = new Dictionary<string, string>
            {
                {"FBAN", appName},
                {"FBAV", ApiVersion.Current.AppVersion},
                {"FBBV", ApiVersion.Current.AppVersionCode},
                {"FBDM",
                    $"{{density={Math.Round(device.Dpi / 160f, 1):F1},width={device.ScreenResolution.Width},height={device.ScreenResolution.Height}}}"
                },
                {"FBLC", Instagram.GetCurrentLocale() ?? "en_US"},
                {"FBCR", ""},   // We don't have cellular
                {"FBMF", device.HardwareManufacturer},
                {"FBBD", device.HardwareManufacturer},
                {"FBPN", ApiVersion.PackageName},
                {"FBDV", device.HardwareModel.Replace(" ", "")},
                {"FBSV", device.AndroidVersion.VersionNumber},
                {"FBLR", "0"},  // android.hardware.ram.low
                {"FBBK", "1"},  // Const (at least in 10.12.0)
                {"FBCA", AndroidDevice.CPU_ABI}
            };
            var mergeList = new List<string>();
            foreach (var field in fields)
            {
                mergeList.Add($"{field.Key}/{field.Value}");
            }

            var userAgent = "";
            foreach (var field in mergeList)
            {
                userAgent += field + ';';
            }

            return '[' + userAgent + ']';
        }
    }
}
