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

Tasks
=====

## ExecuteFunction
Frends.SAPConnector.ExecuteFunction task to xecute SAP RFC-function. Returns: JToken dictionary of export parameter or table values returned by SAP function.

Input:

| Property        | Type     | Description                       | Example                               |
|-----------------|----------|-----------------------------------|---------------------------------------|
| Connection string |         |          |                 |                |
 | Input type|                 |                |
|      |          |                 |                |
|         |          |                 |                |
|         |          |                 |                |


PARAMETERS Name field pair

JSON


Options:

| Property                                    | Type           | Description                                    | Example                   |
|---------------------------------------------|----------------|------------------------------------------------|---------------------------|
| UseGivenUserCredentialsForRemoteConnections | bool           | If set, allows you to give the user credentials to use to read files on remote hosts. If not set, the agent service user credentials will be used. Note: For deleting directories on the local machine, the agent service user credentials will always be used, even if this option is set.| |
| UserName                                    | string         | Needs to be of format `domain\username`        | `example\Admin`           |
| Password                                    | string         |                                                |                           |

Result:

| Property        | Type     | Description                 |
|-----------------|----------|-----------------------------|
| ContentBytes    | byte[]   |                             |
| Path            | string   | Full path for the read file |
| SizeInMegaBytes | double   |                             |
| CreationTime    | DateTime |                             |
| LastWriteTime   | DateTime |                             |


## ExecuteQuery
Frends.SAPConnector.ExecuteQuery task to query SAP table. TODO: Timeout value in options input does nothing. Returns: JToken containing data returned by table query

Input:

| Property        | Type     | Description                  | Example                 |
|-----------------|----------|------------------------------|---------------------------|
| Path            | string   | Full path to the file to be written to. | `c:\temp\foo.txt` `c:/temp/foo.txt` |
| Content         | string   | | |

Options:

| Property                                    | Type           | Description                                    | Example                   |
|---------------------------------------------|----------------|------------------------------------------------|---------------------------|
| UseGivenUserCredentialsForRemoteConnections | bool           | If set, allows you to give the user credentials to use to write files on remote hosts. If not set, the agent service user credentials will be used. Note: For deleting directories on the local machine, the agent service user credentials will always be used, even if this option is set.| |
| UserName                                    | string         | Needs to be of format `domain\username`        | `example\Admin`           |
| Password                                    | string         | | |
| FileEncoding                                | Enum           | Encoding for the content. By selecting 'Other' you can use any encoding. | |
| EncodingInString                            | string         | This should be filled if the FileEncoding choice is 'Other' A partial list of possible encodings: https://en.wikipedia.org/wiki/Windows_code_page#List | `iso-8859-1` |
| EnableBom                                   | bool           | Visible if UTF-8 is used as the option for FileEncoding. | |
| WriteBehaviour                              | Enum{Append,Overwrite,Throw} | Determines how the File.Write works when the destination file already exists | |

Result: 

| Property        | Type   | Description                 |
|-----------------|--------|-----------------------------|
| Path            | string | Full path to the written file |
| SizeInMegaBytes | double |                             |


License
=======

This project is licensed under the MIT License - see the LICENSE file for details

Building
========

Clone a copy of the repo

`git clone https://github.com/FrendsPlatform/Frends.File.git`

Restore dependencies

`nuget restore frends.file`

Rebuild the project

Run Tests with nunit3. Tests can be found under

`Frends.File.Tests\bin\Release\Frends.File.Tests.dll`

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
