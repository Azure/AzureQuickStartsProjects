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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSQLEF.DataModel
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public string Title { get; set; }
        public int Qty { get; set; }

        // You’ll notice that we’re making thisnavigation properties virtual. 
        // This enables the Lazy Loading feature of Entity Framework. 
        // Lazy Loading means that the contents of these properties will be automatically loaded from the database when you try to access them.
        public virtual Order Order { get; set; }
    }
}
