﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using Composite.Logging;
using Composite.Linq;
using Composite.Instrumentation;
using Composite.GlobalSettings;
using Composite.Users;


namespace Composite.Security
{
    internal static class EntityTokenCacheFacade
    {
        private sealed class CacheKey
        {
            public string Username { get; set; }
            public EntityToken EntityToken { get; set; }


            public override bool Equals(object obj)
            {
                return Equals(obj as CacheKey);
            }


            public bool Equals(CacheKey cacheKey)
            {
                if (cacheKey == null) return false;

                return
                    cacheKey.Username.Equals(this.Username) &&
                    cacheKey.EntityToken.Equals(this.EntityToken);
            }


            public override int GetHashCode()
            {
                return this.Username.GetHashCode() ^ this.EntityToken.GetHashCode();
            }
        }


        private static ConcurrentDictionary<CacheKey, CacheEntry> _nativeCache = new ConcurrentDictionary<CacheKey, CacheEntry>();
        private static ConcurrentDictionary<CacheKey, CacheEntry> _hookingCache = new ConcurrentDictionary<CacheKey, CacheEntry>();
        private static object _lock = new object();

        private static bool Enabled { get; set; }
        private static int MaxSize { get; set; }



        private const int DefaultSize = 50000;


        static EntityTokenCacheFacade()
        {
            CachingSettings cachingSettings = GlobalSettingsFacade.GetNamedCaching("Entity token parents");
            Enabled = cachingSettings.Enabled;
            MaxSize = cachingSettings.GetSize(DefaultSize);
        }
        


        public static void AddNativeCache(EntityToken entityToken, IEnumerable<EntityToken> parentEntityTokens)
        {
            if ((Enabled == false) || (UserValidationFacade.IsLoggedIn() == false)) return;

            CacheEntry cacheEntry = new CacheEntry(entityToken)
            {
                ParentEntityTokens = parentEntityTokens.EvaluateOrNull(),
                Timestamp = DateTime.Now
            };

            CacheKey cacheKey = new CacheKey { Username = ResolveUsername(), EntityToken = entityToken };

            _nativeCache.TryAdd(cacheKey, cacheEntry);

            if (_nativeCache.Count > MaxSize)
            {
                _nativeCache = new ConcurrentDictionary<CacheKey, CacheEntry>();
            }
        }



        public static void AddHookingCache(EntityToken entityToken, IEnumerable<EntityToken> parentEntityTokens)
        {
            if ((Enabled == false) || (UserValidationFacade.IsLoggedIn() == false)) return;

            CacheEntry cacheEntry = new CacheEntry(entityToken)
            {
                ParentEntityTokens = parentEntityTokens.EvaluateOrNull(),
                Timestamp = DateTime.Now
            };

            CacheKey cacheKey = new CacheKey { Username = ResolveUsername(), EntityToken = entityToken };

            _hookingCache.TryAdd(cacheKey, cacheEntry);

            if (_hookingCache.Count > MaxSize)
            {
                _hookingCache = new ConcurrentDictionary<CacheKey, CacheEntry>();
            }
        }



        public static bool GetCachedNativeParents(EntityToken entityToken, out IEnumerable<EntityToken> parentEntityTokens)
        {
            if (!Enabled == false)
            {
                parentEntityTokens = null;
                return false;
            }

            string userName = UserValidationFacade.IsLoggedIn() ? ResolveUsername() : null;

            return GetCachedNativeParents(entityToken, out parentEntityTokens, userName);
        }

        internal static bool GetCachedNativeParents(EntityToken entityToken, out IEnumerable<EntityToken> parentEntityTokens, string userName)
        {
            if (!Enabled || userName == null)
            {
                parentEntityTokens = null;
                return false;
            }

            CacheKey cacheKey = new CacheKey { Username = userName, EntityToken = entityToken };

            CacheEntry cacheEntry;
            if (_nativeCache.TryGetValue(cacheKey, out cacheEntry) == true)
            {
                PerformanceCounterFacade.EntityTokenParentCacheHitIncrement();
                parentEntityTokens = cacheEntry.ParentEntityTokens;
                return true;                
            }
            else
            {
                PerformanceCounterFacade.EntityTokenParentCacheMissIncrement();
                parentEntityTokens = null;
                return false;
            }
        }


        public static bool GetCachedHookingParents(EntityToken entityToken, out IEnumerable<EntityToken> parentEntityTokens)
        {
            if (!Enabled == false)
            {
                parentEntityTokens = null;
                return false;
            }

            string userName = UserValidationFacade.IsLoggedIn() ? ResolveUsername() : null;

            return GetCachedHookingParents(entityToken, out parentEntityTokens, userName);
        }


        internal static bool GetCachedHookingParents(EntityToken entityToken, out IEnumerable<EntityToken> parentEntityTokens, string userName)
        {
            if (!Enabled || userName == null)
            {
                parentEntityTokens = null;
                return false;
            }

            CacheKey cacheKey = new CacheKey { Username = userName, EntityToken = entityToken };

            CacheEntry cacheEntry;
            if (_hookingCache.TryGetValue(cacheKey, out cacheEntry) == true)
            {
                PerformanceCounterFacade.EntityTokenParentCacheHitIncrement();
                parentEntityTokens = cacheEntry.ParentEntityTokens;
                return true;
            }
            else
            {
                PerformanceCounterFacade.EntityTokenParentCacheMissIncrement();                
                parentEntityTokens = null;
                return false;
            }
        }        



        public static void ClearCache()
        {
            _nativeCache = new ConcurrentDictionary<CacheKey, CacheEntry>();
            _hookingCache = new ConcurrentDictionary<CacheKey, CacheEntry>();
        }



        private static string ResolveUsername()
        {
            return UserSettings.Username;
        }
     

        private sealed class CacheEntry
        {
            public CacheEntry(EntityToken entityToken)
            {
                this.EntityToken = entityToken;
            }


            public EntityToken EntityToken { get; private set; }
            public DateTime Timestamp { get; set; }
            public IEnumerable<EntityToken> ParentEntityTokens { get; set; }




            public override int GetHashCode()
            {
                return this.EntityToken.GetHashCode();
            }



            public override bool Equals(object obj)
            {
                bool result = Equals(obj as CacheEntry);

                return result;
            }


            public bool Equals(CacheEntry obj)
            {
                if (obj == null) return false;

                bool result = this.EntityToken.Equals(obj.EntityToken);

                return result;
            }


            public override string ToString()
            {
                return this.EntityToken.ToString();
            }
        }
    }
}

