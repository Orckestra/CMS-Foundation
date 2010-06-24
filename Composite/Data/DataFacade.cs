﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Transactions;
using Composite.Collections.Generic;
using Composite.Data.Foundation;
using Composite.EventSystem;
using Composite.Transactions;
using Composite.Types;
using Composite.Logging;


namespace Composite.Data
{
    public enum CascadeDeleteType
    {
        Allow, // Cascade delete are performed if the references allows it, if referees dont allow it and exception is thrown
        Disallow, // Cascade deletes are not performed and if referees exists an exception is thrown
        Disable // No check on existens of referees is done. This might result in foreign key violation
    }

    public sealed class DataMoveResult
    {
        internal DataMoveResult(IData movedData, IEnumerable<IData> movedRefereeDatas)
        {
            this.MovedData = movedData;
            this.MovedRefereeDatas = movedRefereeDatas;
        }

        public IData MovedData
        {
            get;
            private set;
        }

        public IEnumerable<IData> MovedRefereeDatas
        {
            get;
            private set;
        }
    }




    public static class DataFacade
    {
        private static ResourceLocker<Resources> _resourceLocker = new ResourceLocker<Resources>(new Resources(), Resources.DoInitializeResources);

        private static IDataFacade _dataFacade = new DataFacadeImpl();

        static DataFacade()
        {
            GlobalEventSystemFacade.SubscribeToFlushEvent(OnFlushEvent);
        }


        internal static IDataFacade Implementation { get { return _dataFacade; } set { _dataFacade = value; } }


        /// <summary>
        /// Gets an empty predicate (f => true)
        /// </summary>
        public static Expression<Func<T, bool>> GetEmptyPredicate<T>() where T : class
        {
            return EmptyPredicate<T>.Instance;
        }


        #region Data interception methods

        public static void SetDataInterceptor<T>(DataInterceptor dataInterceptor)
            where T : class, IData
        {
            _dataFacade.SetDataInterceptor<T>(dataInterceptor);
        }



        // Overload
        public static void SetDataInterceptor(Type interfaceType, DataInterceptor dataInterceptor)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");

            MethodInfo methodInfo = GetSetDataInterceptorMethodInfo(interfaceType);

            methodInfo.Invoke(null, new object[] { dataInterceptor });
        }



        public static bool HasDataInterceptor<T>()
            where T : class, IData
        {
            return _dataFacade.HasDataInterceptor<T>();
        }



        // Overload
        public static void HasDataInterceptor(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");

            MethodInfo methodInfo = GetHasDataInterceptorMethodInfo(interfaceType);

            methodInfo.Invoke(null, new object[] { });
        }



        public static void ClearDataInterceptor<T>()
            where T : class, IData
        {
            _dataFacade.ClearDataInterceptor<T>();
        }



        // Overload
        public static void ClearDataInterceptor(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");

            MethodInfo methodInfo = GetClearDataInterceptorMethodInfo(interfaceType);

            methodInfo.Invoke(null, new object[] { });
        }

        #endregion



        #region GetData methods

        public static IQueryable<T> GetData<T>(bool useCaching, IEnumerable<string> providerNames)
            where T : class, IData
        {
            return _dataFacade.GetData<T>(useCaching, providerNames);
        }

        public static IQueryable<T> GetData<T>(bool useCaching)
            where T : class, IData
        {
            return _dataFacade.GetData<T>(useCaching, null);
        }


        // Overload
        public static IQueryable<T> GetData<T>()
            where T : class, IData
        {
            return GetData<T>(true, null);
        }



        // Overload
        public static IQueryable<T> GetData<T>(Expression<Func<T, bool>> predicate)
            where T : class, IData
        {
            if (predicate == null) throw new ArgumentNullException("predicate");

            IQueryable<T> result = GetData<T>(true, null);

            if (object.Equals(predicate, EmptyPredicate<T>.Instance))
            {
                return result;
            }

            return result.Where(predicate);
        }



        // Overload
        public static IQueryable<T> GetData<T>(Expression<Func<T, bool>> predicate, bool useCaching)
            where T : class, IData
        {
            if (predicate == null) throw new ArgumentNullException("predicate");

            IQueryable<T> result = GetData<T>(useCaching, null);

            if (object.Equals(predicate, EmptyPredicate<T>.Instance))
            {
                return result;
            }

            return result.Where(predicate);
        }



        // Overload
        public static IQueryable GetData(Type interfaceType)
        {
            return GetData(interfaceType, true);
        }


        // Overload
        public static IQueryable GetData(Type interfaceType, bool useCaching)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");

            MethodInfo methodInfo = GetGetDataMethodInfo(interfaceType);

