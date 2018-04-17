using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Data;
using NSAPConnector;
using Newtonsoft.Json.Linq;
using SAP.Middleware;
using SAP.Middleware.Connector;

namespace Frends.SAPConnector
{
    public static class SAP
    {
        public class Parameter
        {
            public String Name { get; set; }
            public String Value { get; set; }
        }

        public enum ReadTableRFC { BBP_RFC_READ_TABLE, RFC_READ_TABLE }

        public class Field
        {
            public String FieldName { get; set; }
        }

        public class InputBAPI
        {
            [PasswordPropertyText]
            [DefaultValue("\"ASHOST=sapserver01;SYSNR=00;CLIENT=000;LANG=EN;USER=SAPUSER;PASSWD=****;\"")]
            public string ConnectionString { get; set; }

            [DefaultValue("BAPI_RFC_READ_TABLE")]
            public string BAPIName { get; set; }

            public Parameter[] Parameters { get; set; }
        }

        public class InputQuery
        {
            [PasswordPropertyText]
            [DefaultValue("\"ASHOST=sapserver01;SYSNR=00;CLIENT=000;LANG=EN;USER=SAPUSER;PASSWD=****;\"")]
            public string ConnectionString { get; set; }

            [DefaultValue("MARA")]
            public string TableName { get; set; }

            public Parameter[] Parameters { get; set; }

            [DefaultValue("MATNR")]
            public String Fields { get; set; }

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
            /// Command timeout in seconds
            /// </summary>
            [DefaultValue(60)]
            public ReadTableRFC ReadTableTargetRFC { get; set; }
        }


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

        public static dynamic ExecuteQuery(InputQuery query, Options options)
        {
            List<string> rows = new List<string>();

            DataTable results = new DataTable("DATA");

            string[] field_names = query.Fields.Split(",".ToCharArray());
            RfcConfigParameters connectionParams = new RfcConfigParameters();
            String[] connectionStringArray = query.ConnectionString.Split(';');

           
            foreach(String configEntry in connectionStringArray)
            {
                connectionParams.Add(configEntry.TrimEnd().TrimStart().Split('=')[0], configEntry.TrimEnd().TrimStart().Split('=')[1]);
            }

            RfcDestination destination = RfcDestinationManager.GetDestination(connectionParams);
            IRfcFunction readTable;
            try
            {
                switch(options.ReadTableTargetRFC)
                {
                    case ReadTableRFC.BBP_RFC_READ_TABLE:
                        readTable = destination.Repository.CreateFunction("BBP_RFC_READ_TABLE");
                        break;
                    case ReadTableRFC.RFC_READ_TABLE:
                        readTable = destination.Repository.CreateFunction("RFC_READ_TABLE");
                        break;
                    default:
                        readTable = destination.Repository.CreateFunction("BBP_RFC_READ_TABLE");
                        break;
                }
                
            }
            catch (RfcBaseException ex)
            {
                throw (ex);
            }
            
            readTable.SetValue("query_table", query.TableName);
            readTable.SetValue("delimiter", "~");
            IRfcTable t = readTable.GetTable("DATA");
            t.Clear();
            t = readTable.GetTable("FIELDS");
            t.Clear();

            if (field_names.Length > 0)
            {
                t.Append(field_names.Length);
                int i = 0;
                foreach (string n in field_names)
                {
                    t.CurrentIndex = i++;
                    t.SetValue(0, n);
                }
            }

            t = readTable.GetTable("OPTIONS");
            t.Clear();
            t.Append(1);
            t.CurrentIndex = 0;
            t.SetValue(0, query.Filter);
            readTable.Invoke(destination);
            t = readTable.GetTable("DATA");

            JArray dataRows = new JArray();
            int a = t.Count;
            rows = new List<string>();

            for (int x = 0; x < t.RowCount; x++)
            {
                JObject dataObject = new JObject();

                t.CurrentIndex = x;
                String[] columnValues = t.GetString(0).Split('~');

                for(int i = 0; i < columnValues.Length; i++)
                {
                    dataObject.Add(field_names[i],columnValues[i]);
                }
                dataRows.Add(dataObject);
            }

            return JToken.FromObject(dataRows);
        }
    }
}
