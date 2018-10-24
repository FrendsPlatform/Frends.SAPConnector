using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using NSAPConnector;
using Newtonsoft.Json.Linq;
using SAP.Middleware.Connector;
using System.Text.RegularExpressions;

#pragma warning disable 1591

namespace Frends.SAPConnector
{
    public static class SAP
    {
        private static readonly int FilterMaxLen = 72;

        public class Parameter
        {
            public String Name { get; set; }
            public String Value { get; set; }
        }

        /// <summary>
        /// RFC function to use for reading table
        /// </summary>
        public enum ReadTableRFC { BBP_RFC_READ_TABLE, RFC_READ_TABLE }
        public enum InputType { PARAMETERS, JSON }

        public class ExecuteFunctionInput
        {
            public ConnectionString ConnectionString { get; set; }

            public InputType InputType { get; set; }

            // Function calls in JSON format. Frends cannot use
            // recursive inputs, therefore JSON is used and deserialized.
            [DisplayFormat(DataFormatString = "Json")]
            [UIHint(nameof(InputType), "", InputType.JSON)]
            public string InputFunctions { get; set; }

            [UIHint(nameof(InputType), "", InputType.PARAMETERS)]
            public SimpleFunctionInput SimpleInput { get; set; }
        }

        public class ConnectionString
        {
            [PasswordPropertyText]
            [DefaultValue("\"ASHOST=sapserver01;SYSNR=00;CLIENT=000;LANG=EN;USER=SAPUSER;PASSWD=****;\"")]
            public string Value { get; set; }
        }

        public class InputBAPI
        {
            [PasswordPropertyText]
            [DefaultValue("\"ASHOST=sapserver01;SYSNR=00;CLIENT=000;LANG=EN;USER=SAPUSER;PASSWD=****;\"")]
            public string ConnectionString { get; set; }

            [DisplayFormat(DataFormatString = "Text")]
            [DefaultValue("BAPI_RFC_READ_TABLE")]
            public string BAPIName { get; set; }

            public Parameter[] Parameters { get; set; }
        }

        public class InputQuery
        {
            [PasswordPropertyText]
            [DefaultValue("\"ASHOST=sapserver01;SYSNR=00;CLIENT=000;LANG=EN;USER=SAPUSER;PASSWD=****;\"")]
            public string ConnectionString { get; set; }

            [DisplayFormat(DataFormatString = "Text")]
            [DefaultValue("MARA")]
            public string TableName { get; set; }

            [DisplayFormat(DataFormatString = "Text")]
            [DefaultValue("MATNR")]
            public String Fields { get; set; }

            [DisplayFormat(DataFormatString = "Text")]
            [DefaultValue("MTART EQ 'HAWA'")]
            public String Filter { get; set; }
        }

        public class Options
        {
            /// <summary>
            /// Command timeout in seconds
            /// </summary>
            [DefaultValue(60)]
            public int CommandTimeoutSeconds { get; set; }

            /// <summary>
            /// RFC to use for reading table.
            /// </summary>
            [DefaultValue(ReadTableRFC.RFC_READ_TABLE)]
            public ReadTableRFC ReadTableTargetRFC { get; set; }
        }

        /// <summary>
        /// Execute BAPI that takes import parameters and return results as tables.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static dynamic ExecuteBAPI(InputBAPI Input)
        {
            DataSet resultDataSet;
            Dictionary<String, String> connectionParams = new Dictionary<string, string>();
            String[] connectionStringArray = Input.ConnectionString.Split(';');

            foreach (String configEntry in connectionStringArray)
            {
                connectionParams.Add(configEntry.TrimEnd().TrimStart().Split('=')[0], configEntry.TrimEnd().TrimStart().Split('=')[1]);
            }

            using (var connection = new SapConnection(connectionParams))
            {

                connection.Open();

                var command = new SapCommand(Input.BAPIName, connection);

                foreach (Parameter param in Input.Parameters)
                {
                    command.Parameters.Add(param.Name, param.Value);
                }

                resultDataSet = command.ExecuteDataSet();
            }

            return JToken.FromObject(resultDataSet);
        }

        /// <summary>
        /// Execute SAP RFC-function.
        /// </summary>
        /// <param name="function">Name of the SAP function</param>
        /// <returns>JToken dictionary of export parameter or table values returned by SAP function.</returns>
        public static dynamic ExecuteFunction(ExecuteFunctionInput taskInput)
        {
            var connectionParams = ConnectionStringToDictionary(taskInput.ConnectionString.Value);
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
                    session.StartSession();
                    
                    foreach (var f in input.Functions)
                    {
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

                        var tables = GetTableNames(sapFunction);
                        var exportParams = GetExportParameters(sapFunction);

                        var tablesAsJObject = new JObject();

                        foreach (var table in tables)
                        {
                            var rfcTable = sapFunction.GetTable(table);
                            tablesAsJObject.Add(table, JToken.FromObject(RfcTableToDataTable(rfcTable, table)));
                        }

                        foreach (var parameter in exportParams)
                        {
                            tablesAsJObject.Add(parameter.Key, parameter.Value);
                        }

                        returnvalues.Add(f.Name, tablesAsJObject);
                    }

                    session.EndSession();
                }
            }

            return returnvalues;
        }

        /// <summary>
        /// Query SAP table. TODO: Timeout value in options input does nothing.
        /// </summary>
        /// <param name="query">Query parameters</param>
        /// <param name="options">Connection options</param>
        /// <returns>JToken containing data returned by table query</returns>
        public static dynamic ExecuteQuery(InputQuery query, Options options)
        {
            var dataRows = new JArray();
            var fieldNames = query.Fields.Split(',');
            IRfcFunction readerRfc;

            var connectionParams = new RfcConfigParameters();

            // Read connection parameters from task input
            try
            {
                var connectionStringArray = query.ConnectionString.Split(';');

                foreach (var configEntry in connectionStringArray)
                {
                    connectionParams.Add(configEntry.TrimEnd().TrimStart().Split('=')[0], configEntry.TrimEnd().TrimStart().Split('=')[1]);
                }
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
                    readerRfc.SetValue("DELIMITER", "~");

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
                        var columnValues = exportData.GetString(0).Split('~');

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
            foreach (var param in sapConnStr.Split(';'))
            {
                connectionParams.Add(param.TrimEnd().TrimStart().Split('=')[0], param.TrimEnd().TrimStart().Split('=')[1]);
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
    }
}
