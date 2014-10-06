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

using System;
using System.Collections.Generic;
using System.Threading;
using StackExchange.Redis;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text;

namespace DataCacheService
{
    class Program
    {
        //***************************************************************************************************************************
        // TODO: provision your cache, configure the cache client, and configure the ConnectionMultiplexer. In this example the
        //       ConnectionMultiplexer is configured using lazy initialization which provides a thread safe way to ensure that only
        //       one ConnectedMultiplexer instance is used. 
        //       For instructions on creating an Azure Redis Cache instance and connecting to a cache, see:
        //           http://aka.ms/CreateAzureRedisCache 
        //           http://aka.ms/ConfigureAzureRedisCacheClients
        // NOTE: Never store credentials in source code. In this example they are hardcoded into the code for simplicity. For information
        //       on how to store credentials, see:
        //           Azure Websites: How Application Strings and Connection Strings Work 
        //           http://azure.microsoft.com/blog/2013/07/17/windows-azure-web-sites-how-application-strings-and-connection-strings-work/
        //***************************************************************************************************************************
        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            // Replace these values with the values from your Azure Redis Cache instance.
            // For more information, see http://aka.ms/ConnectToTheAzureRedisCache
            return ConnectionMultiplexer.Connect("<your cache name here>.redis.cache.windows.net,abortConnect=false,ssl=true,password=...");
        });

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

        static void Main(string[] args)
        {

            // 1. Get a reference to your Azure Redis Cache.
            //    Azure Redis Cache instances have a default of 16 databases,
            //    numbered 0-15, with 0 being the default database if none
            //    is specified. These databases share the memory of the cache.
            //    For more information, see http://aka.ms/ConfigureAzureRedisCache  
            Console.WriteLine("1. Get a reference to the redis cache database");
            IDatabase cache = Connection.GetDatabase();

            // 2. Store integral data types in the cache and retrieve them. In the StackExchange.Redis
            //    client, StringSet and StringGet are used for integral types, even if they are not strings.
            //    For more information, see http://aka.ms/CachingDataInAzureRedisCache
            Console.WriteLine("2. Store and retrieve integral types from the cache.");
            cache.StringSet("item1", "Value 1");
            cache.StringSet("item2", 25);

            string item1 = cache.StringGet("item1");
            int item2 = (int)cache.StringGet("item2");

            Console.WriteLine("Item 1: {0}, Item 2: {1}", item1, item2);

            // 3. To store .NET objects in the cache, they must be serialized. This is the responsibility
            //    of the application developer. This example serializes and deserializes an instance of
            //    the Employee class defined below using the BinaryFormatter.
            //    For more information, see http://aka.ms/StoreNetObjectsInAzureRedisCache
            Console.WriteLine("3. Store and retrieve .NET objects from the cache.");
            Employee e25 = new Employee(25, "Clayton Gragg");
            cache.StringSet("e25", Serialize(e25));

            // Retrieve the employee from the cache, and write it to the console.
            Console.WriteLine(Deserialize<Employee>(cache.StringGet("e25")));

            // One way to do this is to use extension methods to extend IDatabase. This next example
            // uses a SampleStackExchangeRedisExtensions class to add Get and Set methods that
            // serialize and deserialize .NET objects for you using BinaryFormatter.
            // For more information, see http://aka.ms/SampleStackExchangeRedisExtensions  
            // Placing an object in the cache with the same key as another item replaces that item,
            // as shown in this next example.
            cache.Set("e25", e25);

            // Retrieve it as an Employee.
            Employee e25copy = cache.Get<Employee>("e25");

            // Retrieve it as an object.
            e25copy = (Employee)cache.Get("e25");

            // 4. Lookup items in cache, if no items present retrieve and store from data source. This is
            //    known as the cache-aside pattern.
            Console.WriteLine("4. Lookup items in cache, if no items present retrieve from data source and store in cache");

            // First hit will be slower since the item is not in the cache.
            var employees = cache.Get("Employees") as IList<Employee>;
            if (employees == null)
            {
                // Item not in cache. Obtain it from specified data source
                // and add it.
                // GetDataSource is a simulated database call that returns an IList<Employee>.
                employees = GetDataFromDataSource();
                cache.Set("employees", employees);
            }

            // Subsequent hits will be direct from cache so long as item has not timed out.
            Console.WriteLine("Subsequent hits will be direct from cache so long as item has not timed out.");
            LogEmployeesToConsole(cache.Get<IList<Employee>>("employees"));

            // To remove an item call KeyDelete with the item's key.
            Console.WriteLine("7. To remove an item call Remove");
            if (cache.KeyDelete("employees"))
            {
                Console.WriteLine("Item under key 'employees' removed from cache");
            }

            // 8. You can also cache an item with a specific expiration time by providing a timeout.
            Console.WriteLine("8. You can also cache an item with a specific expiration time by providing a timeout.");

            // In this example the timeout is 5 seconds.
            cache.StringSet("item25", "Time sensitive item", TimeSpan.FromSeconds(5));

            Console.WriteLine("Item present in cache: {0}", cache.StringGet("item25"));
            Console.Write("Sleeping for 6 seconds");
            for (int i = 0; i < 12; i++)
            {
                Console.Write(".");
                Thread.Sleep(500);
            }
            Console.WriteLine();
            Console.WriteLine("Attempt to read expired item from cache: {0}", cache.StringGet("item25"));

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static void LogEmployeesToConsole(IList<Employee> employees)
        {
            foreach (var employee in employees)
            {
                Console.WriteLine(employee);
            }
        }

        static IList<Employee> GetDataFromDataSource()
        {
            // You would normally hit your repository, service, storage here.
            // Adding a delay here to simulate a read from disk/service.
            Thread.Sleep(200);

            var employees = new List<Employee>();
            for (int i = 0; i < 10; i++)
            {
                employees.Add(new Employee(i, string.Format("Employee {0}", i)));
            }

            return employees;
        }

        // Serialize a .NET object to a byte array.
        static byte[] Serialize(object o)
        {
            if (o == null)
            {
                return null;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, o);
                byte[] objectDataAsStream = memoryStream.ToArray();
                return objectDataAsStream;
            }
        }

        // Deserialize a .NET object from a byte array.
        static T Deserialize<T>(byte[] stream)
        {
            if (stream == null)
            {
                return default(T);
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream(stream))
            {
                T result = (T)binaryFormatter.Deserialize(memoryStream);
                return result;
            }
        }
    }

    [Serializable]
    class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public Employee(int EmployeeId, string Name)
        {
            this.Id = EmployeeId;
            this.Name = Name;
        }

        public override string ToString()
        {
            return string.Format("EmployeeId: {0}, Employee Name: {1}", Id, Name);
        }
    }

    // Sample extension class that extends StackExchange.Redis.IDatabase and provides
    // Get and Set methods that perform the required serialization of .NET objects
    // using BinaryFormatter.
    // For more information, see http://aka.ms/SampleStackExchangeRedisExtensions  
    public static class SampleStackExchangeRedisExtensions
    {
        public static T Get<T>(this IDatabase cache, string key)
        {
            return Deserialize<T>(cache.StringGet(key));
        }

        public static object Get(this IDatabase cache, string key)
        {
            return Deserialize<object>(cache.StringGet(key));
        }

        public static void Set(this IDatabase cache, string key, object value)
        {
            cache.StringSet(key, Serialize(value));
        }

        static byte[] Serialize(object o)
        {
            if (o == null)
            {
                return null;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, o);
                byte[] objectDataAsStream = memoryStream.ToArray();
                return objectDataAsStream;
            }
        }

        static T Deserialize<T>(byte[] stream)
        {
            if (stream == null)
            {
                return default(T);
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream(stream))
            {
                T result = (T)binaryFormatter.Deserialize(memoryStream);
                return result;
            }
        }
    }
}