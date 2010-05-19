using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Configuration;
using VAkos;


namespace Org.Infobip.DeamonService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            // 
            // DeamonServiceInstaller
            // 
            Xmlconfig xmlConfig = new Xmlconfig("Install.xml", false);
            this.DeamonServiceInstaller.DisplayName = xmlConfig.Settings["DisplayName"].Value;
            this.DeamonServiceInstaller.ServiceName = xmlConfig.Settings["ServiceName"].Value;
            this.DeamonServiceInstaller.Description = xmlConfig.Settings["Description"].Value;

            this.DeamonServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // DeamonServiceProcessInstaller
            // 
            this.DeamonServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.DeamonServiceProcessInstaller.Password = null;
            this.DeamonServiceProcessInstaller.Username = null;
            /*
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
																					  this.DeamonServiceInstaller,
																					  this.DeamonServiceProcessInstaller});
             */

        }
    }
}
