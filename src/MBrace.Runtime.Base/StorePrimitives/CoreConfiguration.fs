﻿namespace Nessos.MBrace.Runtime.Store

    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module CoreConfiguration =
    
        open Nessos.MBrace.Core
        open Nessos.MBrace.Utils
        open Nessos.MBrace.Runtime
        open Nessos.FsPickler

        let Create (logger : ILogger, pickler : FsPickler, store : IStore, cacheStoreEndpoint) : CoreConfiguration =
            // fsStore used but caches
            // inMemCache used by cref store
            // localCache used by cseq/cfile store
            let fsStore = new FileSystemStore(cacheStoreEndpoint)
            let inMemCache = new Cache(fsStore, Serializer.Pickler)
            let localCache = new LocalCacheStore(fsStore, store)

            let crefStore  = new CloudRefProvider(store, inMemCache)  :> ICloudRefProvider
            let cseqStore  = new CloudSeqProvider(store, localCache)  :> ICloudSeqProvider
            let mrefStore  = new MutableCloudRefProvider(store)       :> IMutableCloudRefProvider
            let cfileStore = new CloudFileProvider(store, localCache) :> ICloudFileProvider
            let clogsStore = new StoreLogger(store, batchCount = 50, batchTimespan = 500) 

            let cloner = 
                {
                    new IObjectCloner with
                        member __.Clone(t : 'T) = t |> Serializer.Pickler.Pickle |> Serializer.Pickler.UnPickle
                }

            {
                CloudRefProvider        = crefStore
                CloudSeqProvider        = cseqStore
                CloudFileProvider       = cfileStore
                MutableCloudRefProvider = mrefStore
                CloudLogger             = clogsStore
                Cloner                  = cloner
            }
        