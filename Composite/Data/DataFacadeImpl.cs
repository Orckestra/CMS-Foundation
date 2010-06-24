using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Composite.Data.Caching;
using Composite.Data.DynamicTypes;
using Composite.Data.Foundation;
using Composite.Data.Foundation.PluginFacades;
using Composite.Linq;
using Composite.Logging;
using Composite.Security;
using Composite.Threading;
using Composite.Types;
using Composite.Validation;
using Microsoft.Practices.EnterpriseLibrary.Validation;


namespace Composite.Data
{
    internal class DataFacadeImpl : IDataFacade
    {
        static readonly Cache<string, IData> _dataBySourceIdCache = new Cache<string, IData>("Data by sourceId", 2000);

        static DataFacadeImpl()
        {
            DataEventSystemFacade.SubscribeToDataAfterUpdate<IData>(OnDataChanged, true);
            DataEventSystemFacade.SubscribeToDataDeleted<IData>(OnDataChanged, true);

            DataEventSystemFacade.SubscribeToDataBeforeAdd<IData>(SetChangeHistoryInformation, true);
            DataEventSystemFacade.SubscribeToDataBeforeUpdate<IData>(SetChangeHistoryInformation, true);
        }




        public IQueryable<T> GetData<T>(bool useCaching, IEnumerable<string> providerNames)
            where T : class, IData
        {
            IQueryable<T> resultQueryable;

            if (DataProviderRegistry.AllInterfaces.Contains(typeof(T)) == true)
            {
                if (useCaching && DataCachingFacade.IsDataAccessCacheEnabled(typeof(T)))
                {
                    resultQueryable = DataCachingFacade.GetDataFromCache<T>();
                }
                else
                {
                    if (providerNames == null)
                    {
                        providerNames = DataProviderRegistry.GetDataProviderNamesByInterfaceType(typeof(T));
                    }

                    List<IQueryable<T>> queryables = new List<IQueryable<T>>();
                    foreach (string providerName in providerNames)
                    {
                        IQueryable<T> queryable = DataProviderPluginFacade.GetData<T>(providerName);

                        queryables.Add(queryable);
                    }

                    DataFacadeQueryable<T> multibleSourceQueryable = new DataFacadeQueryable<T>(queryables);

                    resultQueryable = multibleSourceQueryable;
                }
            }
            else if (typeof(T).GetCustomInterfaceAttributes<AutoUpdatebleAttribute>().Any())
            {
                resultQueryable = new List<T>().AsQueryable();
            }
            else
            {
                throw new ArgumentException(string.Format("The given interface type ({0}) is not supported by any data providers", typeof(T)));
            }

            DataInterceptor dataInterceptor;
            this.DataInterceptors.TryGetValue(typeof(T), out dataInterceptor);
            if (dataInterceptor != null)
            {
                try
                {
                    resultQueryable = dataInterceptor.InterceptGetData<T>(resultQueryable);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError("DataFacade", "Calling data interceptor failed with the following exception");
                    LoggingService.LogError("DataFacade", ex);
                }
            }

            return resultQueryable;
        }





        public T GetDataFromDataSourceId<T>(DataSourceId dataSourceId, bool useCaching)
            where T : class, IData
        {
            if (null == dataSourceId) throw new ArgumentNullException("dataSourceId");

            useCaching = useCaching && DataCachingFacade.IsTypeCacheble(typeof(T));

            using (new DataScope(dataSourceId.DataScopeIdentifier, dataSourceId.LocaleScope))
            {
                T resultData = null;

                string cacheKey = string.Empty;
                if (useCaching)
                {
                    cacheKey = dataSourceId.ToString();
                    resultData = (T)_dataBySourceIdCache.Get(cacheKey);
                }

                if (resultData == null)
                {
                    resultData = DataProviderPluginFacade.GetData<T>(dataSourceId.ProviderName, dataSourceId.DataId);

                    if (useCaching && resultData != null && _dataBySourceIdCache.Enabled)
                    {
                        _dataBySourceIdCache.Add(cacheKey, resultData);
                    }
                }

                if (useCaching && resultData != null)
                {
                    resultData = DataWrappingFacade.Wrap(resultData);
                }

                DataInterceptor dataInterceptor;
                this.DataInterceptors.TryGetValue(typeof(T), out dataInterceptor);
                if (dataInterceptor != null)
                {
                    try
                    {
                        resultData = dataInterceptor.InterceptGetDataFromDataSourceId<T>(resultData);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError("DataFacade", "Calling data interceptor failed with the following exception");
                        LoggingService.LogError("DataFacade", ex);
                    }
                }

                return resultData;
            }
        }



