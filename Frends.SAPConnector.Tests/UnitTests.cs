using FRENDS.SAPConnector;
using NUnit.Framework;
using Newtonsoft.Json;
using SAP.Middleware.Connector;
using System;
using System.Threading;

namespace Frends.SAPConnector.Tests
{
    [TestFixture]
    public class UnitTest
    {
        private static readonly string ConnectionString2 = "ASHOST=13.94.141.8;SYSNR=00;CLIENT=100;LANG=EN;USER=S4H_EWM;PASSWD=Welcome1";

        [Test]
        public void ExecuteFunctionWithParameters()
        {
            var input = new ExecuteFunctionInput
            {
                ConnectionString = ConnectionString2,
                InputType = InputType.PARAMETERS,
                SimpleInput = new SimpleFunctionInput
                {
                    Functions = new SimpleStructure[]
                    {
                        new SimpleStructure
                        {
                            Name = "DATE_GET_WEEK",
                            Fields = new Field[]
                            {
                                new Field { Name = "DATE", Value = "20181031" }
                            }
                        }
                    }
                }
            };
            var results = SAP.ExecuteFunction(input, new CancellationToken());
            Assert.That((string)results["DATE_GET_WEEK"]["WEEK"], Is.EqualTo("201844"));
        }


        [Test]

        public void ExecuteFunctionWithJSON()
        {
            var input = new ExecuteFunctionInput
            {
                ConnectionString = ConnectionString2,
                InputType = InputType.JSON,
                InputFunctions = "[{\"Name\": \"DATE_GET_WEEK\", \"Fields\": [{\"Name\": \"DATE\", \"Value\": \"20181031\"}] }]",
            };

            var results = SAP.ExecuteFunction(input, new CancellationToken());
            Assert.That((string)results["DATE_GET_WEEK"]["WEEK"], Is.EqualTo("201844"));
        }

        [Test]
        public void ExecuteQuery()
        {
            var input = new InputQuery
            {
                ConnectionString = ConnectionString2,
                TableName = "MARA",
                Fields = "MATNR",
                Filter = "MTART EQ 'HAWA'",
                Delimiter = "~",
            };

            var options = new Options
            {
                CommandTimeoutSeconds = 60,
                ReadTableTargetRFC = ReadTableRFC.RFC_READ_TABLE
            };

            var results = SAP.ExecuteQuery(input, options, new CancellationToken());

            // This data is found on trial version of SAP S/4HANA 1709 FPS01, Fully-Activated Appliance
            string expected = "[{\"MATNR\":\"TG12\"},{\"MATNR\":\"TG10\"},{\"MATNR\":\"TG11\"},{\"MATNR\":\"TG13\"},{\"MATNR\":\"TG14\"},{\"MATNR\":\"TG20\"},{\"MATNR\":\"TG21\"},{\"MATNR\":\"TG22\"},{\"MATNR\":\"QM001\"},{\"MATNR\":\"EWMS4-01\"},{\"MATNR\":\"EWMS4-02\"},{\"MATNR\":\"QM002\"},{\"MATNR\":\"QM003\"},{\"MATNR\":\"QM004\"},{\"MATNR\":\"EWMS4-10\"},{\"MATNR\":\"EWMS4-11\"},{\"MATNR\":\"EWMS4-40\"},{\"MATNR\":\"EWMS4-41\"},{\"MATNR\":\"EWMS4-42\"},{\"MATNR\":\"EWMS4-20\"},{\"MATNR\":\"EWMS4-03\"},{\"MATNR\":\"MZ-TG-Y120\"},{\"MATNR\":\"MZ-TG-Y200\"},{\"MATNR\":\"MZ-TG-Y240\"}]";

            Assert.That(JsonConvert.SerializeObject(results), Is.EqualTo(expected));
        }

        [Test]
        public void ExecuteFunctionWithParametersException()
        {
            var input = new ExecuteFunctionInput
            {
                ConnectionString = ConnectionString2,
                InputType = InputType.JSON,
                InputFunctions = "[{\"Name\": \"DATE_GET_WEEKXD\", \"Fields\": [{\"Name\": \"DATE\", \"Value\": \"20181031\"}] }]",
            };

            try
            {
                var results = SAP.ExecuteFunction(input, new CancellationToken());
            }
            catch (Exception e)
            {
                Assert.That(e.Message, Is.EqualTo("Failed to create function: metadata for function DATE_GET_WEEKXD not available: FU_NOT_FOUND: Function module DATE_GET_WEEKXD does not exist"));

            }
        }

        [Test]
        public void ExecuteQueryException()
        {
            var input = new InputQuery
            {
                ConnectionString = ConnectionString2,
                TableName = "MARANOTEXIST",
                Fields = "MATNR",
                Filter = "MTART EQ 'HAWA'",
                Delimiter = "~",
            };

            var options = new Options
            {
                CommandTimeoutSeconds = 60,
                ReadTableTargetRFC = ReadTableRFC.RFC_READ_TABLE
            };

            try
            {
                var results = SAP.ExecuteQuery(input, options, new CancellationToken());
            }
            catch (Exception e)
            {
                Assert.That(e.Message, Is.EqualTo("Failed to invoke SAP function: TABLE_NOT_AVAILABLE"));
            }
        }

            // https://searchsap.techtarget.com/tip/Getting-around-RFC_READ_TABLE-limitations 

            // tommin spostissa taulu ongelma

    }
}