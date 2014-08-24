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

using DataSQLEF.DataModel;
using System;
using System.Collections.Generic;
using System.Data.Entity.Spatial;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSQLEF
{
    class Program
    {
        static void Main(string[] args)
        {
            //******************************************************************************************************
            //TODO: 1. In the App.config set the connection string named DataSQLEF.DataModel.OrderDbContext 
            //	       to that of your Azure SQL Server.
            //      2. Note that you must have configured the Azure SQL Server to allow connections from your 
            //	       host IP in the portal
            //*****************************************************************************************************

            Console.WriteLine("- Create a context which will scaffold the DB if not already done");
            using (OrderDbContext ctx = new OrderDbContext())
            {

                Console.WriteLine("- Adding an Order to the db context");
                var order = new Order
                {
                    Title = string.Format("Some order {0}", DateTime.Now.Ticks),
                    Status = OrderStatus.New,
                    DeliveryLocation = DbGeography.FromText("POINT(-122.336106 47.605049)"),
                    DeliveryDate = DateTime.Now.AddDays(10)
                };

                ctx.Orders.Add(order);

                Console.WriteLine("- Adding OrderItems to the db context");
                ctx.OrderItems.Add(new OrderItem
                {
                    Qty = 10,
                    Title = "Visual Studio 2013",
                    Order = order
                });

                ctx.OrderItems.Add(new OrderItem
                 {
                     Qty = 21,
                     Title = "Office 2013",
                     Order = order
                 });

                //Saves changes to Azure SQL DB
                Console.WriteLine("- Save DB context changes to Azure SQL DB");
                ctx.SaveChanges();

                Console.WriteLine("- Query Server for Orders");
                //Query orders back from the server
                var orders = from o in ctx.Orders
                             where o.DeliveryDate > DateTime.Now
                             select o;

                //Alternatively you could have used an Linq expression
                //var orders = ctx.Orders.Where(o => o.DeliveryDate > DateTime.Now);

                //Printing the orders
                foreach (var o in orders)
                {
                    Console.WriteLine(string.Format("\t Order: {0}, {1}, {2}, {3}", o.OrderId, o.Title, o.Status, o.DeliveryLocation.AsText()));

                    Console.WriteLine("\t\t Order Items:");
                    foreach (var item in o.OrderItems)
                        Console.WriteLine(string.Format("\t\t\t {0} x {1}", item.Qty, item.Title));
                }

                Console.WriteLine("> Press any key to continue");
                Console.ReadLine();
            }
        }
    }
}
