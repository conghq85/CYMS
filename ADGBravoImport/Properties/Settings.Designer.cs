﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ADGBravoImport.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.8.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.WebServiceUrl)]
        [global::System.Configuration.DefaultSettingValueAttribute("http://erpdev.adg.vn:8000/sap/bc/srt/rfc/sap/zbarcode_input/300/zbarcode/zbarcode" +
            "")]
        public string ADGBravoImport_SAPService_ZBARCODE {
            get {
                return ((string)(this["ADGBravoImport_SAPService_ZBARCODE"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.WebServiceUrl)]
        [global::System.Configuration.DefaultSettingValueAttribute("http://erp-app.adg.vn:8000/sap/bc/srt/rfc/sap/zsd_order1/900/zsd_order1/zsd_order" +
            "1")]
        public string ADGBravoImport_ADGSAP_ZSD_ORDER1 {
            get {
                return ((string)(this["ADGBravoImport_ADGSAP_ZSD_ORDER1"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.WebServiceUrl)]
        [global::System.Configuration.DefaultSettingValueAttribute("http://erpdev.adg.vn:8000/sap/bc/srt/rfc/sap/zsd_ttcvt/300/zsd_ttcvt/zsd_ttcvt")]
        public string ADGBravoImport_Ser_GH_ZSD_TTCVT {
            get {
                return ((string)(this["ADGBravoImport_Ser_GH_ZSD_TTCVT"]));
            }
        }
    }
}
