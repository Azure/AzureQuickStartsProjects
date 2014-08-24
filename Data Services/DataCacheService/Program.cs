//----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//----------------------------------------------------------------------------------

using DataCacheService.DataModel;
using Microsoft.ApplicationServer.Caching;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DataCacheService
{
    class Program
    {
        private const string PRODUCTS = "products";

        static void Main(string[] args)
        {
            //***************************************************************************************************************************
            //TODO: provision your cache and update App.Config with your [cache_endpoint_identifier] (e.g yourendpoint.cache.windows.net)
            //      and [Authentication Key] http://go.microsoft.com/fwlink/?LinkID=325011
            //***************************************************************************************************************************

            //1. Option1: Get a handle to named cached while creating an instance
            Console.WriteLine("1. Get a handle to named cached while creating an instance");
            DataCache cache = new DataCache("default");

            //2. Alternatively you can get a handle to your cache using DataCacheFactory.  You dont have to do both.
            Console.WriteLine("2. Alternatively you can get a handle to your cache using DataCacheFactory");

            DataCacheFactory cacheFactory = new DataCacheFactory();
            cache = cacheFactory.GetDefaultCache();
            // Or DataCache cache = cacheFactory.GetCache("MyCache");
            // cache can now be used to add and retrieve items.

            // 3. Lookup items in cache, if no items present retrieve and store from data source
            // Add products to the cache, keyed by "products"
            Console.WriteLine("3. Lookup items in cache, if no items present retrieve from data source and store in cache");

            //First hit will be slow as hitting data source. The below demonstrates a standard pattern for request and optionally hydrate the cache
            var products = cache.Get(PRODUCTS) as IList<Product>;
            if (products == null)
            {
                // "Item" not in cache. Obtain it from specified data source
                // and add it.
                products = GetDataFromDataSource();
                cache.Add(PRODUCTS, products);
            }

            //Subsequent hits will be direct from cache so long as item has not timed out
            //the GetCollectionFromCache demonstrates one way to implement the above using a generic function
            Console.WriteLine("4. Subsequent hits will be direct from cache so long as item has not timed out");

            products = GetCollectionFromCache(cache, PRODUCTS, () => { return GetDataFromDataSource(); });
            LogProductsToConsole(products);

            // Attempting to .Add an item with the same key will throw a DataCacheException
            Console.WriteLine("5. Attempting to .Add an item with the same key will throw a DataCacheException");

            try
            {
                cache.Add(PRODUCTS, products);
            }
            catch (DataCacheException ex)
            {
                Console.WriteLine("\t{0}", ex.Message.ToString());
            }

            // Calling .Put will add the item or if it exists, it will replace it
            Console.WriteLine("6. Call .Put will add the item or if it exists, it will replace it");
            products[1].Name += DateTime.Now;

            cache.Put(PRODUCTS, products);

            LogProductsToConsole(cache.Get(PRODUCTS) as IList<Product>);

            //To remove an item call .Remove
            Console.WriteLine("7. To remove an item call .Remove");
            if (cache.Remove(PRODUCTS))
                Console.WriteLine("\tItem under key '{0}' removed from cache", PRODUCTS);

            // You can also cache an item with an expiration different to the default by providing a timeout
            Console.WriteLine("8. You can also cache an item with an expiration different to the default by providing a timeout");

            cache.Add(PRODUCTS, products, TimeSpan.FromMinutes(30));

            // Get a DataCacheItem object that contains information about
            // item in the cache. If there is no object keyed by "products" null
            // is returned. 
            Console.WriteLine("9.  Method .GetCacheItem returns DataCacheItem object that contains information about the cached item");

            DataCacheItem item = cache.GetCacheItem(PRODUCTS);
            TimeSpan timeRemaining = item.Timeout;
            Console.WriteLine("\tDataCacheItem has a:");

            Console.WriteLine("\t\t CacheName:\t{0}", item.CacheName);
            Console.WriteLine("\t\t RegionName:\t{0}", item.RegionName);
            Console.WriteLine("\t\t Key:\t\t{0}", item.Key);
            Console.WriteLine("\t\t Size:\t\t{0}", item.Size);
            Console.WriteLine("\t\t Timeout:\t{0}", item.Timeout.ToString());
            Console.WriteLine("\t\t Tag Count:\t{0}", item.Tags.Count);
            Console.WriteLine("\t\t Value:\t{0}", item.Value.ToString());
            Console.WriteLine("\t\t Version:\t{0}", item.Version.ToString());

            Console.ReadLine();
        }

        private static void LogProductsToConsole(IList<Product> products)
        {
            foreach (var product in products)
                Console.WriteLine("\t Product Id: {0}, Name: {1}", product.Id, product.Name);
        }

        /// <summary>
        /// Returns a collection of T from the cache for a given key.  If the cache is empty Func QueryDataStore will retrieve 
        /// data from the data store and this method will then add or update the cache with the Functions result.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="cache">DataCache instance</param>
        /// <param name="key">The key to lookup from the DataCache instance</param>
        /// <param name="QueryDataStore">A function returning a IList of T that is used to retrieve data from your datastore when the requested key is empty</param>
        /// <returns></returns>
        static IList<T> GetCollectionFromCache<T>(DataCache cache, string key, Func<IList<T>> QueryDataStore)
        {
            IList<T> results = cache.Get(key) as IList<T>;

            // If results are null hit the data store
            if (results == null)
            {
                results = QueryDataStore();
                cache.Put(key, results);
            }

            return results;
        }

        static IList<Product> GetDataFromDataSource()
        {
            // you would normally hit your repository, service, storage
            // adding a delay here to simulate a read from disk/service
            Thread.Sleep(200);

            var products = new List<Product>();
            products.Add(new Product() { Id = 0, Name = "Product 0" });
            products.Add(new Product() { Id = 1, Name = "Product 1" });

            return products;
        }





    }
}
