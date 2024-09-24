namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
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

                private void SetProperty(string name, object obj)
                {
                    var type = this.GetType();
                    var property = type.GetProperty(name);
                    Debug.Assert(property != null);
                    property.SetValue(this, obj);
                }

                public void ExtractReferences()
                {
                    var sectionProperties = this.SectionProperties;
                    foreach (var (entryName, connection) in sectionProperties.ReferenceAttributes)
                    {
                        var hasID = this.MaybeExtractIntegerFromParsed(entryName, out var id);
                        if (!hasID)
                        {
                            continue;
                        }

                        var (name, type, attribute) = connection;

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

                        // The reference ID

                        // Now get the referenced object from the parent container field (via ID as an index)
                        Debug.Assert(fieldList != null);
                        var referencingObject = fieldList[id];

                        // Now set our reference field
                        Debug.Assert(referencingObject != null);
                        this.SetProperty(name, referencingObject);
                    }
                }
            }
        }
    }
}