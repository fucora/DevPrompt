﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DevPrompt {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DevPrompt.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to JSON parse failure at {0}, token={1}.
        /// </summary>
        internal static string JsonException_Message {
            get {
                return ResourceManager.GetString("JsonException.Message", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to JSON parse failure at {0}, token={1}, {2}.
        /// </summary>
        internal static string JsonException_Message2 {
            get {
                return ResourceManager.GetString("JsonException.Message2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expected comma or close bracket after array.
        /// </summary>
        internal static string JsonParser_ExpectedCommaOrBracket {
            get {
                return ResourceManager.GetString("JsonParser.ExpectedCommaOrBracket", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expected comma or close curly brace after value.
        /// </summary>
        internal static string JsonParser_ExpectedCommaOrCurly {
            get {
                return ResourceManager.GetString("JsonParser.ExpectedCommaOrCurly", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expected a colon after the key name.
        /// </summary>
        internal static string JsonParser_ExpectedKeyColon {
            get {
                return ResourceManager.GetString("JsonParser.ExpectedKeyColon", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expected a string key name.
        /// </summary>
        internal static string JsonParser_ExpectedKeyName {
            get {
                return ResourceManager.GetString("JsonParser.ExpectedKeyName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expected open curly brace.
        /// </summary>
        internal static string JsonParser_ExpectedObject {
            get {
                return ResourceManager.GetString("JsonParser.ExpectedObject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Duplicate key: {0}.
        /// </summary>
        internal static string JsonParser_ExpectedUniqueKey {
            get {
                return ResourceManager.GetString("JsonParser.ExpectedUniqueKey", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expected a value.
        /// </summary>
        internal static string JsonParser_ExpectedValue {
            get {
                return ResourceManager.GetString("JsonParser.ExpectedValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid string.
        /// </summary>
        internal static string JsonParser_InvalidStringToken {
            get {
                return ResourceManager.GetString("JsonParser.InvalidStringToken", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to JSON value is not a {0}.
        /// </summary>
        internal static string JsonValue_WrongType {
            get {
                return ResourceManager.GetString("JsonValue.WrongType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Available.
        /// </summary>
        internal static string PluginsTabType_PluginAvailable {
            get {
                return ResourceManager.GetString("PluginsTabType.PluginAvailable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Installed.
        /// </summary>
        internal static string PluginsTabType_PluginInstalled {
            get {
                return ResourceManager.GetString("PluginsTabType.PluginInstalled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Updates.
        /// </summary>
        internal static string PluginsTabType_PluginUpdates {
            get {
                return ResourceManager.GetString("PluginsTabType.PluginUpdates", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Consoles.
        /// </summary>
        internal static string SettingsTabType_Consoles {
            get {
                return ResourceManager.GetString("SettingsTabType.Consoles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Grab.
        /// </summary>
        internal static string SettingsTabType_Grab {
            get {
                return ResourceManager.GetString("SettingsTabType.Grab", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Tools.
        /// </summary>
        internal static string SettingsTabType_Links {
            get {
                return ResourceManager.GetString("SettingsTabType.Links", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Links.
        /// </summary>
        internal static string SettingsTabType_Tools {
            get {
                return ResourceManager.GetString("SettingsTabType.Tools", resourceCulture);
            }
        }
    }
}