        public void SetDataInterceptor<T>(DataInterceptor dataInterceptor) where T : class, IData
        {
            if (this.DataInterceptors.ContainsKey(typeof(T)) == true) throw new InvalidOperationException("A data interceptor has already been set");

            this.DataInterceptors.Add(typeof(T), dataInterceptor);

            LoggingService.LogVerbose("DataFacade", string.Format("Data interception added to the data type '{0}' with interceptor type '{1}'", typeof(T), dataInterceptor.GetType()));
        }



        public bool HasDataInterceptor<T>() where T : class, IData
        {
            return this.DataInterceptors.ContainsKey(typeof(T));
        }



        public void ClearDataInterceptor<T>() where T : class, IData
        {
            if (this.DataInterceptors.ContainsKey(typeof(T)) == true)
            {
                this.DataInterceptors.Remove(typeof(T));

                LoggingService.LogVerbose("DataFacade", string.Format("Data interception cleared for the data type '{0}'", typeof(T)));
            }
        }



        private Dictionary<Type, DataInterceptor> DataInterceptors
        {
            get
            {
                const string threadDataKey = "DataFacade:DataInterceptors";

                var threadData = ThreadDataManager.GetCurrentNotNull();

                Dictionary<Type, DataInterceptor> dataInterceptors = threadData.GetValue(threadDataKey) as Dictionary<Type, DataInterceptor>;

                if (dataInterceptors == null)
                {
                    dataInterceptors = new Dictionary<Type, DataInterceptor>();
                    threadData.SetValue(threadDataKey, dataInterceptors);
                }

                return dataInterceptors;
            }
        }



        public void Update(IEnumerable<IData> dataset, bool suppressEventing, bool performForeignKeyIntegrityCheck, bool performeValidation)
        {
            Verify.ArgumentNotNull(dataset, "dataset");

            Dictionary<string, Dictionary<Type, List<IData>>> sortedDataset = dataset.ToDataProviderAndInterfaceTypeSortedDictionary();

            if (!suppressEventing)
            {
                foreach (IData data in dataset)
                {
                    DataEventSystemFacade.FireDataBeforeUpdateEvent(data.DataSourceId.InterfaceType, data);
                }
            }


            foreach (IData data in dataset)
            {
                if (performeValidation == true)
                {
                    ValidationResults validationResults = ValidationFacade.Validate(data);

                    if (validationResults.IsValid == false)
                    {
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();

                        foreach (ValidationResult result in validationResults)
                        {
                            sb.AppendLine(result.Message);
                        }

                        throw new InvalidOperationException(string.Format("The data of type '{0}' did not validate, with the following errors: {1}", data.DataSourceId.InterfaceType, sb.ToString()));
                    }
                }

                if (performForeignKeyIntegrityCheck)
                {
                    data.ValidateForeignKeyIntegrity();
                }
            }


            foreach (KeyValuePair<string, Dictionary<Type, List<IData>>> providerPair in sortedDataset)
            {
                foreach (KeyValuePair<Type, List<IData>> interfaceTypePair in providerPair.Value)
                {
                    List<IData> dataToUpdate = interfaceTypePair.Value;

                    if (DataCachingFacade.IsTypeCacheble(interfaceTypePair.Key) == true)
                    {
                        List<IData> newDataToUpdate = new List<IData>();

                        foreach (IData d in interfaceTypePair.Value)
                        {
                            newDataToUpdate.Add(DataWrappingFacade.UnWrap(d));
                        }

                        dataToUpdate = newDataToUpdate;
                    }

                    DataProviderPluginFacade.Update(providerPair.Key, dataToUpdate);

                    if (DataCachingFacade.IsTypeCacheble(interfaceTypePair.Key) == true)
                    {
                        DataCachingFacade.ClearCache(interfaceTypePair.Key);
                    }
                }
            }


            if (!suppressEventing)
            {
                foreach (IData data in dataset)
                {
                    DataEventSystemFacade.FireDataAfterUpdateEvent(data.DataSourceId.InterfaceType, data);
                }
            }
        }



