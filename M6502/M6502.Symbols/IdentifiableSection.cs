namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            using System.Collections.Generic;

            public class IdentifiableSection : Section
            {
                [SectionProperty("id")]
                public int ID { get; protected set; }

                protected IdentifiableSection(Parser container)
                : base(container)
                {}

                #region Foreign key constraints

                #region Generic FK access

                protected static T TakeReference<T>(int key, List<T>? from) where T : IdentifiableSection
                {
                    ArgumentNullException.ThrowIfNull(from);
                    var identifiable = from[key];
                    ArgumentOutOfRangeException.ThrowIfNotEqual(identifiable.ID, key, nameof(key));
                    return identifiable;
                }

                protected T TakeReference<T>(string key, List<T>? from) where T : IdentifiableSection => TakeReference(this.TakeInteger(key), from);

                protected T? MaybeTakeReference<T>(string key, List<T>? from) where T : IdentifiableSection
                {
                    var id = this.MaybeTakeInteger(key);
                    return id == null ? null : TakeReference(id.Value, from);
                }

                protected List<T> TakeReferences<T>(string key, List<T>? from) where T : IdentifiableSection
                {
                    ArgumentNullException.ThrowIfNull(from);
                    var ids = this.MaybeTakeMultiple(key);
                    if (ids != null)
                    {
                        var returned = new List<T>(ids.Count);
                        foreach (var id in ids)
                        {
                            returned.Add(from[id]);
                        }
                        return returned;
                    }
                    return [];
                }

                #endregion

                #region Specific FK access

                protected Module TakeModuleReference(string key = "mod") => this.TakeReference<Module>(key, this._container?.Modules);

                protected File TakeFileReference(string key = "file") => this.TakeReference<File>(key, this._container?.Files);

                protected Type TakeTypeReference(string key = "type") => this.TakeReference<Type>(key, this._container?.Types);

                protected Segment TakeSegmentReference(string key = "seg") => this.TakeReference<Segment>(key, this._container?.Segments);

                protected Scope TakeScopeReference(string key = "scope") => this.TakeReference<Scope>(key, this._container?.Scopes);

                protected Scope? MaybeTakeParentReference(string key = "parent") => this.MaybeTakeReference<Scope>(key, this._container?.Scopes);

                protected Symbol? MaybeTakeSymbolReference(string key = "sym") => this.MaybeTakeReference<Symbol>(key, this._container?.Symbols);

                protected List<Span> TakeSpanReferences(string key = "span") => this.TakeReferences<Span>(key, this._container?.Spans);

                protected List<Line> TakeLineReferences(string key) => this.TakeReferences<Line>(key, this._container?.Lines);

                #endregion

                #endregion
            }
        }
    }
}