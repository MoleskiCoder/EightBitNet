﻿namespace EightBit
{
    namespace Files
    {
        namespace Symbols
        {
            public class IdentifiableSection : Section
            {
                public int ID => this.TakeInteger("id");

                protected IdentifiableSection() => _ = this._integer_keys.Add("id");

                #region Foreign key constraints

                #region Generic FK access

                protected static T TakeReference<T>(int key, List<T> from) where T : IdentifiableSection
                {
                    var identifiable = from[key];
                    if (identifiable.ID != key)
                    {
                        throw new ArgumentOutOfRangeException(nameof(key), "Retrieved object has a mismatched key");
                    }
                    return identifiable;
                }

                protected T TakeReference<T>(string key, List<T> from) where T : IdentifiableSection => TakeReference(this.TakeInteger(key), from);

                protected T? MaybeTakeReference<T>(string key, List<T> from) where T : IdentifiableSection
                {
                    var id = this.MaybeTakeInteger(key);
                    return id == null ? null : TakeReference(id.Value, from);
                }

                protected List<T> TakeReferences<T>(string key, List<T> from) where T : IdentifiableSection
                {
                    List<T> returned = [];
                    var ids = this.MaybeTakeMultiple(key);
                    if (ids != null)
                    {
                        foreach (var id in ids)
                        {
                            returned.Add(from[id]);
                        }
                    }
                    return returned;
                }

                #endregion

                #region Specific FK access

                protected Module TakeModuleReference(string key = "mod") => this.TakeReference<Module>(key, this._parent.Modules);

                protected File TakeFileReference(string key = "file") => this.TakeReference<File>(key, this._parent.Files);

                protected Type TakeTypeReference(string key = "type") => this.TakeReference<Type>(key, this._parent.Types);

                protected Segment TakeSegmentReference(string key = "seg") => this.TakeReference<Segment>(key, this._parent.Segments);

                protected Scope TakeScopeReference(string key = "scope") => this.TakeReference<Scope>(key, this._parent.Scopes);

                protected Scope? MaybeTakeParentReference(string key = "parent") => this.MaybeTakeReference<Scope>(key, this._parent.Scopes);

                protected Symbol? MaybeTakeSymbolReference(string key = "sym") => this.MaybeTakeReference<Symbol>(key, this._parent.Symbols);

                protected List<Span> TakeSpanReferences(string key = "span") => this.TakeReferences<Span>(key, this._parent.Spans);

                protected List<Line> TakeLineReferences(string key) => this.TakeReferences<Line>(key, this._parent.Lines);

                #endregion

                #endregion
            }
        }
    }
}