        public List<T> AddNew<T>(IEnumerable<T> datas, bool allowStoreCreation, bool suppressEventing, bool performForeignKeyIntegrityCheck, bool performeValidation, List<string> writeableProviders)
            where T : class, IData
        {
            if (writeableProviders == null)
            {
                writeableProviders = DataProviderRegistry.GetWriteableDataProviderNamesByInterfaceType(typeof(T));
                if (writeableProviders.Count > 1 && writeableProviders.Contains(DataProviderRegistry.DefaultDynamicTypeDataProviderName))
                {
                    writeableProviders = new List<string> { DataProviderRegistry.DefaultDynamicTypeDataProviderName };
                }
            }

            if ((writeableProviders.Count == 0) &&
                (typeof(T).GetCustomInterfaceAttributes<AutoUpdatebleAttribute>().Any()) &&
                (allowStoreCreation == true))
            {
                LoggingService.LogVerbose("DataFacade", string.Format("Type data interface '{0}' is marked auto updateble and is not supported by any providers adding it to the default dynamic type data provider", typeof(T)));

                List<T> result;
                DynamicTypeManager.EnsureCreateStore(typeof(T));

                result = AddNew<T>(datas, false, suppressEventing, performForeignKeyIntegrityCheck, performeValidation, null);
                return result;
            }
            if (writeableProviders.Count == 1)
            {
                return AddNew_AddingMethod<T>(writeableProviders[0], datas, suppressEventing, performForeignKeyIntegrityCheck, performeValidation);
            }
            throw new InvalidOperationException(string.Format("{0} writeable data providers exists for data '{1}'.", writeableProviders.Count, typeof(T)));
        }



        private static List<T> AddNew_AddingMethod<T>(string providerName, IEnumerable<T> datas, bool suppressEventing, bool performForeignKeyIntegrityCheck, bool performeValidation)
             where T : class, IData
        {
            if (true == string.IsNullOrEmpty(providerName)) throw new ArgumentNullException("providerName");
            if (null == datas) throw new ArgumentNullException("datas");
            if (datas.Contains(null) == true) throw new ArgumentException("The enumerations 'datas' may not contain null's");

            List<T> addedDatas;

            List<string> writeableProviders = DataProviderRegistry.GetWriteableDataProviderNamesByInterfaceType(typeof(T));
            if (writeableProviders.Contains(providerName) == false)
            {
                LoggingService.LogVerbose("DataFacade", string.Format("Type data interface '{0}' is marked auto updateble and is not supported by the provider '{1}', adding it", typeof(T), providerName));

                DynamicTypeManager.EnsureCreateStore(typeof(T), providerName);
            }

            writeableProviders = DataProviderRegistry.GetWriteableDataProviderNamesByInterfaceType(typeof(T));
            if (writeableProviders.Contains(providerName) == false)
            {
                throw new InvalidOperationException(string.Format("The writeable data providers '{0}' does not support the interface '{1}'.", providerName, typeof(T)));
            }


            foreach (T data in datas)
            {
                if (performeValidation == true)
                {
                    ValidationResults validationResults = ValidationFacade.Validate<T>(data);

                    if (validationResults.IsValid == false)
                    {
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();

                        foreach (ValidationResult result in validationResults)
                        {
                            sb.AppendLine(result.Message);
                        }

                        throw new InvalidOperationException(string.Format("The data of type '{0}' did not validate, with the following errors: {1}", typeof(T), sb.ToString()));
                    }
                }

                if (performForeignKeyIntegrityCheck == true)
                {
                    data.ValidateForeignKeyIntegrity();
                }
            }


            if (suppressEventing == false)
            {
                foreach (T data in datas)
                {
                    DataEventSystemFacade.FireDataBeforeAddEvent<T>(data);
                }
            }

            addedDatas = DataProviderPluginFacade.AddNew<T>(providerName, datas);

            if (DataCachingFacade.IsTypeCacheble(typeof(T)) == true)
            {
                DataCachingFacade.ClearCache(typeof(T));
            }

            if (suppressEventing == false)
            {
                foreach (T data in addedDatas)
                {
                    DataEventSystemFacade.FireDataAfterAddEvent<T>(data);
                }
            }


            return addedDatas;
        }



