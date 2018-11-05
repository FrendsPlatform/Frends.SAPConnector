using System;
using FRENDS.SAPConnector;
using NUnit.Framework;

namespace Frends.SAPConnector.Tests
{
    [TestFixture]
    public class UnitTest
    {
        private static readonly string ConnectionString2 = "ASHOST=xxx;SYSNR=00;CLIENT=100;LANG=EN;USER=S4H_EWM;PASSWD=Welcome1";


        [SetUp]
        public void Foo()
        {   
            return;
        }

    [Test]

    public void FirstTest()
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

    var results = SAP.ExecuteFunction(input);
    Assert.That((string)results["DATE_GET_WEEK"]["WEEK"], Is.EqualTo("201844"));
    }


    [Test]

    public void SecondTest()
    {
        var input = new ExecuteFunctionInput
        {
            ConnectionString = ConnectionString2,
            InputType = InputType.JSON,
            InputFunctions = "[{\"Name\": \"DATE_GET_WEEK\", \"Fields\": [{\"Name\": \"DATE\", \"Value\": \"20181031\"}] }]",
        };

        var results = SAP.ExecuteFunction(input);
        Assert.That((string)results["DATE_GET_WEEK"]["WEEK"], Is.EqualTo("201844"));
        }
}
}
