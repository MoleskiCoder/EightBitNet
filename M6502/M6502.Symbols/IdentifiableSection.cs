namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            using System;
            using System.Collections;
            using System.Diagnostics;

            public class IdentifiableSection : Section
            {
                [SectionProperty("id")]
                public int ID { get; protected set; }

                protected IdentifiableSection(Parser container)
                : base(container)
                {}

                private bool MaybeExtractFromParsed(string key, out string value)
                {
                    Debug.Assert(this._parsed != null);
                    var found = this._parsed.TryGetValue(key, out var extracted);
                    if (found)
                    {
                        Debug.Assert(extracted != null);
                        value = extracted;
                    }
                    else
                    {
                        value = string.Empty;
                    }
                    return found;
                }

                private bool MaybeExtractIntegerFromParsed(string key, out int value)
                {
                    var available = this.MaybeExtractFromParsed(key, out var extracted);
                    if (!available)
                    {
                        value = 0;
                        return false;
                    }
                    value = ExtractInteger(extracted);
                    return available;
                }

                protected bool MaybeExtractCompoundInteger(string key, out List<int> value)
                {
                    var available = this.MaybeExtractFromParsed(key, out var extracted);
                    if (!available)
                    {
                        value = [];
                        return false;
                    }
                    value = ExtractCompoundInteger(extracted);
                    return available;
                }

                private void SetProperty(string name, object obj)
                {
                    var type = this.GetType();
                    var property = type.GetProperty(name);
                    Debug.Assert(property != null);
                    property.SetValue(this, obj);
                }

                public void ExtractReferences()
                {
                    this.ExtractReferenceProperties();
                    this.ExtractReferencesProperties();
                }

                private void ExtractReferenceProperties()
                {
                    foreach (var (entryName, connection) in this.SectionProperties.ReferenceAttributes)
                    {
                        var hasID = this.MaybeExtractIntegerFromParsed(entryName, out var id);
                        if (hasID)
                        {
                            var (name, type, _) = connection;
                            this.ExtractReferenceProperty(id, name, type);
                        }
                    }
                }

                private void ExtractReferenceProperty(int id, string name, System.Type type)
                {
                    // The reference container in the parent class
                    var referenceSectionProperties = GetSectionProperties(type);
                    var referenceClassAttribute = referenceSectionProperties.ClassAttribute;
                    Debug.Assert(referenceClassAttribute != null);
                    var referencingContainer = referenceClassAttribute.Referencing;

                    // Get the parent container field
                    var containerType = this._container.GetType();
                    Debug.Assert(referencingContainer != null);
                    var containerField = containerType.GetField(referencingContainer);
                    Debug.Assert(containerField != null);
                    var fieldValue = containerField.GetValue(this._container);
                    var fieldList = fieldValue as IList;

                    // Now get the referenced object from the parent container field (via ID as an index)
                    Debug.Assert(fieldList != null);
                    var referencingObject = fieldList[id];

                    // Now set our reference field
                    Debug.Assert(referencingObject != null);
                    this.SetProperty(name, referencingObject);
                }

                private void ExtractReferencesProperties()
                {
                    foreach (var (entryName, connection) in this.SectionProperties.ReferencesAttributes)
                    {
                        // this'll be a set of ids
                        var hasIDs = this.MaybeExtractCompoundInteger(entryName, out var ids);
                        if (hasIDs)
                        {
                            var (name, type, _) = connection;
                            this.ExtractReferencesProperty(ids, name, type);
                        }
                    }
                }

                private void ExtractReferencesProperty(List<int> ids, string name, System.Type type)
                {
                    // The reference container in the parent class
                    //var referenceSectionProperties = GetSectionProperties(type);
                    //var referenceClassAttribute = referenceSectionProperties.ClassAttribute;
                    //Debug.Assert(referenceClassAttribute != null);
                    //var referencingContainer = referenceClassAttribute.Referencing;

                    // Get the parent container field
                    //var containerType = this._container.GetType();
                    //Debug.Assert(referencingContainer != null);
                    //var containerField = containerType.GetField(referencingContainer);
                    //Debug.Assert(containerField != null);
                    //var fieldValue = containerField.GetValue(this._container);
                    //var fieldList = fieldValue as IList;

                    // Now get the referenced object from the parent container field (via ID as an index)
                    //Debug.Assert(fieldList != null);
                    //var referencingObject = fieldList[id];

                    //// Now set our reference field
                    //Debug.Assert(referencingObject != null);
                    //this.SetProperty(name, referencingObject);
                }
            }
        }
    }
}