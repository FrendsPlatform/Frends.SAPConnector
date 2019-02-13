# Frends.SAPConnector
FRENDS SAP Tasks to execute RFC functions Querying tables.  

These Tasks require the Microsoft C++ Runtime DLLs version 10.0 (contained in Microsoft Visual C++ 2010 Redistributables), so they must be installed in advance. These Tasks will also only work with the 64-bit processor.

These Tasks rely on SAP Connector for Microsoft .NET 3.0 https://support.sap.com/en/product/connectors/msnet.html. Support for connector will end at December 31, 2020.

At the moment Tasks don't cache table metadata coming from the SAP. In some circumstances, this might slow the performance as metadata have to be fetched every time.

- [Installing](#installing)
- [Tasks](#tasks)
    - [ExecuteFunction](#ExecuteFunction)
    - [ExecuteQuery](#ExecuteQuery)
    - [RfcRepositoryModifier](#RfcRepositoryModifier)
- [License](#license)
- [Building](#building)
- [Contributing](#contributing)

Installing
==========

**NOTE: During development following Task is not available from the following feed:**

You can install the Task via FRENDS UI Task view, by searching for packages. You can also download the latest NuGet package from https://www.myget.org/feed/frends/package/nuget/Frends.SAPConnector. and import it manually via the Task view.

Tasks
=====

## ExecuteFunction
Frends.SAPConnector.SAP.ExecuteFunction Task to execute SAP RFC-function. Returns: JToken dictionary of export parameter or table values returned by SAP function.

### Input:

| Property        | Type     | Description                       | Example                               |
|-----------------|----------|-----------------------------------|---------------------------------------|
| Connection string | string | Connection String to be used to connect to the SAP.   |   `ASHOST=sapserver01;SYSNR=00;CLIENT=000;LANG=EN;USER=SAPUSER;PASSWD=*;IDLE_TIMEOUT=60;`   |
 | Input type | enum | Defines if PARAMETERS or JSON is used.    | `PARAMETERS`   |
| PARAMETERS | enum | Function call parameters in PARAMETERS format. | See below.  |
| JSON | string | Function call parameters in JSON format. | See below.  |

### Parameters

Input type PARAMETERS can be used when RFC function(s) takes simple values as parameters. Parameters are defined by giving parameter name and an input value for it. It is possible to define many function and functions can take many key-value pairs as parameters.

For example, DATE_GET_WEEK function can be used to calculate the week number for 2018-10-31 (October 31st, 2018) by the following configuration:

| Property        | Type     | Description                       | Example                               |
|-----------------|----------|-----------------------------------|---------------------------------------|
| Functions | array | Array Name of function(s) and parameter(s) to be called. |  |
| name | string | Name of function to be called. |   `DATE_GET_WEEK`   |
| Fields | array |     |  |
| Name | string |     | `DATE`   |
| Value | string |   | `20181031`   |

Example values would return object "DATE_GET_WEEK" with property "WEEK" with value "201844".

### JSON

Input type JSON can be used when RFC function takes arbitrary data as a parameter. Frends cannot use recursive inputs, therefore JSON is used and deserialized.

JSON follows format that JSON.Net uses for serializing objects. The format of the configuration is following:
On a top level is an Array of objects. Each object is a function being called. Each object has at least a key "name" that defines function being called. If the function takes parameters they are defined too. Parameters are defined with keys "Fields", "Tables" and "Structures"

Parameters are same as in SAP and they can be chained in any way.

Key "Field" defines an array of fields.

```json
"Fields": [
   {
      "Name":"PARTNER_GUID",
      "Value":"16c0f469-aef0-4f22-ae78-20c964d74b9d"
   }
```

Key "Tables" defines an array of tables. The array contains objects i.e. tables. In each object key, "Name" defines a table name and Key "Rows" defines an array of rows in the table. Rows them self can contain fields, structures, and tables.

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
      ]
   }
```

Key "Structures" defines an object that has at least name.

```json
"Structures":[
   {
      "Name":"OBJECT_INSTANCE",
      "Fields":[
         {
            "Name":"PARTNER_GUID",
            "Value":"16c0f469-aef0-4f22-ae78-20c964d74b9d"
         }
      ]
   }
```

Parameters can freely be chained to, so they complete JSON could be:

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
                        "Value": "16c0f469-aef0-4f22-ae78-20c964d74b9d"
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

** Example **

For example, DATE_GET_WEEK function can be used to calculate the week number for 2018-10-31 (October 31st, 2018) by the following configuration:

```json
[
  {
    "Name": "DATE_GET_WEEK",
        "Fields": [
          {
            "Name": "DATE",
            "Value": "20181031"
            }
        ]
  }
]
```

This would return object "DATE_GET_WEEK" with property "WEEK" with value "201844".

### Result

Returns a JToken dictionary of export parameter or table values returned by SAP function.

## ExecuteQuery
Frends.SAPConnector.ExecuteQuery Task to query SAP table.

### Input:

| Property        | Type     | Description                       | Example                               |
|-----------------|----------|-----------------------------------|---------------------------------------|
| Connection string | string | Connection String to be used to connect to the SAP.   |   `ASHOST=sapserver01;SYSNR=00;CLIENT=000;LANG=EN;USER=SAPUSER;PASSWD=*;IDLE_TIMEOUT=60;`   |
 | TableName | enum | Table Name   | `MARA` |
| Fields | string  |     |   `MATNR`   |
| Filter | string | Function call parameters in JSON format. | `MTART EQ 'HAWA'` |
| CommandTimeoutSeconds | Int | Command timeout in seconds | `60` |
| ReadTableTargetRFC | enum | Defines if RFC_READ_TABLE or BBP_RFC_READ_TABLE or some other function call is used to read SAP tables. CUSTOM_FUNCTION enables writing any function name. | `RFC_READ_TABLE` |
| Delimiter | string | Delimiter used in the table. | `~` |
| CustomFunctionName | string | When ReadTableTargetRFC is set to CUSTOM_FUNCTION this parameter is used to define name of any function. The function must take the same parameters and return similar data as RFC_READ_TABLE and BBP_RFC_READ_TABLE. A custom function can be used to overcome limitations of built-in functions. | `ZRFC_READ_TABLE` |

## RfcRepositoryModifier
Frends.SAPConnector.RfcRepositoryModifier Task to handle RfcRepository class from NCO 3.0 directly. These functions are rarely needed and they are documented at https://help.sap.com/doc/saphelp_crm700_ehp02/7.0.2.17/en-US/0f/8635d6362c4123a37d39b2c8e652b5/frameset.htm

License
=======

This project is licensed under the MIT License - see the LICENSE file for details.

Building
========

Clone a copy of the repo

`git clone https://github.com/FrendsPlatform/Frends.SAPConnector.git`

Restore dependencies

`nuget restore Frends.SAPConnect`

Rebuild the project, target x64 CPU architecture. *Any CPU or x86 won't work.*

Run Tests with nunit3. To make test work, *nunit3 have to use x64 processor.* This can be changed in Visual Studio under `Test` > `Test Settings` > `Default Processor Architechture` > `X64`. Tests can be found under:

`Frends.File.Tests\bin\Release\Frends.SAPConnector.Tests.dll`

Create a NuGet package

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
