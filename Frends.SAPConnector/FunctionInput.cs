using Newtonsoft.Json;
using SAP.Middleware.Connector;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

#pragma warning disable 1591


namespace Frends.SAPConnector
{
    public interface ISapStructure
    {
        void PopulateRfcDataContainer(IRfcDataContainer container);

        Field[] Fields { get; set; }

        string Name { get; set; }
    }

    /// <summary>
    /// Array of function to be called.
    /// </summary>
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

    /// <summary>
    /// Function to be called.
    /// </summary>
    public class SimpleStructure : ISapStructure
    {
        /// <summary>
        /// Name of RFC function.
        /// </summary>
        [DisplayName ("Function Name")]
        public string Name { get; set; }

        /// <summary>
        /// Array of parameter(s).
        /// </summary>
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
        /// <summary>
        /// Name of parameter.
        /// </summary>
        [DisplayName ("Parameter Name")]
        public string Name { get; set; }
        /// <summary>
        /// Value of parameter.
        /// </summary>
        public string Value { get; set; }
    }
}
