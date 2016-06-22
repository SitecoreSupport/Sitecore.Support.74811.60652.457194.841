using LightLDAP.Caching;
using LightLDAP.Notification;
using LightLDAP.Notification.Events;
using Sitecore;
using Sitecore.Diagnostics;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LightLDAP.Support
{
    public class SitecoreADRoleProvider : LightLDAP.SitecoreADRoleProvider
    {
        MembershipResolver currentResolver;

        private bool _initialized;

        public SitecoreADRoleProvider() : base(new MembershipResolverFixed())
        { }
        public override string[] GetRolesForUser(string username)
        {
            string[] rolesForUser;
            try
            {
                rolesForUser = InnerGetRolesForUser(username);
            }
            catch (COMException ex)
            {
                Log.Info("Sitecore.Support.74811.60652.457194: COM Exception encountered in the GetRolesForUser method Refreshing AD cache and retrying", this);
                Log.Info(string.Concat(new object[]
                {
                    "Sitecore.Support.74811.60652.457194. Error code:",
                    ex.ErrorCode,
                    " | Message",
                    ex.Message
                }), this);
                this.ForceRefreshADCache();
                try
                {
                    rolesForUser = InnerGetRolesForUser(username);
                }
                catch (Exception ex2)
                {
                    Log.Info("Sitecore.Support.74811.60652.457194: AD Operation failed in the GetRolesForUser method", this);
                    throw ex2;
                }
            }
            return rolesForUser;
        }

        protected string[] InnerGetRolesForUser(string username)
        {
            if (!this._initialized)
            {
                return null;
            }
            if (currentResolver != null)
            {
                MembershipResolverFixed membershipResolverFixed = (MembershipResolverFixed)currentResolver;
                return membershipResolverFixed.GetRolesForUser(username).ToArray<string>();
            }
            else
            {
                return null;
            }
        }


        public override void Initialize(string name, NameValueCollection config)
        {
            try {
                base.Initialize(name, config);
                _initialized = (bool)typeof(LightLDAP.SitecoreADRoleProvider).GetField("isInitialized", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);

                FieldInfo field = GetType().BaseType.GetField("resolver", BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                currentResolver = (MembershipResolver)field.GetValue(this);
                PropertyInfo property = currentResolver.Source.GetType().GetProperty("NotificationProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                DirectoryNotificationProvider directoryNotificationProvider = property.GetValue(currentResolver.Source) as DirectoryNotificationProvider;

                if (directoryNotificationProvider != null)
                {
                    System.Reflection.MethodInfo onRoleAddedMethod = base.GetType().BaseType.GetMethod("OnRoleAdded", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    directoryNotificationProvider.ObjectCategoryAdded -= (onRoleAddedMethod.CreateDelegate(typeof(System.EventHandler<ObjectCategoryAddedEventArgs>), this) as System.EventHandler<ObjectCategoryAddedEventArgs>);

                    bool useNotification = MainUtil.GetBool(config["useNotification"] ?? "1", true);
                    if (useNotification)
                    {
                        directoryNotificationProvider.ObjectCategoryAdded += new System.EventHandler<ObjectCategoryAddedEventArgs>(this.OnRoleAddedWithCountCheck);
                    }
                    else
                    {
                        System.Reflection.MethodInfo onRoleDeletedMethod = base.GetType().BaseType.GetMethod("OnRoleDeleted", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        System.Reflection.MethodInfo onRoleModifiedMethod = base.GetType().BaseType.GetMethod("OnRoleModified", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                        directoryNotificationProvider.ObjectCategoryDeleted -= (onRoleDeletedMethod.CreateDelegate(typeof(System.EventHandler<ObjectCategoryDeletedEventArgs>), this) as System.EventHandler<ObjectCategoryDeletedEventArgs>);
                        directoryNotificationProvider.ObjectCategoryModified -= (onRoleModifiedMethod.CreateDelegate(typeof(System.EventHandler<ObjectCategoryModifiedEventArgs>), this) as System.EventHandler<ObjectCategoryModifiedEventArgs>);
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Warn("Sitecore.Support.74811.60652.457194 patch fails with the following error message during initializing the SitecoreADRoleProvider: " + ex.Message + ". The exception type is " + ex.GetType().ToString(), this);
            }
        }

        private void OnRoleAddedWithCountCheck(object sender, ObjectCategoryAddedEventArgs e)
        {
            string text = base.GetType().BaseType.GetField("fullOUEntryPath", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(this).ToString();
            RolesCache rolesCache = (RolesCache)base.GetType().BaseType.GetField("cache", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(this);
            if (rolesCache.InnerCache.Count != 0 && e.FullOUEntryPath.ToLower().EndsWith(text.ToLower()) && !string.IsNullOrEmpty(e.GroupType))
            {
                System.Security.Principal.SecurityIdentifier key = new System.Security.Principal.SecurityIdentifier((byte[])e.ObjectSID, 0);
                rolesCache.SetObject(key, e.UserName);
            }
        }

        protected virtual void ForceRefreshADCache()
        {
            try
            {
                LightLDAP.ActiveDirectory.DirectoryEntry directoryEntry = currentResolver.Root as LightLDAP.ActiveDirectory.DirectoryEntry;
                if (directoryEntry != null)
                {
                    directoryEntry.RefreshCache(new string[] { "tokenGroups" });
                }
            }
            catch (Exception exception)
            {
                Log.Error("Sitecore.Support.105901.457194: An error occured during SitecoreADRoleProvider.ForceRefreshADCache", exception, this);
            }
        }
    }
}
