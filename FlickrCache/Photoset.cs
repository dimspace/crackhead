// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 4.0.30319.1
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

// 
//This source code was auto-generated by MonoXSD
//
namespace FlickrCache {
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class Photoset {
        
        private string titleField;
        
        private System.DateTime lastUpdatedField;
        
        private bool lastUpdatedSpecifiedField;
        
        private PhotosetPhoto[] photoField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Title {
            get {
                return this.titleField;
            }
            set {
                this.titleField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public System.DateTime LastUpdated {
            get {
                return this.lastUpdatedField;
            }
            set {
                this.lastUpdatedField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnore()]
        public virtual bool LastUpdatedSpecified {
            get {
                return this.lastUpdatedSpecifiedField;
            }
            set {
                this.lastUpdatedSpecifiedField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Photo", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public PhotosetPhoto[] Photo {
            get {
                return this.photoField;
            }
            set {
                this.photoField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class PhotosetPhoto {
        
        private string idField;
        
        private string titleField1;
        
        private string urlField;
        
        private string[] tagsField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Id {
            get {
                return this.idField;
            }
            set {
                this.idField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Title {
            get {
                return this.titleField1;
            }
            set {
                this.titleField1 = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Url {
            get {
                return this.urlField;
            }
            set {
                this.urlField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Tags", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string[] Tags {
            get {
                return this.tagsField;
            }
            set {
                this.tagsField = value;
            }
        }
    }
}
