using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Caching;

namespace coursework_2.Models
{
    public static class SettingsService
    {
        private const string CacheKey = "HealthTrack_SystemSettings";
        private const string ZoneCacheKey = "HealthTrack_TimeZone";

        public static SystemSetting Current
        {
            get
            {
                var cached = HttpRuntime.Cache[CacheKey] as SystemSetting;
                if (cached != null) return cached;

                using (var db = new HealthTrackContext())
                {
                    var settings = db.SystemSettings.AsNoTracking().FirstOrDefault();

                    if (settings == null)
                    {
                        settings = new SystemSetting
                        {
                            CompanyName = "HealthTrack",
                            Tagline = "Personal Health & Fitness Management",
                            CancellationWindowHours = 24,
                            TimeZoneId = TimeZoneInfo.Local.Id,
                            SmsApiUrl = SmsService.DefaultApiUrl,
                            UpdatedAt = DateTime.UtcNow
                        };

                        db.SystemSettings.Add(settings);
                        db.SaveChanges();
                    }

                    HttpRuntime.Cache.Insert(CacheKey, settings, null,
                        DateTime.Now.AddMinutes(10), Cache.NoSlidingExpiration);

                    return settings;
                }
            }
        }

        public static TimeZoneInfo TimeZone
        {
            get
            {
                var cached = HttpRuntime.Cache[ZoneCacheKey] as TimeZoneInfo;
                if (cached != null) return cached;

                TimeZoneInfo zone;
                try
                {
                    zone = TimeZoneInfo.FindSystemTimeZoneById(Current.TimeZoneId ?? TimeZoneInfo.Local.Id);
                }
                catch
                {
                    zone = TimeZoneInfo.Local;
                }

                HttpRuntime.Cache.Insert(ZoneCacheKey, zone, null,
                    DateTime.Now.AddMinutes(10), Cache.NoSlidingExpiration);

                return zone;
            }
        }

        public static DateTime Now
        {
            get { return FromUtc(DateTime.UtcNow); }
        }

        public static DateTime FromUtc(DateTime utc)
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZone);
            }
            catch { return utc; }
        }

        public static DateTime ToDisplayTime(DateTime serverLocal)
        {
            try
            {
                DateTime utc = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(serverLocal, DateTimeKind.Unspecified), TimeZoneInfo.Local);

                return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone);
            }
            catch { return serverLocal; }
        }

        public static DateTime ToServerTime(DateTime configuredLocal)
        {
            try
            {
                DateTime utc = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(configuredLocal, DateTimeKind.Unspecified), TimeZone);

                return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);
            }
            catch { return configuredLocal; }
        }

        public static void Clear()
        {
            HttpRuntime.Cache.Remove(CacheKey);
            HttpRuntime.Cache.Remove(ZoneCacheKey);
        }
    }
}