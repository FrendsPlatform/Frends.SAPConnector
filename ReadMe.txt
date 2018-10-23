Add Following in App.Config or Web.Config file. It is separate configuration section for client to access the SAP Server. It also includes the connection paramaters so that it can be used to 
create a connection object

<configSections>
    <sectionGroup name="SAP.Middleware.Connector">
      <sectionGroup name="ClientSettings">
        <section name="DestinationConfiguration" type="SAP.Middleware.Connector.RfcDestinationConfiguration, sapnco"/>
      </sectionGroup>
    </sectionGroup>
  </configSections>

  <SAP.Middleware.Connector>
    <ClientSettings>
      <DestinationConfiguration>
        <destinations>
         <add NAME="DEV" USER="<userName>" PASSWD="<password>" CLIENT="500" SYSID="DEV" LANG="<Language>" ASHOST="<Server IP>" SYSNR="00" MAX_POOL_SIZE="10" IDLE_TIMEOUT="600"/>
      </DestinationConfiguration>
    </ClientSettings>
  </SAP.Middleware.Connector>
  
  
  
  here is the sample code to call this connection
  
  
    using (var connection = new SapConnection("DEV"))
            {
                connection.Open();

                var session = new SapSession(connection);

          
                string dt = DateTime.Now.ToString("yyyyMMdd");

                var command = new SapCommand("<BAPI NAME> ", connection);
                SapParameter param = new SapParameter("CREATED_ON", dt, "structure", "BAPIITABDATA");
                command.Parameters.Add(param);
              
                
                
             
                session.StartSession();

                var resultDataSet = command.ExecuteDataSet();
        

                     
                session.EndSession();

            

            }