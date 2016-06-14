using LightLDAP.ActiveDirectory;
using LightLDAP.Configurations;
using LightLDAP.Diagnostic;
using LightLDAP.Helpers;
using LightLDAP.Resources;
using LightLDAP.Utility;
using Sitecore.StringExtensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightLDAP.Support
{
    public class MembershipResolverFixed : LightLDAP.MembershipResolver
    {
        public new IEnumerable<string> GetRolesForUser(string username)
        {
            IEnumerable<string> enumerable = base.Source.GetParentsRoles(username);
            if (enumerable != null)
            {
                return enumerable.ToArray<string>();
            }
            string userPrimaryGroup = this.GetUserPrimaryGroup(username);
            bool indirectMembership = Settings.IndirectMembership;
            enumerable = this.GetUserMembership(username, userPrimaryGroup, indirectMembership);
            base.Source.SetParentsRoles(username, enumerable);
            return enumerable;
        }

        private string GetUserPrimaryGroup(string userName)
        {
            string text = StringExtensions.FormatWith("(&(objectCategory=person)({0}={1}))", new object[]
            {
                base.AttributeMapRoleName,
                EscapeHelper.EscapeCharacters(userName)
            });
            ISearchResult searchResult;
            using (IDirectorySearcher adSearcher = this.GetAdSearcher())
            {
                searchResult = DirectoryExtension.FindOne(adSearcher, base.Root, text, new string[]
                {
                    ObjectAttribute.PrimaryGroupID
                });
            }
            string text2 = null;
            if (searchResult != null)
            {
                ConditionLog.Debug(SR.GetString("USER_PRIMARY_GROUP_FOUND_USER"), new object[]
                {
                    userName
                });
                int @int = DataHelper.GetInt(searchResult, ObjectAttribute.PrimaryGroupID);
                IDirectoryEntry directoryEntry = searchResult.GetDirectoryEntry();
                directoryEntry.RefreshCache(new string[]
                {
                    ObjectAttribute.TokenGroups
                });
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("(|");
                foreach (byte[] array in directoryEntry.Properties[ObjectAttribute.TokenGroups])
                {
                    stringBuilder.AppendFormat("(objectSid={0})", StringUtil.BuildOctetString(array));
                }
                stringBuilder.Append(")");
                ConditionLog.Debug(SR.GetString("USER_PRIMARY_GROUP_START"), new object[]
                {
                    userName
                });
                ISearchResultCollection searchResultCollection;
                using (IDirectorySearcher adSearcher2 = this.GetAdSearcher())
                {
                    searchResultCollection = DirectoryExtension.SafeFindAll(adSearcher2, base.Root, stringBuilder.ToString(), new string[]
                    {
                        ObjectAttribute.PrimaryGroupToken,
                        ObjectAttribute.SAMAccountName
                    });
                }
                if (searchResultCollection != null)
                {
                    foreach (ISearchResult current in searchResultCollection)
                    {
                        int int2 = DataHelper.GetInt(current, ObjectAttribute.PrimaryGroupToken);
                        if (@int == int2)
                        {
                            text2 = DataHelper.GetString(current, ObjectAttribute.SAMAccountName);
                            ConditionLog.Debug(SR.GetString("USER_PRIMARY_GROUP_FOUND"), new object[]
                            {
                                text2,
                                userName
                            });
                            break;
                        }
                    }
                }
            }
            return text2;
        }

        private IEnumerable<string> GetUserMembership(string username, string primaryGroupName, bool includeIndirectMembership)
        {
            string text = StringExtensions.FormatWith("(&(objectCategory=person)({0}={1}))", new object[]
            {
                base.AttributeMapRoleName,
                EscapeHelper.EscapeCharacters(username)
            });
            List<string> list = new List<string>();
            IEnumerable<object> enumerable;
            using (IDirectorySearcher adSearcher = this.GetAdSearcher())
            {
                enumerable = DirectoryExtension.FindLargeAttributeRange(adSearcher, base.Root, text, ObjectAttribute.MemberOf);
            }
            if (enumerable != null)
            {
                ConditionLog.Debug(SR.GetString("USER_MEMBEROF_FOUND"), new object[]
                {
                    username
                });
                using (IEnumerator<object> enumerator = enumerable.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        string text2 = (string)enumerator.Current;
                        string sAMAccountNameByDN;
                        using (IDirectorySearcher adSearcher2 = this.GetAdSearcher())
                        {
                            sAMAccountNameByDN = DirectoryExtension.GetSAMAccountNameByDN(adSearcher2, base.Root, text2);
                        }
                        if (!string.IsNullOrEmpty(sAMAccountNameByDN))
                        {
                            list.Add(sAMAccountNameByDN);
                        }
                    }
                }
                if (!string.IsNullOrEmpty(primaryGroupName))
                {
                    ConditionLog.Debug(SR.GetString("GET_USER_MEMBERSHIP_PRIMARY"), new object[]
                    {
                        primaryGroupName,
                        username
                    });
                    list.Add(primaryGroupName);
                }
                if (includeIndirectMembership)
                {
                    ConditionLog.Debug(SR.GetString("GET_USER_MEMBERSHIP_INDIRECT"), new object[]
                    {
                        username
                    });
                    list.AddRange(this.GetUserMembership(list, null));
                }
            }
            return from s in list.Distinct<string>()
                   orderby s
                   select s;
        }

        private IEnumerable<string> GetUserMembership(IEnumerable<string> roleNames, List<string> allRoles = null)
        {
            if (allRoles == null)
            {
                allRoles = new List<string>();
            }
            foreach (string current in roleNames)
            {
                string text = StringExtensions.FormatWith("(&(objectCategory=group)({0}={1}))", new object[]
                {
                    ObjectAttribute.SAMAccountName,
                    EscapeHelper.EscapeCharacters(current)
                });
                ISearchResult searchResult;
                using (IDirectorySearcher adSearcher = this.GetAdSearcher())
                {
                    searchResult = DirectoryExtension.FindOne(adSearcher, base.Root, text, new string[]
                    {
                        ObjectAttribute.MemberOf
                    });
                }
                if (searchResult != null)
                {
                    ConditionLog.Debug(SR.GetString("GET_USER_MEMBERSHIP_NESTED"), new object[]
                    {
                        current
                    });
                    IEnumerable<string> enumerable;
                    using (IDirectorySearcher searcher = this.GetAdSearcher())
                    {
                        enumerable = from s in (from s in DataHelper.GetStrings(searchResult, ObjectAttribute.MemberOf)
                                                select DirectoryExtension.GetSAMAccountNameByDN(searcher, this.Root, s)).Distinct<string>()
                                     where !string.IsNullOrEmpty(s)
                                     select s;
                    }
                    IEnumerable<string> roleNames2 = enumerable.Except(allRoles).ToList<string>();
                    allRoles.AddRange(enumerable);
                    this.GetUserMembership(roleNames2, allRoles);
                }
            }
            return allRoles.Distinct<string>();
        }
    }
}
