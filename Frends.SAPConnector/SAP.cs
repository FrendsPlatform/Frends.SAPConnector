using System;
using System.Collections.Generic;
using System.Data;
using NSAPConnector;
using Newtonsoft.Json.Linq;
using SAP.Middleware.Connector;
using System.Text.RegularExpressions;
using FRENDS.SAPConnector;
using System.Threading;
using System.Linq;


#pragma warning disable 1591

namespace Frends.SAPConnector
{
    public static class SAP
    {
        private static readonly int FilterMaxLen = 72;

        /// <summary>
        /// Execute SAP RFC-function.
        /// </summary>
        /// <param name="taskInput">Task input parameters</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns>JToken dictionary of export parameter or table values returned by SAP function. See: https://github.com/FrendsPlatform/Frends.SAPConnector#ExecuteFunction </returns>
        public static dynamic ExecuteFunction(ExecuteFunctionInput taskInput, CancellationToken cancellationToken)
        {

            Dictionary<String, String> connectionParams;

            // Read connection parameters from task input
            try
            {
                connectionParams = ConnectionStringToDictionary(taskInput.ConnectionString);

            }
            catch (Exception e)
            {
                throw new Exception($"Failed reading parameters from connection string: {e.Message}", e);
            }

            var returnvalues = new JObject();
            FunctionInput input;
            IRfcFunction sapFunction;

            if (taskInput.InputType == InputType.JSON)
            {
                try
                {
                    input = new FunctionInput(taskInput.InputFunctions);
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to parse input JSON", e);
                }
            }
            else if (taskInput.InputType == InputType.PARAMETERS)
            {
                input = new FunctionInput();
                var structures = new List<Structure>();
                foreach (var s in taskInput.SimpleInput.Functions)
                {
                    structures.Add(new Structure
                    {
                        Name = s.Name,
                        Fields = s.Fields
                    });
                }
                input.Functions = structures.ToArray();
            }
            else
            {
                throw new Exception("Invalid input type!");
            }

            using (var connection = new SapConnection(connectionParams))
            {
                connection.Open();

                var repo = connection.Destination.Repository;

                using (var session = new SapSession(connection))
                {
                    try
                    {
                        session.StartSession();

                        foreach (var f in input.Functions)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            try
                            {
                                sapFunction = repo.CreateFunction(f.Name);
                            }
                            catch (Exception e)
                            {
                                throw new Exception($"Failed to create function: {e.Message}", e);
                            }

                            try
                            {
                                f.PopulateRfcDataContainer(sapFunction);
                            }
                            catch (Exception e)
                            {
                                throw new Exception($"Failed to populate function input structure: {e.Message}", e);
                            }

                            try
                            {
                                sapFunction.Invoke(connection.Destination);
                            }
                            catch (Exception e)
                            {
                                throw new Exception($"Invoking function failed: {e.Message}", e);
                            }

                            try
                            {
                                var tables = GetTableNames(sapFunction);
                                var exportParams = GetExportParameters(sapFunction);

                                var tablesAsJObject = new JObject();

                                foreach (var table in tables)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    var rfcTable = sapFunction.GetTable(table);
                                    tablesAsJObject.Add(table, JToken.FromObject(RfcTableToDataTable(rfcTable, table)));
                                }

                                foreach (var parameter in exportParams)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    tablesAsJObject.Add(parameter.Key, parameter.Value);
                                }

                                returnvalues.Add(f.Name, tablesAsJObject);
                            }

                            catch (Exception e)
                            {
                                session.EndSession();
                                throw new Exception($"Failed to read return values: {e.Message}", e);
                            }
                        }

                    }
                    finally
                    {
                        session.EndSession();
                    }
                }
            }

            return returnvalues;
        }

        /// <summary>
        /// Query SAP table.
        /// </summary>
        /// <param name="query">Query parameters</param>
        /// <param name="options">Connection options</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns>JToken containing data returned by table query. See: https://github.com/FrendsPlatform/Frends.SAPConnector#ExecuteQuery </returns>
        public static dynamic ExecuteQuery(InputQuery query, Options options, CancellationToken cancellationToken)
        {
            char delimiter;
            if (query.Delimiter.Length == 1)
            {
                delimiter = query.Delimiter[0];
            }
            else throw new Exception("Delimiter should be single character!");

            var dataRows = new JArray();
            var fieldNames = query.Fields.Split(',');
            IRfcFunction readerRfc;

            Dictionary<String, String> connectionParams;

            // Read connection parameters from task input
            try
            {
                connectionParams = ConnectionStringToDictionary(query.ConnectionString);

            }
            catch (Exception e)
            {
                throw new Exception($"Failed reading parameters from connection string: {e.Message}", e);
            }

            using (var connection = new SapConnection(connectionParams))
            {
                connection.Open();

                try
                {
                    switch (options.ReadTableTargetRFC)
                    {
                        case ReadTableRFC.BBP_RFC_READ_TABLE:
                            readerRfc = connection.Destination.Repository.CreateFunction("BBP_RFC_READ_TABLE");
                            break;
                        case ReadTableRFC.RFC_READ_TABLE:
                            readerRfc = connection.Destination.Repository.CreateFunction("RFC_READ_TABLE");
                            break;
                        case ReadTableRFC.CUSTOM_FUNCTION:
                            readerRfc = connection.Destination.Repository.CreateFunction(options.CustomFuntionName);
                            break;
                        default:
                            readerRfc = connection.Destination.Repository.CreateFunction("RFC_READ_TABLE");
                            break;
                    }

                }
                catch (RfcBaseException ex)
                {
                    throw new Exception("Failed to fetch reader function metadata.", ex);
                }

                // Populate required import tables
                try
                {
                    readerRfc.SetValue("QUERY_TABLE", query.TableName);
                    readerRfc.SetValue("DELIMITER", delimiter);

                    var fieldsTable = readerRfc.GetTable("FIELDS");

                    foreach (var field in fieldNames)
                    {
                        var fieldsRow = fieldsTable.Metadata.LineType.CreateStructure();
                        fieldsRow.SetValue(0, field.Trim());
                        fieldsTable.Append(fieldsRow);
                    }

                    var optionsTable = readerRfc.GetTable("OPTIONS");

                    foreach (var chunk in SplitSapFilterString(query.Filter))
                    {

                        var optionsRow = optionsTable.Metadata.LineType.CreateStructure();
                        optionsRow.SetValue("TEXT", chunk);
                        optionsTable.Append(optionsRow);
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to set input values: {e.Message}", e);
                }

                try
                {
                    readerRfc.Invoke(connection.Destination);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to invoke SAP function: {e.Message}", e);
                }

                // read results from DATA table, they are on rows delimited
                // by configured delimiter symbol
                try
                {
                    var exportData = readerRfc.GetTable("DATA");

                    for (var i = 0; i < exportData.RowCount; i++)
                    {
                        var dataObject = new JObject();

                        exportData.CurrentIndex = i;
                        var columnValues = exportData.GetString(0).Split(delimiter);

                        for (var j = 0; j < columnValues.Length; j++)
                        {
                            dataObject.Add(fieldNames[j], columnValues[j]);
                        }

                        dataRows.Add(dataObject);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to read return values: {e.Message}", e);
                }
            }

            return JToken.FromObject(dataRows);
        }

        /// <summary>
        /// Create datatable with same structure that RfcTable and populate
        /// it with data from RfcTable
        /// </summary>
        /// <param name="rfcTable"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static DataTable RfcTableToDataTable(IRfcTable rfcTable, string name)
        {
            var metadata = rfcTable.Metadata.LineType;
            var datatable = new DataTable(name);

            for (var i = 0; i < metadata.FieldCount; i++)
            {
                datatable.Columns.Add(
                    metadata[i].Name,
                    SapTypeToDotNetType(metadata[i].DataType.ToString(), metadata[i].NucLength)
                );
            }

            for (var i = 0; i < rfcTable.RowCount; i++)
            {
                rfcTable.CurrentIndex = i;
                var row = datatable.NewRow();

                foreach (DataColumn column in datatable.Columns)
                {
                    if (column.DataType == typeof(byte[]))
                        row[column.ColumnName] = rfcTable.CurrentRow[column.ColumnName].GetByteArray();
                    else if (column.DataType == typeof(int))
                        row[column.ColumnName] = rfcTable.CurrentRow[column.ColumnName].GetInt();
                    else if (column.DataType == typeof(byte))
                        row[column.ColumnName] = rfcTable.CurrentRow[column.ColumnName].GetByte();
                    else if (column.DataType == typeof(short))
                        row[column.ColumnName] = rfcTable.CurrentRow[column.ColumnName].GetShort();
                    else if (column.DataType == typeof(double))
                        row[column.ColumnName] = rfcTable.CurrentRow[column.ColumnName].GetDouble();
                    else if (column.DataType == typeof(long))
                        row[column.ColumnName] = rfcTable.CurrentRow[column.ColumnName].GetLong();
                    else
                        row[column.ColumnName] = rfcTable.CurrentRow[column.ColumnName].GetString();
                }

                datatable.Rows.Add(row);
            }

            return datatable;
        }

        private static Dictionary<string, string> GetExportParameters(IRfcFunction function)
        {
            var regex = new Regex(@"EXPORT (\w+):");
            var exportParams = new Dictionary<string, string>();

            foreach (Match match in regex.Matches(function.Metadata.ToString()))
            {
                var name = match.Groups[1].Value;
                var value = function.GetValue(name).ToString();
                exportParams.Add(name, value);
            }
            return exportParams;
        }

        private static IEnumerable<string> GetTableNames(IRfcFunction function)
        {
            var regex = new Regex(@"TABLES (\w+):");
            var matches = regex.Matches(function.Metadata.ToString());
            
            foreach (Match match in matches)
            {
                yield return match.Groups[1].Value;
            }
        }

        private static Dictionary<string, object> GetTables(IRfcFunction function)
        {
            var regex = new Regex(@"TABLES (\w+):");
            var exportParams = new Dictionary<string, object>();

            foreach (Match match in regex.Matches(function.Metadata.ToString()))
            {
                var name = match.Groups[1].Value;
                var value = function.GetValue(name).ToString();
                exportParams.Add(name, value);
            }
            return exportParams;
        }

        private static Dictionary<string, string> ConnectionStringToDictionary(string sapConnStr)
        {
            var connectionParams = new Dictionary<string, string>();
            foreach (var param in sapConnStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().Split('=').Select(s2 => s2.Trim()).ToArray()))
            {
                connectionParams.Add(param[0], param[1]);
            }
            return connectionParams;
        }

        private static Type SapTypeToDotNetType(string sapType, int typeLength)
        {
            switch (sapType)
            {
                case "BYTE":
                    return typeof(byte[]);

                case "INT":
                    return typeof(int);

                case "INT1":
                    return typeof(byte);

                case "INT2":
                    return typeof(short);

                case "FLOAT":
                    return typeof(double);

                case "NUM":

                    if (typeLength <= 9)
                    {
                        return typeof(int);
                    }

                    if (typeLength <= 19)
                    {
                        return typeof(long);
                    }

                    return typeof(string);

                default:
                    return typeof(string);
            }
        }

        /// <summary>
        /// One filter row in RFC_READ_TABLE can hold maximum of 72 characters.
        /// Longer queries need to be split.
        /// Strings can only be split between words.
        /// </summary>
        /// <param name="filter">SAP query filter (WHERE condition)</param>
        /// <returns>Array of strings of max 72 characters each</returns>
        private static IEnumerable<string> SplitSapFilterString(string filter)
        {
            // replace newline with whitespace
            var filterWithoutNewlines = filter.Replace("\n", " ");

            if (filterWithoutNewlines.Length <= FilterMaxLen)
            {
                yield return filterWithoutNewlines;
            }
            else
            {
                // find whitespace to split at
                int lastWhitespace = filterWithoutNewlines.Substring(0, FilterMaxLen + 1).LastIndexOf(' ');

                if (lastWhitespace < 0)
                    throw new Exception($"Query parameter length should not exceed {FilterMaxLen.ToString()} parameters");

                yield return filterWithoutNewlines.Substring(0, lastWhitespace);

                // Whitespace can be dropped
                foreach (var chunk in SplitSapFilterString(filterWithoutNewlines.Substring(lastWhitespace + 1)))
                    yield return chunk;
            }
        }

        /// <summary>
        /// Exposes methods of RfcRepository class of SAP Connector for Microsoft .NET 3.0. Usually this task is not needed for other than debugging purposes. See: https://github.com/FrendsPlatform/Frends.SAPConnector#RfcRepositoryModifier SAP documentation for Repository: https://help.sap.com/doc/saphelp_crm700_ehp02/7.0.2.17/en-US/0f/8635d6362c4123a37d39b2c8e652b5/frameset.htm
        /// </summary>
        /// <param name="input">Query parameters</param>
        /// <param name="cancellationToken">Cancellation Tokens</param>
        /// <returns>Dynamic object or NULL containing data returned by selected function. </returns>
        public static dynamic RfcRepositoryModifier(RfcRepositoryInput input, CancellationToken cancellationToken)
        {

            Dictionary<String, String> connectionParams = new Dictionary<string, string>();

            // Read connection parameters from task input
            try
            {
                connectionParams = ConnectionStringToDictionary(input.ConnectionString);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed reading parameters from connection string: {e.Message}", e);
            }

            var returnValues = new Object();

            using (var connection = new SapConnection(connectionParams))
            {
                cancellationToken.ThrowIfCancellationRequested();

                connection.Open();
                var repo = connection.Destination.Repository;

                using (var session = new SapSession(connection))
                {
                    try
                    {

                        session.StartSession();
                        cancellationToken.ThrowIfCancellationRequested();

                        switch (input.function)
                        {
                            case RfcRepositoryModifierFunctions.ClearAbapObjectMetadata:
                                repo.ClearAbapObjectMetadata();
                                break;
                            case RfcRepositoryModifierFunctions.ClearAllMetadata:
                                repo.ClearAllMetadata();
                                break;
                            case RfcRepositoryModifierFunctions.ClearFunctionMetadata:
                                repo.ClearFunctionMetadata();
                                break;
                            case RfcRepositoryModifierFunctions.ClearTableMetadata:
                                repo.ClearTableMetadata();
                                break;
                            case RfcRepositoryModifierFunctions.GetFunctionMetadata:
                                returnValues = repo.GetFunctionMetadata(input.Name);
                                break;
                            case RfcRepositoryModifierFunctions.CreateFunction:
                                returnValues = repo.CreateFunction(input.Name);
                                break;
                            case RfcRepositoryModifierFunctions.GetAbapObjectMetadata:
                                returnValues = repo.GetAbapObjectMetadata(input.Name);
                                break;
                            case RfcRepositoryModifierFunctions.GetStructureMetadata:
                                returnValues = repo.GetStructureMetadata(input.Name);
                                break;
                            case RfcRepositoryModifierFunctions.GetTableMetadata:
                                returnValues = repo.GetTableMetadata(input.Name);
                                break;
                            case RfcRepositoryModifierFunctions.RemoveAbapObjectMetadata:
                                repo.RemoveAbapObjectMetadata(input.Name);
                                break;
                            case RfcRepositoryModifierFunctions.RemoveFunctionMetadata:
                                repo.RemoveFunctionMetadata(input.Name);
                                break;
                            case RfcRepositoryModifierFunctions.RemoveStructureMetadata:
                                repo.RemoveStructureMetadata(input.Name);
                                break;
                            case RfcRepositoryModifierFunctions.RemoveTableMetadata:
                                repo.RemoveTableMetadata(input.Name);
                                break;
                            default:
                                returnValues = "Unknown/Not implemented function!";
                                break;
                        }
                    }
                    finally
                    {
                        session.EndSession();
                    }

                }
            }
            return returnValues;
        }
    }
}
