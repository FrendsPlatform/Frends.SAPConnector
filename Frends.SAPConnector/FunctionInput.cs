using Newtonsoft.Json;
using SAP.Middleware.Connector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable 1591


namespace Frends.SAPConnector
{
    public interface ISapStructure
    {
        void PopulateRfcDataContainer(IRfcDataContainer container);
        Field[] Fields { get; set; }
        string Name { get; set; }
    }

    public class SimpleFunctionInput
    {
        public SimpleStructure[] Functions { get; set; }
    }

    public class FunctionInput
    {
        public Structure[] Functions { get; set; }

        public FunctionInput() { }

        public FunctionInput(string json)
        {
            Functions = JsonConvert.DeserializeObject<Structure[]>(json);
        }
    }

    public class SimpleStructure : ISapStructure
    {
        public string Name { get; set; }
        public Field[] Fields { get; set; }

        public void PopulateRfcDataContainer(IRfcDataContainer container)
        {
            if (Fields != null)
            {
                foreach (var field in Fields)
                {
                    container.SetValue(field.Name, field.Value);
                }
            }
        }
    }

    public class Structure : ISapStructure
    {
        public string Name { get; set; }
        public Table[] Tables { get; set; }
        public Structure[] Structures { get; set; }
        public Field[] Fields { get; set; }

        /// <summary>
        /// Recursively populate RfcDataContainer
        /// </summary>
        /// <param name="container">Reference to RfcFunction object</param>
        public void PopulateRfcDataContainer(IRfcDataContainer container)
        {
            if (Fields != null)
            {
                foreach (var field in Fields)
                {
                    container.SetValue(field.Name, field.Value);
                }
            }

            if (Tables != null)
            {
                foreach (var table in Tables)
                {
                    var t = container.GetTable(table.Name);

                    foreach (var row in table.Rows)
                    {
                        var tablerow = t.Metadata.LineType.CreateStructure();
                        row.PopulateRfcDataContainer(tablerow);
                        t.Append(tablerow);
                    }
                }
            }

            if (Structures != null)
            {
                foreach (var structure in Structures)
                {
                    var s = container.GetStructure(structure.Name);

                    structure.PopulateRfcDataContainer(s);
                }
            }
        }
    }

    public class Table
    {
        public string Name { get; set; }
        public Structure[] Rows { get; set; }
    }

    public class Field
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
