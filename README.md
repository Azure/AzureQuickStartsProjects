Azure QuickStarts Projects
==========================

A collection of quickstart projects demonstrating core Microsoft Azure services and their APIs.

[Download from Visual Studio Extension Gallery](http://visualstudiogallery.msdn.microsoft.com/f56f321d-e2a7-4ee1-864e-5752617dbf1d)

# Development Environment

In order to contribute to the Azure QuickStarts you will need the following tools install on your machine.

+ [Visual Studio 2013](http://go.microsoft.com/?linkid=9832446&clcid=0x409)
+ [Visual Studio 2013 SDK](http://go.microsoft.com/?linkid=9832352)
+ [Azure SDK & Tools](http://go.microsoft.com/fwlink/p/?linkid=323510&clcid=0x409)
+ [SideWaffle Template Pack](http://visualstudiogallery.msdn.microsoft.com/a16c2d07-b2e1-4a25-87d9-194f04e7a698)
+ [NuGet](http://visualstudiogallery.msdn.microsoft.com/4ec1526c-4a8c-4a84-b702-b21a8f5293ca)
+ [Microsoft Azure Management Libraries](http://www.nuget.org/packages/Microsoft.WindowsAzure.Management.Libraries) <small>(MAML)</small>

# Getting Started

I'm glad that you're interested in providing a sample for the Azure QuickStarts, here are some quick notes to help you along your way.

<a name="what-is-a-sample"></a>
## What is a Sample?

A sample is meant to be the simplest possible example of the use of a service. Essentially the point is to exercise the API/SDK for a particular Service or if needed provide a link to a more advanced sample.

> **Example**
> 
> *Blob Storage Sample*
>
> + Create Storage Account
> + Create Container
> + Create Blob
> + Modify Blob Properties/Metadata
> + Read Blob 
> + Delete Blob
> + Delete Container
> + Delete Storage Account

## Where do I put things? <small>(aka Directory Structure)</small>

This provides a general reference as to where a sample should be placed. If in doubt see the [Documentation](http://azure.microsoft.com/en-us/documentation/) under Documentation by Service section.

### App Services
+ Samples for services which provide support an application accomplish a given task.

### Compute
+ Samples for services which provide an endpoint for an application.

### Data Services
+ Samples for services which provide storage for an application.

### Deployment and Management
+ Samples for Automating the creation and management of services using an API/SDK.
	
### Network Services
+ Samples for services which connect Compute Services using Network or DNS.

## How do I build a sample?

### App Services
+ Console Application which shows service functionality
+ Reference Online Documentation

### Compute
+ Reference Online Documentation

### Data Services
+ Console Application which shows service functionality
+ Reference Online Documentation

### Deployment and Management
+ Console Application which uses:
	+ [MAML](http://www.nuget.org/packages/Microsoft.WindowsAzure.Management.Libraries)
	+ [Azure Resource Management API](http://msdn.microsoft.com/en-us/library/azure/dn790568.aspx) (only if not defined in previous)
	+ [Service Management API](http://msdn.microsoft.com/en-us/library/azure/ee460799.aspx) (only if not defined in previous)

### Network Services
+ Reference Online Documentation

## How to prepare my sample for inclusion

### I can write a simple sample

1. Right click on the sample project, select **`Add > New Item > SideWaffle Project Template Files`**
1. Provide the **`_Definitions/_project.vstemplate.xml`** file with an appropriate Name and Description for the project template
	+ Be sure to include the `<WizardExtension>` xml snippet (below) as a child of the `<VSTemplate>` element.
		<pre>
		&lt;WizardExtension&gt;
	      &lt;Assembly&gt;ProjectWizard, Version=1.0.0.0, Culture=Neutral, PublicKeyToken=f30ae472f039a534&lt;/Assembly&gt;
	      &lt;FullClassName&gt;ProjectWizard.Wizard&lt;/FullClassName&gt;
	  	&lt;/WizardExtension&gt;
		</pre>
1. Update `_Preprocess.xml` as needed (generally not)
	+ Ensure the attribute `Path` in the `TemplateInfo` element describes where the Project Template should show in the **`File > New Project`** dialog
	+ Add a key value pair (replacement token) to the `Replacements` element if the project contains code. 
		+ Key value pairs can be removed from the `Replacements` element if it provides a link to some documentation.
1. Update sw-file-icon.png with the appropriate azure service logo which will be used for the template.

> **I need to create a Multi-Project Template!**
>
> Contact [Sayed Hashimi](https://twitter.com/sayedihashimi) for more information[.](https://microsoft.sharepoint.com/teams/aspnet/webprojd14/Shared%20Documents/Planning/templates/MultiProject%20Template.docx?web=1)

### I can't prepare a simple sample

If there isn't a public API surface which is [easy to explore via a QuickStart](#what-is-a-sample), it's still valuable to add a template which provides a redirect to an online sample (maybe from azure.microsoft.com).

This is valuable because the service will still be discover-able by Developers from within Visual Studio.

Follow the steps from above with one additional step:

1. Add a `WizardData` element as a child of the `<VSTemplate>`	element.

	<pre>
	&lt;WizardData&gt;
	  &lt;navigation&gt;
	    &lt;navigate path="[path-to-tutorial]" generateProject="false" /&gt;
	  &lt;/navigation&gt;
	&lt;/WizardData&gt;
	</pre>