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

using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableStorage.Model
{

    // Entities map to C# objects using a custom class derived from TableEntity. 
    public class CustomerEntity : TableEntity
    {

        public CustomerEntity(string lastName, string firstName)
        {
            // The following code defines an entity class that uses the customer's first name as the row key and last name as the partition key.
            // Together, an entity's partition and row key uniquely identify the entity in the table. 
            // Entities with the same partition key can be queried faster than those with different partition keys, 
            // but using diverse partition keys allows for greater parallel operation scalability. 

            this.PartitionKey = lastName;
            this.RowKey = firstName;
        }

        // Your entity type must expose a parameter-less constructor.
        public CustomerEntity() { }

        //For any property that should be stored in the table service, the property must be a public property of a supported type that exposes both get and set.        
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}
