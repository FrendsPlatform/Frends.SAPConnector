# Frends.SAPConnector
FRENDS sap database tasks
- [Installing](#installing)
- [Tasks](#tasks)
	- [ExecuteFunction](#ExecuteFunction)
    - [ExecuteQuery](#ExecuteQuery)
- [License](#license)
- [Building](#building)
- [Contributing](#contributing)   

Installing
==========
You can install the task via FRENDS UI Task view, by searching for packages. You can also download the latest NuGet package from https://www.myget.org/feed/frends/package/nuget/Frends.File and import it manually via the Task view.

These task requires the Microsoft C++ Runtime DLLs version 10.0 (contained in Microsoft Visual C++ 2010 Redistributables), so they must be installed in advance. These task will also only work with 64 bit processor.

These task relies on SAP Connector for Microsoft .NET 3.0 https://support.sap.com/en/product/connectors/msnet.html Support for connector will end at December 31, 2020.

Tasks
=====

## ExecuteFunction
Frends.SAPConnector.SAP.ExecuteFunction task to execute SAP RFC-function. Returns: JToken dictionary of export parameter or table values returned by SAP function.

Input:

| Property        | Type     | Description                       | Example                               |
|-----------------|----------|-----------------------------------|---------------------------------------|
| Connection string | string | Connection String to be used to connect to the SAP.   |   ASHOST=sapserver01;SYSNR=00;CLIENT=000;LANG=EN;USER=SAPUSER;PASSWD=*;   |   
 | Input type | enum | Defines if PARAMETERS or JSON is used.    |    | 
| PARAMETERS |          |                 |                |
| JSON | string | Function call parameters in JSON format. | See bellow.  |


### Parameters 

Input type PARAMETERS can be used when RFC function takes simple values as parameter. Parameters are defined by giving parameter name and input value for it. 


### JSON 

Input type JSON can be used when RFC function takes arbitary data as parameter. Frends cannot use recursive inputs, 
therefore JSON is used and deserialized.

JSON follows format that JSNO.Net serialize uses. Format of the configuration ias following: On a top level it is Array of objects. Each object is function being called. Each object has atleast a key "name" that defines function being called. If function takes parameters they are defined too.

Parameters are same as in SAP and they can be chained in any way.


Key "Field" defines array of fieleds. 
```json
"Fields": [
   {
      "Name":"PARTNER_GUID",
      "Value":"16c0f469-aef0-4f22-ae78-20c964d74b9d"
   }
```
Kay "Tables" defines that sap has table, in JSON it key "Name" defining table name and Key "Rows" defines array of rows. Rows them self can contain anything, e.g. fields.
```json
"Tables": [
   {
      "Name":"DATA",
      "Rows":[
         {
            "Fields":[
               {
                  "Name":"PARTNER_GUID",
                  "Value":"16c0f469-aef0-4f22-ae78-20c964d74b9d"
               }
            ]
         }
      }
   ]
```

Key "Structures" defines object that have atleas name.
```json
"Structures":[
   {
      "Name":"OBJECT_INSTANCE",
      "Fields":[
         {
            "Name":"PARTNER_GUID",
            "Value":"{{#var.partnerguid}}"
         }
      ]
   }
```
Parameters can freevly chauined to, so they complete JSON could be:
```json
[
  {
    "Name": "CRM_PARTNER_SAVE",
    "Tables": [
      {
        "Name": "DATA",
        "Rows": [
          {
            "Structures": [
              {
                "Name": "HEADER",
                "Structures": [
                  {
                    "Name": "OBJECT_INSTANCE",
                    "Fields": [
                      {
                        "Name": "PARTNER_GUID",
                        "Value": "{{#var.partnerguid}}"
                      }
                    ]
                  }
                ],
                "Fields": [
                  {
                    "Name": "PARTNER_ID",
                    "Value": "1337"
                  }
                ]
              }
            ]
          }
        ]
      }
    ]
  },
  {
    "Name": "BAPI_TRANSACTION_COMMIT"
  }
]
```

### Result

Returns a JToken dictionary of export parameter or table values returned by SAP function.

## ExecuteQuery
Frends.SAPConnector.ExecuteQuery task to query SAP table.

Input:

| Property        | Type     | Description                       | Example                               |
|-----------------|----------|-----------------------------------|---------------------------------------|
| Connection string | string | Connection String to be used to connect to the SAP.   |   ASHOST=sapserver01;SYSNR=00;CLIENT=000;LANG=EN;USER=SAPUSER;PASSWD=*;   |   
 | TableName | enum | Table Name   | MARA | 
| Fields | string  |     |   MATNR    |
| Filter | string | Function call parameters in JSON format. | MTART EQ 'HAWA' |
| CommandTimeoutSeconds | Int | Command timeout in seconds | 60 |
| ReadTableTargetRFC | enum | RFC to use for reading table. | RFC_READ_TABLE |




License
=======

This project is licensed under the MIT License - see the LICENSE file for details

Building
========

Clone a copy of the repo

`git clone https://github.com/FrendsPlatform/Frends.SAPConnector.git`

Restore dependencies

`nuget restore frends.file`

Rebuild the project

Run Tests with nunit3. Tests can be found under

`Frends.File.Tests\bin\Release\Frends.SAPConnector.Tests.dll`

Create a nuget package

`nuget pack nuspec/Frends.File.nuspec`

Contributing
============
When contributing to this repository, please first discuss the change you wish to make via issue, email, or any other method with the owners of this repository before making a change.

1. Fork the repo on GitHub
2. Clone the project to your own machine
3. Commit changes to your own branch
4. Push your work back up to your fork
5. Submit a Pull request so that we can review your changes

NOTE: Be sure to merge the latest from "upstream" before making a pull request!
