using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Frends.SAPConnector;

#pragma warning disable 1591


namespace FRENDS.SAPConnector
{

    public enum RfcRepositoryModifierFunctions
    {
        ClearAbapObjectMetadata, ClearAllMetadata, ClearFunctionMetadata, ClearTableMetadata, CreateFunction,
        GetAbapObjectMetadata, GetFunctionMetadata, GetStructureMetadata, GetTableMetadata, RemoveAbapObjectMetadata,
        RemoveFunctionMetadata, RemoveStructureMetadata, RemoveTableMetadata
    }

    /// <summary>
    /// RFC function to use for reading table
    /// </summary>
    public enum ReadTableRFC { BBP_RFC_READ_TABLE, RFC_READ_TABLE, CUSTOM_FUNCTION }

    /// <summary>
    /// Use JSON or predefined key-value or  pairs to define parameters to RFC function.
    /// </summary>
    public enum InputType { PARAMETERS, JSON }

    public class ExecuteFunctionInput
    {
        /// <summary>
        /// Connection string.
        /// </summary>
        [PasswordPropertyText]
        [DefaultValue("\"ASHOST=sapserver01;SYSNR=00;CLIENT=000;LANG=EN;USER=SAPUSER;PASSWD=****;IDLE_TIMEOUT=60;\"")]
        public string ConnectionString { get; set; }

        public InputType InputType { get; set; }

        /// <summary>
        /// Function calls in JSON format. Frends cannot use recursive inputs, therefore JSON is used and deserialized.
        /// </summary>
        [DisplayFormat(DataFormatString = "Json")]
        [UIHint(nameof(InputType), "", InputType.JSON)]
        public string InputFunctions { get; set; }

        /// <summary>
        /// Function calls with simple parameters.
        /// </summary>
        [UIHint(nameof(InputType), "", InputType.PARAMETERS)]
        public SimpleFunctionInput SimpleInput { get; set; }
    }

    public class InputQuery
    {
        /// <summary>
        /// Connection string.
        /// </summary>
        [PasswordPropertyText]
        [DefaultValue("\"ASHOST=sapserver01;SYSNR=00;CLIENT=000;LANG=EN;USER=SAPUSER;PASSWD=****;IDLE_TIMEOUT=60;\"")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Table name being queried.
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("MARA")]
        public string TableName { get; set; }

        /// <summary>
        /// Fields queried. Separeted with comma.
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("MATNR")]
        public String Fields { get; set; }

        /// <summary>
        /// Filter, think as WHERE in SQL.
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("MTART EQ 'HAWA'")]
        public String Filter { get; set; }

        /// <summary>
        /// Delimeter used in table.
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("~")]
        public String Delimiter { get; set; }
    }

    public class Options
    {
        /// <summary>
        /// RFC to use for reading table.
        /// </summary>
        [DefaultValue(ReadTableRFC.RFC_READ_TABLE)]
        public ReadTableRFC ReadTableTargetRFC { get; set; }

        /// <summary>
        /// Name of function to be used.
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [UIHint(nameof(ReadTableTargetRFC), "", ReadTableRFC.CUSTOM_FUNCTION)]
        [DefaultValue("NOT IMPLEMENTED YET!")]   // ZRFC_READ_TABLE
        public string CustomFuntionName { get; set; }
    }

}
