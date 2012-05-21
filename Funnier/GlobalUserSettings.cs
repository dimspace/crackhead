using System;
using System.Collections.Generic;
using MonoTouchUtils;

namespace Funnier
{
    public class GlobalUserSettings
    {
        private readonly static GlobalUserSettings singleton = new GlobalUserSettings();

        public static GlobalUserSettings Get() {
            return singleton;
        }

        private readonly UserSettings userSettings;
        private readonly List<string> viewedPhotoIds = new List<string>();

        public string LastViewedCartoonId {
            get {
                return userSettings.LastViewedPhotoId;
            }
            set {
                userSettings.LastViewedPhotoId = value;
                viewedPhotoIds.Add(value);
            }
        }

        private const string PhotoSettingsUserDefaultsKeyName = "UserSettings";

        private GlobalUserSettings ()
        {
            userSettings = UserDefaultsUtils.LoadObject<UserSettings>(PhotoSettingsUserDefaultsKeyName);
            viewedPhotoIds.AddRange(userSettings.ViewedPhotoIds);
        }

        public void Save() {
            userSettings.ViewedPhotoIds = viewedPhotoIds.ToArray();
            UserDefaultsUtils.SaveObject(PhotoSettingsUserDefaultsKeyName, userSettings);
        }
    }
}

