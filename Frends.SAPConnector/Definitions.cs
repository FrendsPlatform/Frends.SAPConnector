using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Frends.SAPConnector;

#pragma warning disable 1591


namespace FRENDS.SAPConnector
{
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

        [PasswordPropertyText]
        [DefaultValue("\"ASHOST=sapserver01;SYSNR=00;CLIENT=000;LANG=EN;USER=SAPUSER;PASSWD=****;\"")]
        public string ConnectionString { get; set; }

        public InputType InputType { get; set; }

        /// <summary>
        /// Function calls in JSON format. Frends cannot use recursive inputs, therefore JSON is used and deserialized.
        /// </summary>
        [DisplayFormat(DataFormatString = "Json")]
        [UIHint(nameof(InputType), "", InputType.JSON)]
        public string InputFunctions { get; set; }

        [UIHint(nameof(InputType), "", InputType.PARAMETERS)]
        public SimpleFunctionInput SimpleInput { get; set; }
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

        /// <summary>
        /// Name of function to be used.
        /// </summary>
        [DefaultValue(ReadTableRFC.CUSTOM_FUNCTION)]
        public string CustomFuntionName { get; set; }
    }

}
