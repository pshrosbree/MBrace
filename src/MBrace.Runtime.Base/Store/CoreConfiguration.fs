﻿namespace Nessos.MBrace.Runtime.Store

    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module CoreConfiguration =
    
        open Nessos.MBrace.Core
        open Nessos.MBrace.Utils
        open Nessos.MBrace.Runtime

        let activate (storeInfo : StoreInfo, cacheStoreEndpoint) : CoreConfiguration =
            match StoreRegistry.TryGetCoreConfiguration storeInfo.Id with
            | Some config -> config
            | None ->
                let store = storeInfo.Store
                // fsStore used but caches
                // inMemCache used by cref store
                // localCache used by cseq/cfile store
                let fsStore = new FileSystemStore(cacheStoreEndpoint)
                let inMemCache = new LocalObjectCache(fsStore)
                let localCache = new LocalCacheStore("localCacheStore", fsStore, store)

                let crefStore  = new CloudRefProvider(storeInfo, inMemCache)  :> ICloudRefProvider
                let cseqStore  = new CloudSeqProvider(storeInfo, localCache)  :> ICloudSeqProvider
                let mrefStore  = new MutableCloudRefProvider(storeInfo)       :> IMutableCloudRefProvider
                let cfileStore = new CloudFileProvider(storeInfo, localCache) :> ICloudFileProvider

                let cloner = 
                    {
                        new IObjectCloner with
                            member __.Clone(t : 'T) = Serialization.Clone t
                    }

                let coreConfig : CoreConfiguration =
                    {
                        CloudRefProvider        = crefStore
                        CloudSeqProvider        = cseqStore
                        CloudFileProvider       = cfileStore
                        MutableCloudRefProvider = mrefStore
                        Cloner                  = cloner
                    }

                StoreRegistry.RegisterCoreConfiguration(storeInfo.Id, coreConfig)

                coreConfig
        