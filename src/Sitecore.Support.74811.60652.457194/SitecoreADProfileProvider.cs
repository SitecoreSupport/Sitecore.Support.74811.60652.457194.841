using LightLDAP.DataSource;
using LightLDAP.Diagnostic;
using LightLDAP.Notification;
using LightLDAP.Notification.Events;
using Sitecore.Caching;
using Sitecore.Diagnostics;
using System.Reflection;

namespace LightLDAP.Support
{
    public class SitecoreADProfileProvider : LightLDAP.SitecoreADProfileProvider
    {
        protected override void InitializeSource(string connectionString, string userName, string password, bool useNotification)
        {
            try
            {
                System.Reflection.FieldInfo field = base.GetType().BaseType.GetField("attributeMapUsername", BindingFlags.Instance | BindingFlags.NonPublic);
                string attributeMapName = field.GetValue(this).ToString();
                PartialDataSource partialSource = DataStorageContainer.GetPartialSource(connectionString, userName, password, attributeMapName, true);
                PropertyInfo property = partialSource.GetType().GetProperty("NotificationProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                DirectoryNotificationProvider directoryNotificationProvider = (DirectoryNotificationProvider)property.GetValue(partialSource);

                if (directoryNotificationProvider != null && useNotification)
                {
                    directoryNotificationProvider.ObjectCategoryModified += new System.EventHandler<ObjectCategoryModifiedEventArgs>(this.OnUserChanged);
                }
            }
            catch (System.Exception ex)
            {
                Log.Warn("Sitecore.Support.74811.60652.457194 patch fails with the following error message during initializing the SitecoreADProfileProvider: " + ex.Message + ". The exception type is " + ex.GetType().ToString(), this);
            }
        }

        private void OnUserChanged(object sender, ADObjectEventArgs e)
        {
            string arg = GetType().BaseType.GetField("sitecoreMapDomainName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this).ToString();
            string text = string.Format("{0}\\{1}", arg, e.UserName);
            CacheManager.ClearUserProfileCache(text);
            ConditionLog.Debug("SitecoreADProfileProvider.OnUserChanged - UserProfileCache is cleared for user: '{0}'", new object[]
            {
                text
            });
        }
    }
}