            return methodInfo.Invoke(null, new object[] { useCaching, null }) as IQueryable;
        }



        // Overload
        public static IQueryable GetData(Type interfaceType, string providerName)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (string.IsNullOrEmpty(providerName) == true) throw new ArgumentNullException("providerName");

            MethodInfo methodInfo = GetGetDataMethodInfo(interfaceType);

            return methodInfo.Invoke(null, new object[] { false, new string[] { providerName } }) as IQueryable;
        }

        #endregion



        #region GetDataFromDataSourceId methods

        public static T GetDataFromDataSourceId<T>(DataSourceId dataSourceId, bool useCaching)
            where T : class, IData
        {
            if (null == dataSourceId) throw new ArgumentNullException("dataSourceId");

            return _dataFacade.GetDataFromDataSourceId<T>(dataSourceId, useCaching);
        }


        // Overload
        public static T GetDataFromDataSourceId<T>(DataSourceId dataSourceId)
            where T : class, IData
        {
            if (null == dataSourceId) throw new ArgumentNullException("dataSourceId");

            return GetDataFromDataSourceId<T>(dataSourceId, true);
        }



        // Overload
        public static IData GetDataFromDataSourceId(DataSourceId dataSourceId)
        {
            if (null == dataSourceId) throw new ArgumentNullException("dataSourceId");

            MethodInfo methodInfo = GetGetDataFromDataSourceIdMethodInfo(dataSourceId.InterfaceType);

            IData data = (IData)methodInfo.Invoke(null, new object[] { dataSourceId, true });

            return data;
        }



        // Overload
        public static IData GetDataFromDataSourceId(DataSourceId dataSourceId, bool useCaching)
        {
            if (null == dataSourceId) throw new ArgumentNullException("dataSourceId");

            MethodInfo methodInfo = GetGetDataFromDataSourceIdMethodInfo(dataSourceId.InterfaceType);

            IData data = (IData)methodInfo.Invoke(null, new object[] { dataSourceId, useCaching });

            return data;
        }

        #endregion



        #region GetDataFromOtherScope methods (Only helpers)

        public static IQueryable<T> GetDataFromOtherScope<T>(T data, DataScopeIdentifier dataScopeIdentifier)
            where T : class, IData
        {
            if (data == null) throw new ArgumentNullException("data");
            if (dataScopeIdentifier == null) throw new ArgumentNullException("dataScopeIdentifier");

            if (GetSupportedDataScopes(data.DataSourceId.InterfaceType).Contains(dataScopeIdentifier) == false) throw new ArgumentException(string.Format("The data type '{0}' does not support the data scope '{1}'", data.DataSourceId.InterfaceType, dataScopeIdentifier));

            using (new DataScope(dataScopeIdentifier))
            {
                return DataExpressionBuilder.GetQueryableByData<T>(data, true);
            }
        }

        public static IQueryable<T> GetDataFromOtherLocale<T>(T data, CultureInfo cultureInfo)
            where T : class, IData
        {
            if (data == null) throw new ArgumentNullException("data");
            if (cultureInfo == null) throw new ArgumentNullException("cultureInfo");

            using (new DataScope(cultureInfo))
            {
                return DataExpressionBuilder.GetQueryableByData<T>(data, true);
            }
        }

        public static IEnumerable<IData> GetDataFromOtherScope(IData data, DataScopeIdentifier dataScopeIdentifier)
        {
            return GetDataFromOtherScope(data, dataScopeIdentifier, false);
        }

        public static IEnumerable<IData> GetDataFromOtherScope(IData data, DataScopeIdentifier dataScopeIdentifier, bool useCaching)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (dataScopeIdentifier == null) throw new ArgumentNullException("dataScopeIdentifier");

            if (GetSupportedDataScopes(data.DataSourceId.InterfaceType).Contains(dataScopeIdentifier) == false) throw new ArgumentException(string.Format("The data type '{0}' does not support the data scope '{1}'", data.DataSourceId.InterfaceType, dataScopeIdentifier));

            if (useCaching)
            {
                DataSourceId sourceId = data.DataSourceId;
                DataSourceId newDataSource = new DataSourceId(sourceId.DataId, sourceId.ProviderName,
                                                              sourceId.InterfaceType);

                newDataSource.DataScopeIdentifier = dataScopeIdentifier;

                IData fromDataSource = GetDataFromDataSourceId(newDataSource, true);
                if (fromDataSource == null)
                {
                    return new IData[0];
                }
                return new[] { fromDataSource };
            }

            var result = new List<IData>();

            using (new DataScope(dataScopeIdentifier))
            {
                IQueryable table = GetData(data.DataSourceId.InterfaceType, false);

                IQueryable queryable = DataExpressionBuilder.GetQueryableByData(data, table, true);

                foreach (object obj in queryable)
                {
                    result.Add((IData)obj);
                }
            }

            return result;
        }

        #endregion



        #region GetPredicateExpressionByUniqueKey methods (Only helpers)

        public static Expression<Func<T, bool>> GetPredicateExpressionByUniqueKey<T>(DataKeyPropertyCollection dataKeyPropertyCollection)
            where T : class, IData
        {
            if (dataKeyPropertyCollection == null) throw new ArgumentNullException("dataKeyPropertyCollection");

            List<PropertyInfo> keyPropertyInfos = DataAttributeFacade.GetKeyPropertyInfoes(typeof(T));

            ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "data");

            Expression currentExpression = GetPredicateExpressionByUniqueKeyFilterExpression(keyPropertyInfos, dataKeyPropertyCollection, parameterExpression);

            Expression<Func<T, bool>> lambdaExpression = Expression.Lambda<Func<T, bool>>(currentExpression, new ParameterExpression[] { parameterExpression });

            return lambdaExpression;
        }



        // Overload
        public static Expression<Func<T, bool>> GetPredicateExpressionByUniqueKey<T>(object dataKeyValue)
            where T : class, IData
        {
            PropertyInfo propertyInfo = DataAttributeFacade.GetKeyPropertyInfoes(typeof(T)).Single();

            DataKeyPropertyCollection dataKeyPropertyCollection = new DataKeyPropertyCollection();
            dataKeyPropertyCollection.AddKeyProperty(propertyInfo, dataKeyValue);

            return GetPredicateExpressionByUniqueKey<T>(dataKeyPropertyCollection);
        }



        public static LambdaExpression GetPredicateExpressionByUniqueKey(Type interfaceType, DataKeyPropertyCollection dataKeyPropertyCollection)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (dataKeyPropertyCollection == null) throw new ArgumentNullException("dataKeyPropertyCollection");

            List<PropertyInfo> keyPropertyInfos = DataAttributeFacade.GetKeyPropertyInfoes(interfaceType);

            ParameterExpression parameterExpression = Expression.Parameter(interfaceType, "data");

            Expression currentExpression = GetPredicateExpressionByUniqueKeyFilterExpression(keyPropertyInfos, dataKeyPropertyCollection, parameterExpression);

            Type delegateType = typeof(Func<,>).MakeGenericType(new Type[] { interfaceType, typeof(bool) });

            LambdaExpression lambdaExpression = Expression.Lambda(delegateType, currentExpression, new ParameterExpression[] { parameterExpression });

            return lambdaExpression;
        }



        // Overload
        public static LambdaExpression GetPredicateExpressionByUniqueKey(Type interfaceType, object dataKeyValue)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");

            PropertyInfo propertyInfo = DataAttributeFacade.GetKeyPropertyInfoes(interfaceType).Single();

            DataKeyPropertyCollection dataKeyPropertyCollection = new DataKeyPropertyCollection();
            dataKeyPropertyCollection.AddKeyProperty(propertyInfo, dataKeyValue);

            return GetPredicateExpressionByUniqueKey(interfaceType, dataKeyPropertyCollection);
        }



        // Private helper
        private static Expression GetPredicateExpressionByUniqueKeyFilterExpression(List<PropertyInfo> keyPropertyInfos, DataKeyPropertyCollection dataKeyPropertyCollection, ParameterExpression parameterExpression)
        {
            if (keyPropertyInfos.Count != dataKeyPropertyCollection.Count) throw new ArgumentException("Missing og to many key propertyies");

            Expression currentExpression = null;
            foreach (var kvp in dataKeyPropertyCollection.KeyProperties)
            {
                PropertyInfo keyPropertyInfo = keyPropertyInfos.Where(f => f.Name == kvp.Key).Single();

                Expression left = LambdaExpression.Property(parameterExpression, keyPropertyInfo);
                object castedDataKey = ValueTypeConverter.Convert(kvp.Value, keyPropertyInfo.PropertyType);
                Expression right = Expression.Constant(castedDataKey);

                Expression filter = Expression.Equal(left, right);

                if (currentExpression == null)
                {
                    currentExpression = filter;
                }
                else
                {
                    currentExpression = Expression.And(currentExpression, filter);
                }
            }

            return currentExpression;
        }

        #endregion



        #region GetDataByUniqueKey methods (Only helpers)

        // Overload
        public static T TryGetDataByUniqueKey<T>(object dataKeyValue)
            where T : class, IData
        {
            PropertyInfo propertyInfo = DataAttributeFacade.GetKeyPropertyInfoes(typeof(T)).Single();

            DataKeyPropertyCollection dataKeyPropertyCollection = new DataKeyPropertyCollection();
            dataKeyPropertyCollection.AddKeyProperty(propertyInfo, dataKeyValue);

            return TryGetDataByUniqueKey<T>(dataKeyPropertyCollection);
        }



        public static T TryGetDataByUniqueKey<T>(DataKeyPropertyCollection dataKeyPropertyCollection)
            where T : class, IData
        {
            if (dataKeyPropertyCollection == null) throw new ArgumentNullException("dataKeyPropertyCollection");

            Expression<Func<T, bool>> lambdaExpression = GetPredicateExpressionByUniqueKey<T>(dataKeyPropertyCollection);

            return GetData<T>(lambdaExpression).SingleOrDefault();
        }



        // Overload
        public static T GetDataByUniqueKey<T>(object dataKeyValue)
            where T : class, IData
        {
            IData data = TryGetDataByUniqueKey<T>(dataKeyValue);

            if (data == null) throw new InvalidOperationException("No data exist given the data key values");

            return (T)data;
        }


        // Overload
        public static IData TryGetDataByUniqueKey(Type interfaceType, object dataKeyValue)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");

            PropertyInfo propertyInfo = DataAttributeFacade.GetKeyPropertyInfoes(interfaceType).Single();

            DataKeyPropertyCollection dataKeyPropertyCollection = new DataKeyPropertyCollection();
            dataKeyPropertyCollection.AddKeyProperty(propertyInfo, dataKeyValue);

            return TryGetDataByUniqueKey(interfaceType, dataKeyPropertyCollection);
        }



        public static IData TryGetDataByUniqueKey(Type interfaceType, DataKeyPropertyCollection dataKeyPropertyCollection)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (dataKeyPropertyCollection == null) throw new ArgumentNullException("dataKeyPropertyCollection");

            LambdaExpression lambdaExpression = GetPredicateExpressionByUniqueKey(interfaceType, dataKeyPropertyCollection);

            MethodInfo methodInfo = GetGetDataWithPredicatMethodInfo(interfaceType);

            IQueryable queryable = (IQueryable)methodInfo.Invoke(null, new object[] { lambdaExpression });

            IData data = queryable.OfType<IData>().SingleOrDefault();

            return data;
        }



        // Overload
        public static IData GetDataByUniqueKey(Type interfaceType, object dataKeyValue)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");

            PropertyInfo propertyInfo = DataAttributeFacade.GetKeyPropertyInfoes(interfaceType).Single();

            DataKeyPropertyCollection dataKeyPropertyCollection = new DataKeyPropertyCollection();
            dataKeyPropertyCollection.AddKeyProperty(propertyInfo, dataKeyValue);

            IData data = TryGetDataByUniqueKey(interfaceType, dataKeyPropertyCollection);

            if (data == null) throw new InvalidOperationException("No data exist given the data key values");

            return data;
        }



        // Overload
        public static IData GetDataByUniqueKey(Type interfaceType, DataKeyPropertyCollection dataKeyPropertyCollection)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (dataKeyPropertyCollection == null) throw new ArgumentNullException("dataKeyPropertyCollection");

            IData data = TryGetDataByUniqueKey(interfaceType, dataKeyPropertyCollection);

            if (data == null) throw new InvalidOperationException("No data exist given the data key values");

            return data;
        }
        #endregion



        #region GetDataOrderedBy methods (Only helpers)

        // Overload
        public static IEnumerable<IData> GetDataOrderedBy(Type interfaceType, PropertyInfo propertyInfo)
        {
            return GetDataOrderedByQueryable(interfaceType, propertyInfo).ToDataEnumerable();
        }



        public static IQueryable GetDataOrderedByQueryable(Type interfaceType, PropertyInfo propertyInfo)
        {
            IQueryable source = DataFacade.GetData(interfaceType);

            ParameterExpression parameter = Expression.Parameter(interfaceType, "f");
            LambdaExpression lambdaExpression = Expression.Lambda(Expression.Property(parameter, propertyInfo), parameter);

            MethodCallExpression methodCallExpression = Expression.Call
            (
                typeof(Queryable),
                "OrderBy",
                new Type[] { interfaceType, propertyInfo.PropertyType },
                source.Expression,
                Expression.Quote(lambdaExpression)
            );

            return source.Provider.CreateQuery(methodCallExpression);
        }

        #endregion



        #region GetPredicateExpression methods (Only helpers)

        public static LambdaExpression GetPredicateExpression(Type interfaceType, DataPropertyValueCollection dataPropertyValueCollection)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (dataPropertyValueCollection == null) throw new ArgumentNullException("dataPropertyValueCollection");

            ParameterExpression parameterExpression = Expression.Parameter(interfaceType, "data");

            Expression currentExpression = GetPredicateExpressionFilterExpression(dataPropertyValueCollection, parameterExpression);

            Type delegateType = typeof(Func<,>).MakeGenericType(new Type[] { interfaceType, typeof(bool) });

            LambdaExpression lambdaExpression = Expression.Lambda(delegateType, currentExpression, new ParameterExpression[] { parameterExpression });

            return lambdaExpression;
        }


        // Private helper
        private static Expression GetPredicateExpressionFilterExpression(DataPropertyValueCollection dataPropertyValueCollection, ParameterExpression parameterExpression)
        {
            Expression currentExpression = null;
            foreach (var kvp in dataPropertyValueCollection.PropertyValues)
            {
                Expression left = LambdaExpression.Property(parameterExpression, kvp.Key);
                object castedValue = ValueTypeConverter.Convert(kvp.Value, kvp.Key.PropertyType);
                Expression right = Expression.Constant(castedValue);

                Expression filter = Expression.Equal(left, right);

                if (currentExpression == null)
                {
                    currentExpression = filter;
                }
                else
                {
                    currentExpression = Expression.And(currentExpression, filter);
                }
            }

            return currentExpression;
        }

        #endregion



        #region WillUpdateSucceed methods (Only helpers)

        public static bool WillUpdateSucceed(IData data)
        {
            if (data == null) throw new ArgumentNullException("data");

            return data.TryValidateForeignKeyIntegrity();
        }



        public static bool WillUpdateSucceed(IEnumerable<IData> datas)
        {
            if (null == datas) throw new ArgumentNullException("datas");

            foreach (IData data in datas)
            {
                if (data == null) throw new ArgumentException("datas may not contain nulls");

                if (data.TryValidateForeignKeyIntegrity() == false) return false;
            }

            return true;
        }

        #endregion



        #region Update methods

        // Overload
        public static void Update(IData data)
        {
            if (null == data) throw new ArgumentNullException("data");

            Update(new[] { data });
        }


        public static void Update(IData data, bool suppressEventing, bool performForeignKeyIntegrityCheck, bool performeValidation)
        {
            if (null == data) throw new ArgumentNullException("data");

            Update(new[] { data }, suppressEventing, performForeignKeyIntegrityCheck, performeValidation);
        }



        public static void Update(IEnumerable<IData> dataset)
        {
            Verify.ArgumentNotNull(dataset, "dataset");

            _dataFacade.Update(dataset, false, true, true);
        }


        public static void Update(IEnumerable<IData> dataset, bool suppressEventing, bool performForeignKeyIntegrityCheck)
        {
            Verify.ArgumentNotNull(dataset, "dataset");

            _dataFacade.Update(dataset, suppressEventing, performForeignKeyIntegrityCheck, true);
        }



        public static void Update(IEnumerable<IData> dataset, bool suppressEventing, bool performForeignKeyIntegrityCheck, bool performeValidation)
        {
            Verify.ArgumentNotNull(dataset, "dataset");

            _dataFacade.Update(dataset, suppressEventing, performForeignKeyIntegrityCheck, performeValidation);
        }

        #endregion



        #region BuildNew methods (Only helpers)

        // Overload
        public static T BuildNew<T>()
            where T : class, IData
        {
            return BuildNew<T>(false);
        }




        public static T BuildNew<T>(bool suppressEventing)
            where T : class, IData
        {
            Type generatedType = BuildNewTypeCache.GetTypeToBuild(typeof(T));

            IData data = (IData)Activator.CreateInstance(generatedType, new object[] { });

            SetNewInstancaFieldDefaultValues(data);

            if (suppressEventing == false)
            {
                DataEventSystemFacade.FireDataAfterBuildNewEvent<T>(data);
            }

            return (T)data;
        }



        // Overload
        public static IData BuildNew(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");

            return BuildNew(interfaceType, false);
        }



        public static IData BuildNew(Type interfaceType, bool suppressEventling)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");

            Type generatedType = BuildNewTypeCache.GetTypeToBuild(interfaceType);

            IData data = (IData)Activator.CreateInstance(generatedType, new object[] { });

            SetNewInstancaFieldDefaultValues(data);

            if (suppressEventling == false)
            {
                DataEventSystemFacade.FireDataAfterBuildNewEvent(generatedType, data);
            }

            return data;
        }



        private static void SetNewInstancaFieldDefaultValues(IData data)
        {
            Type interfaceType = data.DataSourceId.InterfaceType;
            List<PropertyInfo> properties = interfaceType.GetPropertiesRecursively();
            foreach (PropertyInfo propertyInfo in properties)
            {
                try
                {
                    NewInstanceDefaultFieldValueAttribute attribute = propertyInfo.GetCustomAttributesRecursively<NewInstanceDefaultFieldValueAttribute>().SingleOrDefault();
                    if (attribute == null || attribute.HasValue == false) continue;
                    if (propertyInfo.CanWrite == false)
                    {
                        LoggingService.LogError("DataFacade", string.Format("The property '{0}' on the interface '{1}' has defined a standard value, but no setter", propertyInfo.Name, interfaceType));
                        continue;
                    }

                    object value = attribute.GetValue();
                    value = ValueTypeConverter.Convert(value, propertyInfo.PropertyType);

                    PropertyInfo targetPropertyInfo = data.GetType().GetProperties().Where(f => f.Name == propertyInfo.Name).Single();
                    targetPropertyInfo.SetValue(data, value, null);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError("DataFacade", string.Format("Failed to set the standard value on the property '{0}' on the interface '{1}'", propertyInfo.Name, interfaceType));
                    LoggingService.LogError("DataFacade", ex);
                }
            }
        }

        #endregion



        #region WillAddNewSucceed (Only helpers)

        public static bool WillAddNewSucceed(IData data)
        {
            if (data == null) throw new ArgumentNullException("data");

            return data.TryValidateForeignKeyIntegrity();
        }



        public static bool WillAddNewSucceed<T>(T data)
            where T : class, IData
        {
            if (data == null) throw new ArgumentNullException("data");

            return data.TryValidateForeignKeyIntegrity();
        }



        public static bool WillAddNewSucceed<T>(IEnumerable<T> datas)
            where T : class, IData
        {
            if (datas == null) throw new ArgumentNullException("datas");

            foreach (T data in datas)
            {
                if (data == null) throw new ArgumentException("datas may not contain nulls");

                if (data.TryValidateForeignKeyIntegrity() == false) return false;
            }

            return true;
        }

        #endregion



        #region AddNew methods

        // Overload
        public static T AddNew<T>(T data)
            where T : class, IData
        {
            if (data == null) throw new ArgumentNullException("data");

            List<T> result = AddNew<T>(new T[] { data }, true, false, true, true, null);

            return result[0];
        }



        // Overload
        public static T AddNew<T>(T data, string providerName)
            where T : class, IData
        {
            if (data == null) throw new ArgumentNullException("data");
            if (string.IsNullOrEmpty(providerName) == true) throw new ArgumentNullException("providerName");

            List<T> result = AddNew<T>(new T[] { data }, true, false, true, true, new List<string> { providerName });

            return result[0];
        }



        // Overload
        public static List<T> AddNew<T>(IEnumerable<T> datas)
            where T : class, IData
        {
            if (datas == null) throw new ArgumentNullException("datas");

            return AddNew<T>(datas, true, false, true, true, null);
        }



        // Overload
        public static List<T> AddNew<T>(IEnumerable<T> datas, string providerName)
            where T : class, IData
        {
            if (datas == null) throw new ArgumentNullException("datas");
            if (string.IsNullOrEmpty(providerName) == true) throw new ArgumentNullException("providerName");

            return AddNew<T>(datas, true, false, true, true, new List<string> { providerName });
        }



        // Overload
        /// <summary>
        /// WARNING: Setting <paramref name="performForeignKeyIntegrityCheck"/> to 'false' can 
        /// cause serious foreign key corruption.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="performForeignKeyIntegrityCheck"></param>
        /// <returns></returns>
        public static T AddNew<T>(T data, bool performForeignKeyIntegrityCheck)
            where T : class, IData
        {
            if (data == null) throw new ArgumentNullException("data");

            List<T> result = AddNew<T>(new T[] { data }, true, false, performForeignKeyIntegrityCheck, true, null);

            return result[0];
        }



        // Overload
        /// <summary>
        /// WARNING: Setting <paramref name="performForeignKeyIntegrityCheck"/> to 'false' can 
        /// cause serious foreign key corruption.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="performForeignKeyIntegrityCheck"></param>
        /// <returns></returns>
        public static T AddNew<T>(T data, bool suppressEventing, bool performForeignKeyIntegrityCheck)
            where T : class, IData
        {
            if (data == null) throw new ArgumentNullException("data");

            List<T> result = AddNew<T>(new T[] { data }, true, suppressEventing, performForeignKeyIntegrityCheck, true, null);

            return result[0];
        }



        // Overload
        /// <summary>
        /// WARNING: Setting <paramref name="performForeignKeyIntegrityCheck"/> to 'false' can 
        /// cause serious foreign key corruption.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="performForeignKeyIntegrityCheck"></param>
        /// <returns></returns>
        public static T AddNew<T>(T data, bool suppressEventing, bool performForeignKeyIntegrityCheck, bool performeValidation)
            where T : class, IData
        {
            if (data == null) throw new ArgumentNullException("data");

            List<T> result = AddNew<T>(new T[] { data }, true, suppressEventing, performForeignKeyIntegrityCheck, performeValidation, null);

            return result[0];
        }



        // Overload
        public static IData AddNew(IData data)
        {
            if (data == null) throw new ArgumentNullException("data");

            MethodInfo methodInfo = GetAddNewMethodInfo(data.DataSourceId.InterfaceType);

            IData resultData = (IData)methodInfo.Invoke(null, new object[] { data, true, false, true, true, null });

            return resultData;
        }



        /// <summary>
        /// WARNING: Setting <paramref name="performForeignKeyIntegrityCheck"/> to 'false' can cause serious foreign key corruption.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="suppressEventing"></param>
        /// <param name="performForeignKeyIntegrityCheck"></param>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public static IData AddNew(IData data, bool suppressEventing, bool performForeignKeyIntegrityCheck, string providerName)
        {
            if (data == null) throw new ArgumentNullException("data");

            MethodInfo methodInfo = GetAddNewMethodInfo(data.DataSourceId.InterfaceType);

            IData resultData = (IData)methodInfo.Invoke(null, new object[] { data, true, suppressEventing, performForeignKeyIntegrityCheck, true, new List<string> { providerName } });

            return resultData;
        }



        /// <summary>
        /// WARNING: Setting <paramref name="performForeignKeyIntegrityCheck"/> to 'false' can cause serious foreign key corruption.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="suppressEventing"></param>
        /// <param name="performForeignKeyIntegrityCheck"></param>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public static IData AddNew(IData data, Type interfaceType, bool suppressEventing, bool performForeignKeyIntegrityCheck, string providerName)
        {
            if (data == null) throw new ArgumentNullException("data");

            MethodInfo methodInfo = GetAddNewMethodInfo(interfaceType);

            IData resultData = (IData)methodInfo.Invoke(null, new object[] { data, true, suppressEventing, performForeignKeyIntegrityCheck, true, new List<string> { providerName } });

            return resultData;
        }



        /// <summary>
        /// WARNING: Setting <paramref name="performForeignKeyIntegrityCheck"/> to 'false' can cause serious foreign key corruption.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="suppressEventing"></param>
        /// <param name="performForeignKeyIntegrityCheck"></param>        
        /// <returns></returns>
        public static IData AddNew(IData data, bool suppressEventing, bool performForeignKeyIntegrityCheck, bool performeValidation)
        {
            if (data == null) throw new ArgumentNullException("data");

            MethodInfo methodInfo = GetAddNewMethodInfo(data.DataSourceId.InterfaceType);

            IData resultData = (IData)methodInfo.Invoke(null, new object[] { data, true, suppressEventing, performForeignKeyIntegrityCheck, performeValidation, null });

            return resultData;
        }




        /// <summary>
        /// WARNING: Setting <paramref name="performForeignKeyIntegrityCheck"/> to 'false' can cause serious foreign key corruption.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="suppressEventing"></param>
        /// <param name="performForeignKeyIntegrityCheck"></param>        
        /// <returns></returns>
        public static IData AddNew(IData data, Type interfaceType, bool suppressEventing, bool performForeignKeyIntegrityCheck, bool performeValidation)
        {
            if (data == null) throw new ArgumentNullException("data");

            MethodInfo methodInfo = GetAddNewMethodInfo(interfaceType);

            IData resultData = (IData)methodInfo.Invoke(null, new object[] { data, true, suppressEventing, performForeignKeyIntegrityCheck, performeValidation, null });

            return resultData;
        }



        /// <summary>
        /// WARNING: Setting <paramref name="performForeignKeyIntegrityCheck"/> to 'false' can cause serious foreign key corruption.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="suppressEventing"></param>
        /// <param name="performForeignKeyIntegrityCheck"></param>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public static IData AddNew(IData data, bool suppressEventing, bool performForeignKeyIntegrityCheck, bool performeValidation, string providerName)
        {
            if (data == null) throw new ArgumentNullException("data");

            MethodInfo methodInfo = GetAddNewMethodInfo(data.DataSourceId.InterfaceType);

            IData resultData = (IData)methodInfo.Invoke(null, new object[] { data, true, suppressEventing, performForeignKeyIntegrityCheck, performeValidation, new List<string> { providerName } });

            return resultData;
        }




        // Overload
        /// <summary>
        /// WARNING: Setting <paramref name="performForeignKeyIntegrityCheck"/> to 'false' can cause serious foreign key corruption.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="performForeignKeyIntegrityCheck"></param>
        /// <returns></returns>
        public static IData AddNew(IData data, bool performForeignKeyIntegrityCheck)
        {
            if (data == null) throw new ArgumentNullException("data");

            MethodInfo methodInfo = GetAddNewMethodInfo(data.DataSourceId.InterfaceType);

            IData resultData = (IData)methodInfo.Invoke(null, new object[] { data, true, false, performForeignKeyIntegrityCheck, true, null });

            return resultData;
        }



        private static T AddNew<T>(T collection, bool allowStoreCreation, bool suppressEventing, bool performForeignKeyIntegrityCheck, bool performeValidation, List<string> writeableProviders)
            where T : class, IData
        {
            List<T> result = _dataFacade.AddNew<T>(new T[] { collection }, allowStoreCreation, suppressEventing, performForeignKeyIntegrityCheck, performeValidation, writeableProviders);

            return result[0];
        }



        private static List<T> AddNew<T>(IEnumerable<T> collection, bool allowStoreCreation, bool suppressEventing, bool performForeignKeyIntegrityCheck, bool performeValidation, List<string> writeableProviders)
            where T : class, IData
        {
            return _dataFacade.AddNew<T>(collection, allowStoreCreation, suppressEventing, performForeignKeyIntegrityCheck, performeValidation, writeableProviders);
        }

        #endregion



        #region WillDeleteSucceed methods (Only helpers)

        public static bool WillDeleteSucceed<T>(T data)
            where T : class, IData
        {
            if (data == null) throw new ArgumentNullException("data");

            return data.TryValidateDeleteSucces();
        }



        public static bool WillDeleteSucceed(IEnumerable<IData> datas)
        {
            if (datas == null) throw new ArgumentNullException("datas");

            return WillDeleteSucceed<IData>(datas);
        }



        public static bool WillDeleteSucceed(IData data)
        {
            if (data == null) throw new ArgumentNullException("data");

            return data.TryValidateDeleteSucces();
        }



        public static bool WillDeleteSucceed<T>(IEnumerable<T> datas)
            where T : class, IData
        {
            if (datas == null) throw new ArgumentNullException("datas");

            foreach (T data in datas)
            {
                if (data == null) throw new ArgumentException("The datas may not contain nulls");

                if (data.TryValidateDeleteSucces() == false)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion



        #region Delete methods

        // Overload
        public static void Delete<T>(T data)
            where T : class, IData
        {
            if (data == null) throw new ArgumentNullException("data");

            Delete<T>(new T[] { data }, false, CascadeDeleteType.Allow, true);
        }



        // Overload
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="referencesFromAllScopes">
        /// If this is true then cascade delete is performed on all data scopes.
        /// If this is false then cascade delete is only performed in the same scope as <paramref name="data"/>
        /// </param>
        public static void Delete<T>(T data, bool referencesFromAllScopes)
            where T : class, IData
        {
            if (data == null) throw new ArgumentNullException("data");

            Delete<T>(new T[] { data }, false, CascadeDeleteType.Allow, referencesFromAllScopes);
        }



        // Overload
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="referencesFromAllScopes">
        /// If this is true then cascade delete is performed on all data scopes.
        /// If this is false then cascade delete is only performed in the same scope as <paramref name="data"/>
        /// </param>
        public static void Delete<T>(IEnumerable<T> datas, bool suppressEventing, bool referencesFromAllScopes)
            where T : class, IData
        {
            if (datas == null) throw new ArgumentNullException("datas");

            Delete<T>(datas, suppressEventing, CascadeDeleteType.Allow, referencesFromAllScopes);
        }



        // Overload
        public static void Delete<T>(IEnumerable<T> datas)
            where T : class, IData
        {
            if (datas == null) throw new ArgumentNullException("datas");

            Delete<T>(datas, false, CascadeDeleteType.Allow, true);
        }



        // Overload
        /// <summary>
        /// Deletes the given datas. WARNING: Setting <paramref name="cascadeDeleteType"/> 
        /// to 'Disable' can cause serious foreign key corruption.
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="cascadeDeleteType"></param>
        public static void Delete<T>(IEnumerable<T> datas, bool suppressEventing, CascadeDeleteType cascadeDeleteType)
            where T : class, IData
        {
            if (datas == null) throw new ArgumentNullException("datas");

            Delete<T>(datas, suppressEventing, cascadeDeleteType, true);
        }



        // Overload
        /// <summary>
        /// Deletes the given datas. WARNING: Setting <paramref name="cascadeDeleteType"/> 
        /// to 'Disable' can cause serious foreign key corruption.
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="cascadeDeleteType"></param>
        public static void Delete<T>(IEnumerable<T> datas, CascadeDeleteType cascadeDeleteType)
            where T : class, IData
        {
            if (datas == null) throw new ArgumentNullException("datas");

            Delete<T>(datas, false, cascadeDeleteType, true);
        }



        // Overload
        public static void Delete<T>(Expression<Func<T, bool>> predicate)
            where T : class, IData
        {
            using (TransactionScope transactionScope = TransactionsFacade.CreateNewScope())
            {
                IEnumerable<T> datasToDelete = DataFacade.GetData<T>(predicate, false);

                Delete<T>(datasToDelete, false, CascadeDeleteType.Allow, true);

                transactionScope.Complete();
            }
        }



        // Overload
        public static void Delete(IEnumerable<IData> datas)
        {
            if (datas == null) throw new ArgumentNullException("datas");

            Delete<IData>(datas, false, CascadeDeleteType.Allow, true);
        }



        // Overload
        /// <summary>
        /// Deletes the given datas. WARNING: Setting <paramref name="cascadeDeleteType"/> 
        /// to 'Disable' can cause serious foreign key corruption.
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="cascadeDeleteType"></param>
        public static void Delete(IData data, CascadeDeleteType cascadeDeleteType)
        {
            if (data == null) throw new ArgumentNullException("data");

            Delete<IData>(new IData[] { data }, false, cascadeDeleteType, true);
        }



        // Overload
        /// <summary>
        /// Deletes the given datas. WARNING: Setting <paramref name="cascadeDeleteType"/> 
        /// to 'Disable' can cause serious foreign key corruption.
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="cascadeDeleteType"></param>
        public static void Delete(IEnumerable<IData> datas, bool suppressEventing, CascadeDeleteType cascadeDeleteType)
        {
            if (datas == null) throw new ArgumentNullException("datas");

            Delete<IData>(datas, suppressEventing, cascadeDeleteType, true);
        }



        // Overload
        /// <summary>
        /// Deletes the given datas. WARNING: Setting <paramref name="cascadeDeleteType"/> 
        /// to 'Disable' can cause serious foreign key corruption.
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="cascadeDeleteType"></param>
        public static void Delete(IEnumerable<IData> datas, CascadeDeleteType cascadeDeleteType)
        {
            if (datas == null) throw new ArgumentNullException("datas");

            Delete<IData>(datas, false, cascadeDeleteType, true);
        }



        // Overload
        /// <summary>
        /// Deletes the given datas. WARNING: Setting <paramref name="cascadeDeleteType"/> 
        /// to 'Disable' can cause serious foreign key corruption.
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="cascadeDeleteType"></param>
        public static void Delete<T>(T data, bool suppressEventing, CascadeDeleteType cascadeDeleteType)
          where T : class, IData
        {
            if (null == data) throw new ArgumentNullException("data");

            Delete<T>(new T[] { data }, suppressEventing, cascadeDeleteType, true);
        }



        private static void Delete<T>(IEnumerable<T> datas, bool suppressEventing, CascadeDeleteType cascadeDeleteType, bool referencesFromAllScopes)
            where T : class, IData
        {
            _dataFacade.Delete<T>(datas, suppressEventing, cascadeDeleteType, referencesFromAllScopes);
        }

        #endregion



        #region Move methods

        // Overlaod
        public static DataMoveResult Move<T>(T data, DataScopeIdentifier targetDataScopeIdentifier)
            where T : class, IData
        {
            if (data == null) throw new ArgumentNullException("data");
            if (targetDataScopeIdentifier == null) throw new ArgumentNullException("targetDataScopeIdentifier");

            return Move<T>(data, targetDataScopeIdentifier, true);
        }



        // Overload
        public static DataMoveResult Move(IData data, DataScopeIdentifier targetDataScopeIdentifier)
        {
            MethodInfo methodInfo = GetMoveMethodInfo(data.DataSourceId.InterfaceType);

            DataMoveResult dataMoveResult = (DataMoveResult)methodInfo.Invoke(null, new object[] { data, targetDataScopeIdentifier, true });

            return dataMoveResult;
        }



        private static DataMoveResult Move<T>(T data, DataScopeIdentifier targetDataScopeIdentifier, bool allowCascadeMove)
            where T : class, IData
        {
            if (data == null) throw new ArgumentNullException("data");
            if (targetDataScopeIdentifier == null) throw new ArgumentNullException("targetDataScopeIdentifier");

            return _dataFacade.Move<T>(data, targetDataScopeIdentifier, allowCascadeMove);
        }

        #endregion



        #region GetDataProviderNames method (Only helpers)

        public static IEnumerable<string> GetDataProviderNames()
        {
            return DataProviderRegistry.DataProviderNames;
        }



        public static IEnumerable<string> GetDynamicDataProviderNames()
        {
            return DataProviderRegistry.DynamicDataProviderNames;
        }

        #endregion



        #region GetInterfaces methods (Only helpers)

        public static List<Type> GetAllInterfaces()
        {
            return DataProviderRegistry.AllInterfaces.ToList();
        }



        public static List<Type> GetAllInterfaces(UserType relevantToUserType)
        {
            return (from dataInterface in DataProviderRegistry.AllInterfaces
                    where dataInterface.GetCustomInterfaceAttributes<RelevantToUserTypeAttribute>().Any(a => (a.UserType & relevantToUserType) == relevantToUserType)
                    select dataInterface).ToList();
        }



        public static List<Type> GetAllKnownInterfaces()
        {
            return DataProviderRegistry.AllKnownInterfaces.ToList();
        }



        public static List<Type> GetAllKnownInterfaces(UserType relevantToUserType)
        {
            return (from dataInterface in DataProviderRegistry.AllKnownInterfaces
                    where dataInterface.GetCustomInterfaceAttributes<RelevantToUserTypeAttribute>().Any(a => (a.UserType & relevantToUserType) == relevantToUserType)
                    select dataInterface).ToList();
        }



        public static List<Type> GetGeneratedInterfaces()
        {
            return DataProviderRegistry.GeneratedInterfaces.ToList();
        }

        #endregion



        #region DataTag methods (Only helpers)

        // Overload
        public static void SetDataTag(IData data, string id, object value)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (string.IsNullOrEmpty(id) == true) throw new ArgumentNullException("id");

            SetDataTag(data.DataSourceId, id, value);
        }



        public static void SetDataTag(DataSourceId dataSourceId, string id, object value)
        {
            if (dataSourceId == null) throw new ArgumentNullException("dataSourceId");
            if (string.IsNullOrEmpty(id) == true) throw new ArgumentNullException("id");

            dataSourceId.SetTag(id, value);
        }



        // Overload
        public static bool TryGetDataTag<T>(IData data, string id, out T tag)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (string.IsNullOrEmpty(id) == true) throw new ArgumentNullException("id");

            return TryGetDataTag<T>(data.DataSourceId, id, out tag);
        }



        public static bool TryGetDataTag<T>(DataSourceId dataSourceId, string id, out T tag)
        {
            if (dataSourceId == null) throw new ArgumentNullException("dataSourceId");
            if (string.IsNullOrEmpty(id) == true) throw new ArgumentNullException("id");

            object tagValue = null;
            bool result = dataSourceId.TryGetTag(id, out tagValue);

            if (result == true)
            {
                tag = (T)tagValue;
            }
            else
            {
                tag = default(T);
            }

            return result;
        }



        // Overload
        public static T GetDataTag<T>(IData data, string id)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (string.IsNullOrEmpty(id) == true) throw new ArgumentNullException("id");

            return GetDataTag<T>(data.DataSourceId, id);
        }



        public static T GetDataTag<T>(DataSourceId dataSourceId, string id)
        {
            if (dataSourceId == null) throw new ArgumentNullException("dataSourceId");
            if (string.IsNullOrEmpty(id) == true) throw new ArgumentNullException("id");

            object value;
            if (dataSourceId.TryGetTag(id, out value) == false)
            {
                throw new InvalidOperationException(string.Format("The tag '{0}' has not been set on the data source id", id));
            }

            return (T)value;
        }



        // Overload
        public static void RemoveDataTag(IData data, string id)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (string.IsNullOrEmpty(id) == true) throw new ArgumentNullException("id");

            RemoveDataTag(data.DataSourceId, id);
        }



        public static void RemoveDataTag(DataSourceId dataSourceId, string id)
        {
            if (dataSourceId == null) throw new ArgumentNullException("dataSourceId");
            if (string.IsNullOrEmpty(id) == true) throw new ArgumentNullException("id");

            dataSourceId.RemoveTag(id);
        }

        #endregion



        #region Mics methods (Only helpers)

        public static IEnumerable<DataScopeIdentifier> GetSupportedDataScopes(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");

            IEnumerable<DataScopeIdentifier> supportedDataScope = interfaceType.GetSupportedDataScopes();

            if (!supportedDataScope.Any())
            {
                throw new InvalidOperationException(string.Format("The data type '{0}' does not support any data scopes, use the '{1}' attribute", interfaceType, typeof(DataScopeAttribute)));
            }

            return supportedDataScope;
        }



        public static bool HasDataInAnyScope(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");

            foreach (DataScopeIdentifier dataScopeIdentifier in GetSupportedDataScopes(interfaceType))
            {
                using (DataScope dataScope = new DataScope(dataScopeIdentifier))
                {
                    IData data = GetData(interfaceType).ToDataEnumerable().FirstOrDefault();

                    if (data != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }



        public static bool ExistsInAnyLocale<T>(IEnumerable<CultureInfo> excludedCultureInfoes)
            where T : class, IData
        {
            return _dataFacade.ExistsInAnyLocale<T>(excludedCultureInfoes);
        }



        // Overload
        public static bool ExistsInAnyLocale<T>(CultureInfo excludedCultureInfo)
            where T : class, IData
        {
            return ExistsInAnyLocale<T>(new CultureInfo[] { excludedCultureInfo });
        }



        // Overload
        public static bool ExistsInAnyLocale<T>()
            where T : class, IData
        {
            return ExistsInAnyLocale<T>(new CultureInfo[] { });
        }


        public static bool ExistsInAnyLocale(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");

            MethodInfo methodInfo = GetExistsInAnyLocaleMethodInfo(interfaceType);

            bool result = (bool)methodInfo.Invoke(null, new object[] { });

            return result;
        }



        // Overload
        public static bool ExistsInAnyLocale(Type interfaceType, CultureInfo excludedCultureInfo)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");

            MethodInfo methodInfo = GetExistsInAnyLocaleWithParamMethodInfo(interfaceType);

            bool result = (bool)methodInfo.Invoke(null, new object[] { new CultureInfo[] { excludedCultureInfo } });

            return result;
        }

        #endregion



        #region GetXXXMethodInfo methods (Only helpers)

        public static MethodInfo GetSetDataInterceptorMethodInfo(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (typeof(IData).IsAssignableFrom(interfaceType) == false) throw new ArgumentException("The provided type must implement IData", "interfaceType");

            MethodInfo methodInfo;
            using (_resourceLocker.Locker)
            {
                if (_resourceLocker.Resources.GenericSetDataInterceptorMethodInfo.TryGetValue(interfaceType, out methodInfo) == false)
                {
                    MethodInfo nonGenericMethod = typeof(DataFacade).GetMethod(
                        "SetDataInterceptor",
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new Type[] { typeof(DataInterceptor) },
                        null);

                    methodInfo = nonGenericMethod.MakeGenericMethod(new Type[] { interfaceType });

                    _resourceLocker.Resources.GenericSetDataInterceptorMethodInfo.Add(interfaceType, methodInfo);
                }
            }

            return methodInfo;
        }



        public static MethodInfo GetHasDataInterceptorMethodInfo(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (typeof(IData).IsAssignableFrom(interfaceType) == false) throw new ArgumentException("The provided type must implement IData", "interfaceType");

            MethodInfo methodInfo;
            using (_resourceLocker.Locker)
            {
                if (_resourceLocker.Resources.GenericHasDataInterceptorMethodInfo.TryGetValue(interfaceType, out methodInfo) == false)
                {
                    MethodInfo nonGenericMethod = typeof(DataFacade).GetMethod(
                        "HasDataInterceptor",
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new Type[] { },
                        null);

                    methodInfo = nonGenericMethod.MakeGenericMethod(new Type[] { interfaceType });

                    _resourceLocker.Resources.GenericHasDataInterceptorMethodInfo.Add(interfaceType, methodInfo);
                }
            }

            return methodInfo;
        }



        public static MethodInfo GetClearDataInterceptorMethodInfo(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (typeof(IData).IsAssignableFrom(interfaceType) == false) throw new ArgumentException("The provided type must implement IData", "interfaceType");

            MethodInfo methodInfo;
            using (_resourceLocker.Locker)
            {
                if (_resourceLocker.Resources.GenericClearDataInterceptorMethodInfo.TryGetValue(interfaceType, out methodInfo) == false)
                {
                    MethodInfo nonGenericMethod = typeof(DataFacade).GetMethod(
                        "ClearDataInterceptor",
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new Type[] { },
                        null);

                    methodInfo = nonGenericMethod.MakeGenericMethod(new Type[] { interfaceType });

                    _resourceLocker.Resources.GenericClearDataInterceptorMethodInfo.Add(interfaceType, methodInfo);
                }
            }

            return methodInfo;
        }



        public static MethodInfo GetGetDataMethodInfo(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (typeof(IData).IsAssignableFrom(interfaceType) == false) throw new ArgumentException("The provided type must implement IData", "interfaceType");

            MethodInfo methodInfo;
            using (_resourceLocker.Locker)
            {
                if (_resourceLocker.Resources.GenericGetDataFromTypeMethodInfo.TryGetValue(interfaceType, out methodInfo) == false)
                {
                    MethodInfo nonGenericMethod = typeof(DataFacade).GetMethod(
                        "GetData",
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new Type[] { typeof(bool), typeof(IEnumerable<string>) },
                        null);

                    methodInfo = nonGenericMethod.MakeGenericMethod(new Type[] { interfaceType });

                    _resourceLocker.Resources.GenericGetDataFromTypeMethodInfo.Add(interfaceType, methodInfo);
                }
            }

            return methodInfo;
        }



        public static MethodInfo GetGetDataFromDataSourceIdMethodInfo(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (typeof(IData).IsAssignableFrom(interfaceType) == false) throw new ArgumentException("The provided type must implement IData", "interfaceType");

            MethodInfo methodInfo;
            using (_resourceLocker.Locker)
            {
                if (_resourceLocker.Resources.GenericGetDataFromDataSourceIdMethodInfo.TryGetValue(interfaceType, out methodInfo) == false)
                {
                    MethodInfo nonGenericMethod =
                        (from m in typeof(DataFacade).GetMethods(BindingFlags.Public | BindingFlags.Static)
                         where m.Name == "GetDataFromDataSourceId" &&
                               m.IsGenericMethodDefinition == true &&
                               m.GetParameters().Length == 2
                         select m).Single();

                    methodInfo = nonGenericMethod.MakeGenericMethod(new Type[] { interfaceType });

                    _resourceLocker.Resources.GenericGetDataFromDataSourceIdMethodInfo.Add(interfaceType, methodInfo);
                }
            }

            return methodInfo;
        }



        public static MethodInfo GetGetDataWithPredicatMethodInfo(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (typeof(IData).IsAssignableFrom(interfaceType) == false) throw new ArgumentException("The provided type must implement IData", "interfaceType");

            MethodInfo methodInfo;
            using (_resourceLocker.Locker)
            {
                if (_resourceLocker.Resources.GenericGetDataFromTypeWithPredicateMethodInfo.TryGetValue(interfaceType, out methodInfo) == false)
                {
                    MethodInfo genericMethod =
                        (from m in typeof(DataFacade).GetMethods(BindingFlags.Public | BindingFlags.Static)
                         where m.Name == "GetData" &&
                               m.IsGenericMethodDefinition == true &&
                               m.GetParameters().Length == 1 &&
                               m.GetParameters()[0].ParameterType.IsGenericType == true &&
                               m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>)
                         select m).Single();

                    methodInfo = genericMethod.MakeGenericMethod(new Type[] { interfaceType });

                    _resourceLocker.Resources.GenericGetDataFromTypeWithPredicateMethodInfo.Add(interfaceType, methodInfo);
                }
            }

            return methodInfo;
        }



        public static MethodInfo GetAddNewMethodInfo(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (typeof(IData).IsAssignableFrom(interfaceType) == false) throw new ArgumentException("The provided type must implement IData", "interfaceType");

            MethodInfo methodInfo;
            using (_resourceLocker.Locker)
            {
                if (_resourceLocker.Resources.GenericAddNewFromTypeMethodInfo.TryGetValue(interfaceType, out methodInfo) == false)
                {
                    methodInfo =
                        (from method in typeof(DataFacade).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                         where method.Name == "AddNew" &&
                               typeof(IEnumerable).IsAssignableFrom(method.GetParameters()[0].ParameterType) == false
                         select method).First();

                    methodInfo = methodInfo.MakeGenericMethod(new Type[] { interfaceType });

                    _resourceLocker.Resources.GenericAddNewFromTypeMethodInfo.Add(interfaceType, methodInfo);
                }
            }

            return methodInfo;
        }



        public static MethodInfo GetMoveMethodInfo(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (typeof(IData).IsAssignableFrom(interfaceType) == false) throw new ArgumentException("The provided type must implement IData", "interfaceType");

            MethodInfo methodInfo;
            using (_resourceLocker.Locker)
            {
                if (_resourceLocker.Resources.GenericMoveFromTypeMethodInfo.TryGetValue(interfaceType, out methodInfo) == false)
                {
                    methodInfo =
                        (from method in typeof(DataFacade).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                         where method.Name == "Move" &&
                               method.GetParameters().Length == 3
                         select method).First();

                    methodInfo = methodInfo.MakeGenericMethod(new Type[] { interfaceType });

                    _resourceLocker.Resources.GenericMoveFromTypeMethodInfo.Add(interfaceType, methodInfo);
                }
            }

            return methodInfo;
        }



        public static MethodInfo GetExistsInAnyLocaleMethodInfo(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (typeof(IData).IsAssignableFrom(interfaceType) == false) throw new ArgumentException("The provided type must implement IData", "interfaceType");

            MethodInfo methodInfo;
            using (_resourceLocker.Locker)
            {
                if (_resourceLocker.Resources.GenericExistsInAnyLocaleMethodInfo.TryGetValue(interfaceType, out methodInfo) == false)
                {
                    methodInfo =
                        (from method in typeof(DataFacade).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                         where method.Name == "ExistsInAnyLocale" &&
                               typeof(IEnumerable).IsAssignableFrom(method.GetParameters()[0].ParameterType) == false
                         select method).First();

                    methodInfo = methodInfo.MakeGenericMethod(new Type[] { interfaceType });

                    _resourceLocker.Resources.GenericExistsInAnyLocaleMethodInfo.Add(interfaceType, methodInfo);
                }
            }

            return methodInfo;
        }



        public static MethodInfo GetExistsInAnyLocaleWithParamMethodInfo(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (typeof(IData).IsAssignableFrom(interfaceType) == false) throw new ArgumentException("The provided type must implement IData", "interfaceType");

            MethodInfo methodInfo;
            using (_resourceLocker.Locker)
            {
                if (_resourceLocker.Resources.GenericExistsInAnyLocaleWithParamMethodInfo.TryGetValue(interfaceType, out methodInfo) == false)
                {
                    methodInfo =
                        (from method in typeof(DataFacade).GetMethods(BindingFlags.Static | BindingFlags.Public)
                         where
                            (method.Name == "ExistsInAnyLocale") &&
                            (method.GetParameters().Count() == 1) &&
                            (typeof(IEnumerable).IsAssignableFrom(method.GetParameters()[0].ParameterType) == true)
                         select method).First();

                    methodInfo = methodInfo.MakeGenericMethod(new Type[] { interfaceType });

                    _resourceLocker.Resources.GenericExistsInAnyLocaleWithParamMethodInfo.Add(interfaceType, methodInfo);
                }
            }

            return methodInfo;
        }

        #endregion



        private static void Flush()
        {
            _resourceLocker.ResetInitialization();
        }



        private static void OnFlushEvent(FlushEventArgs args)
        {
            Flush();
        }

        private static class EmptyPredicate<T> where T : class
        {
            public static readonly Expression<Func<T, bool>> Instance = f => true;
        }

        private sealed class Resources
        {
            public Dictionary<Type, MethodInfo> GenericSetDataInterceptorMethodInfo { get; set; }
            public Dictionary<Type, MethodInfo> GenericHasDataInterceptorMethodInfo { get; set; }
            public Dictionary<Type, MethodInfo> GenericClearDataInterceptorMethodInfo { get; set; }
            public Dictionary<Type, MethodInfo> GenericGetDataFromTypeMethodInfo { get; set; }
            public Dictionary<Type, MethodInfo> GenericGetDataFromDataSourceIdMethodInfo { get; set; }
            public Dictionary<Type, MethodInfo> GenericGetDataFromTypeWithPredicateMethodInfo { get; set; }
            public Dictionary<Type, MethodInfo> GenericAddNewFromTypeMethodInfo { get; set; }
            public Dictionary<Type, MethodInfo> GenericMoveFromTypeMethodInfo { get; set; }
            public Dictionary<Type, MethodInfo> GenericExistsInAnyLocaleMethodInfo { get; set; }
            public Dictionary<Type, MethodInfo> GenericExistsInAnyLocaleWithParamMethodInfo { get; set; }


            public static void DoInitializeResources(Resources resources)
            {
                resources.GenericSetDataInterceptorMethodInfo = new Dictionary<Type, MethodInfo>();
                resources.GenericHasDataInterceptorMethodInfo = new Dictionary<Type, MethodInfo>();
                resources.GenericClearDataInterceptorMethodInfo = new Dictionary<Type, MethodInfo>();
                resources.GenericGetDataFromTypeMethodInfo = new Dictionary<Type, MethodInfo>();
                resources.GenericGetDataFromDataSourceIdMethodInfo = new Dictionary<Type, MethodInfo>();
                resources.GenericGetDataFromTypeWithPredicateMethodInfo = new Dictionary<Type, MethodInfo>();
                resources.GenericAddNewFromTypeMethodInfo = new Dictionary<Type, MethodInfo>();
                resources.GenericMoveFromTypeMethodInfo = new Dictionary<Type, MethodInfo>();
                resources.GenericExistsInAnyLocaleMethodInfo = new Dictionary<Type, MethodInfo>();
                resources.GenericExistsInAnyLocaleWithParamMethodInfo = new Dictionary<Type, MethodInfo>();
            }
        }
    }
}
