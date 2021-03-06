﻿using System;
using System.IO;
using System.Xml.XPath;

namespace Xeora.Web.Site.Setting
{
    public class Settings : Basics.ISettings
    {
        private StringReader _XPathStream = null;
        private XPathNavigator _XPathNavigator;

        public Settings(string configurationContent)
        {
            if (string.IsNullOrWhiteSpace(configurationContent))
                throw new System.Exception(Global.SystemMessages.CONFIGURATIONCONTENT + "!");

            try
            {
                // Performance Optimization
                this._XPathStream = new StringReader(configurationContent);
                XPathDocument xPathDoc = new XPathDocument(this._XPathStream);

                this._XPathNavigator = xPathDoc.CreateNavigator();
                // !--
            }
            catch (System.Exception)
            {
                this.Dispose();

                throw;
            }

            this.Configurations = new Configurations(ref this._XPathNavigator);
            this.Services = new Services(ref this._XPathNavigator);
            this.URLMappings = new URLMapping(ref this._XPathNavigator);
        }

        public Basics.IConfigurations Configurations { get; private set; }
        public Basics.IServices Services { get; private set; }
        public Basics.IURLMappings URLMappings { get; private set; }

        public void Dispose()
        {
            if (this._XPathStream != null)
            {
                this._XPathStream.Close();
                GC.SuppressFinalize(this._XPathStream);
            }
            GC.SuppressFinalize(this);
        }
    }
}