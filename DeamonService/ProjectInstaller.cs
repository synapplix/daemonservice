using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Configuration;
using VAkos;
using System.Xml;
using DeamonService;


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
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load("DeamonService.exe.config");
            ConfigSetting serrings = new ConfigSetting(xmldoc.DocumentElement);
            ConfigSetting installerSettings = serrings["Installer"];

            this.DeamonServiceInstaller.DisplayName = installerSettings["DisplayName"].Value;
            this.DeamonServiceInstaller.ServiceName = installerSettings["ServiceName"].Value;
            this.DeamonServiceInstaller.Description = installerSettings["Description"].Value;

            this.DeamonServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // DeamonServiceProcessInstaller
            // 
            this.DeamonServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.DeamonServiceProcessInstaller.Password = null;
            this.DeamonServiceProcessInstaller.Username = null;
        }
    }
}