        public void Delete<T>(IEnumerable<T> dataset, bool suppressEventing, CascadeDeleteType cascadeDeleteType, bool referencesFromAllScopes)
            where T : class, IData
        {
            Verify.ArgumentNotNull(dataset, "dataset");

            dataset = dataset.Evaluate();

            if (cascadeDeleteType != CascadeDeleteType.Disable)
            {
                foreach (IData element in dataset)
                {
                    Verify.ArgumentCondition(element != null, "dataset", "datas may not contain nulls");

                    if (element.IsDataReferfed() == true)
                    {
                        Verify.IsTrue(cascadeDeleteType != CascadeDeleteType.Disallow, "One of the given datas is referenced by one or more datas");

                        element.RemoveOptionalReferences();

                        IEnumerable<IData> referees;
                        using (new DataScope(element.DataSourceId.DataScopeIdentifier))
                        {
                            // For some wierd reason, this line does not work.... /MRJ
                            // IEnumerable<IData> referees = dataset.GetRefereesRecursively();
                            referees = element.GetReferees(referencesFromAllScopes);
                        }

                        foreach (IData referee in referees)
                        {
                            if (referee.CascadeDeleteAllowed(element.DataSourceId.InterfaceType) == false)
                            {
                                throw new InvalidOperationException(string.Format("One of the given datas is referenced by one or more datas that does not allow cascade delete"));
                            }
                        }

                        Delete<IData>(referees, suppressEventing, cascadeDeleteType, referencesFromAllScopes);
                    }
                }
            }


            Dictionary<string, Dictionary<Type, List<IData>>> sortedDatas = dataset.ToDataProviderAndInterfaceTypeSortedDictionary();

            foreach (KeyValuePair<string, Dictionary<Type, List<IData>>> providerPair in sortedDatas)
            {
                foreach (KeyValuePair<Type, List<IData>> interfaceTypePair in providerPair.Value)
                {
                    DataProviderPluginFacade.Delete(providerPair.Key, interfaceTypePair.Value.Select(d => d.DataSourceId));

                    if (DataCachingFacade.IsTypeCacheble(interfaceTypePair.Key) == true)
                    {
                        DataCachingFacade.ClearCache(interfaceTypePair.Key, interfaceTypePair.Value.First().DataSourceId.DataScopeIdentifier);
                    }
                }
            }


            if (suppressEventing == false)
            {
                foreach (IData element in dataset)
                {
                    DataEventSystemFacade.FireDataDeletedEvent(element.DataSourceId.InterfaceType, element);
                }
            }
        }



        public DataMoveResult Move<T>(T data, DataScopeIdentifier targetDataScopeIdentifier, bool allowCascadeMove)
            where T : class, IData
        {
            if (data == null) throw new ArgumentNullException("data");
            if (targetDataScopeIdentifier == null) throw new ArgumentNullException("targetDataScopeIdentifier");

            T addedData;
            List<IData> addedReferees = new List<IData>();

            if (allowCascadeMove == true)
            {
                IEnumerable<IData> referees = data.GetRefereesRecursively();

                using (DataScope dataScope = new DataScope(targetDataScopeIdentifier))
                {
                    addedData = DataFacade.AddNew<T>(data, true, false);

                    foreach (IData referee in referees)
                    {
                        addedReferees.Add(DataFacade.AddNew(referee, false));
                    }
                }

                using (DataScope dataScope = new DataScope(data.DataSourceId.DataScopeIdentifier))
                {
                    DataFacade.Delete(data, true, CascadeDeleteType.Disable);
                }

                foreach (IData referee in referees)
                {
                    using (DataScope dataScope = new DataScope(referee.DataSourceId.DataScopeIdentifier))
                    {
                        DataFacade.Delete(referee, true, CascadeDeleteType.Disable);
                    }
                }


                DataEventSystemFacade.FireDataAfterMoveEvent<T>(data, targetDataScopeIdentifier);

                foreach (IData referee in referees)
                {
                    DataEventSystemFacade.FireDataAfterMoveEvent<T>(referee, targetDataScopeIdentifier);
                }
            }
            else
            {
                using (DataScope dataScope = new DataScope(targetDataScopeIdentifier))
                {
                    addedData = DataFacade.AddNew<T>(data, true, true);
                }

                using (DataScope dataScope = new DataScope(data.DataSourceId.DataScopeIdentifier))
                {
                    DataFacade.Delete(data, true, CascadeDeleteType.Disallow);
                }

                DataEventSystemFacade.FireDataAfterMoveEvent<T>(data, targetDataScopeIdentifier);
            }


            DataMoveResult dataMoveResult = new DataMoveResult(addedData, addedReferees);

            return dataMoveResult;
        }



        public bool ExistsInAnyLocale<T>(IEnumerable<CultureInfo> excludedCultureInfoes)
            where T : class, IData
        {
            foreach (CultureInfo cultureInfo in DataLocalizationFacade.ActiveLocalizationCultures.Except(excludedCultureInfoes))
            {
                using (DataScope dataScope = new DataScope(cultureInfo))
                {
                    bool exists = DataFacade.GetData<T>().Any();
                    if (exists == true)
                    {
                        return true;
                    }
                }
            }

            return false;
        }



        private static void OnDataChanged(DataEventArgs dataEventArgs)
        {
            _dataBySourceIdCache.Remove(dataEventArgs.Data.DataSourceId.ToString());
        }



        private static void SetChangeHistoryInformation(DataEventArgs dataEventArgs)
        {
            IData data = dataEventArgs.Data;
            if (data != null && data is IChangeHistory)
            {
                (data as IChangeHistory).ChangeDate = DateTime.Now;

                try
                {
                    if (UserValidationFacade.IsLoggedIn())
                    {
                        (data as IChangeHistory).ChangedBy = UserValidationFacade.GetUsername();
                    }
                }
                catch
                {
                    // silent
                }
            }
        }
    }
}
