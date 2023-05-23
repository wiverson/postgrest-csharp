﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Postgrest.Exceptions;
using Postgrest.Extensions;
using Postgrest.Models;

namespace Postgrest.Attributes;

/// <summary>
/// Used to specify that a foreign key relationship exists in PostgreSQL
/// 
/// See: https://postgrest.org/en/stable/api.html#resource-embedding
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ReferenceAttribute : Attribute
{
    /// <summary>
    /// Type of the model referenced
    /// </summary>
    public Type Model { get; }

    /// <summary>
    /// Associated property name
    /// </summary>
    public string PropertyName { get; private set; }

    /// <summary>
    /// Table name of model
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Columns that exist on the model we will select from.
    /// </summary>
    public List<string> Columns { get; private set; } = new();

    /// <summary>
    /// If the performed query is an Insert or Upsert, should this value be ignored? (DEFAULT TRUE)
    /// </summary>
    public bool IgnoreOnInsert { get; private set; }

    /// <summary>
    /// If the performed query is an Update, should this value be ignored? (DEFAULT TRUE)
    /// </summary>
    public bool IgnoreOnUpdate { get; private set; }

    /// <summary>
    /// If Reference should automatically be included in queries on this reference. (DEFAULT TRUE)
    /// </summary>
    public bool IncludeInQuery { get; }

    /// <summary>
    /// As to whether the query will filter top-level rows.
    /// 
    /// See: https://postgrest.org/en/stable/api.html#resource-embedding
    /// </summary>
    public bool ShouldFilterTopLevel { get; }

    /// <param name="model">Model referenced</param>
    /// <param name="includeInQuery">Should referenced be included in queries?</param>
    /// <param name="ignoreOnInsert">Should reference data be excluded from inserts/upserts?</param>
    /// <param name="ignoreOnUpdate">Should reference data be excluded from updates?</param>
    /// <param name="shouldFilterTopLevel">As to whether the query will filter top-level rows.</param>
    /// <param name="propertyName"></param>
    /// <exception cref="Exception"></exception>
    public ReferenceAttribute(Type model, bool includeInQuery = true, bool ignoreOnInsert = true,
        bool ignoreOnUpdate = true, bool shouldFilterTopLevel = true, [CallerMemberName] string propertyName = "")
    {
        if (!IsDerivedFromBaseModel(model))
        {
            throw new PostgrestException("ReferenceAttribute must be used with Postgrest BaseModels.")
                { Reason = FailureHint.Reason.InvalidArgument };
        }

        Model = model;
        IncludeInQuery = includeInQuery;
        IgnoreOnInsert = ignoreOnInsert;
        IgnoreOnUpdate = ignoreOnUpdate;
        PropertyName = propertyName;
        ShouldFilterTopLevel = shouldFilterTopLevel;

        var attr = GetCustomAttribute(model, typeof(TableAttribute));
        TableName = attr is TableAttribute tableAttr ? tableAttr.Name : model.Name;
    }

    internal void ParseProperties(List<ReferenceAttribute>? seenRefs = null)
    {
        seenRefs ??= new List<ReferenceAttribute>();

        ParseColumns();
        ParseRelationships(seenRefs);
    }

    private void ParseColumns()
    {
        foreach (var property in Model.GetProperties())
        {
            var attrs = property.GetCustomAttributes(true);

            foreach (var item in attrs)
            {
                switch (item)
                {
                    case ColumnAttribute columnAttribute:
                        Columns.Add(columnAttribute.ColumnName);
                        break;
                    case PrimaryKeyAttribute primaryKeyAttribute:
                        Columns.Add(primaryKeyAttribute.ColumnName);
                        break;
                }
            }
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is ReferenceAttribute attribute)
        {
            return TableName == attribute.TableName && PropertyName == attribute.PropertyName &&
                   Model == attribute.Model;
        }

        return false;
    }

    private void ParseRelationships(List<ReferenceAttribute> seenRefs)
    {
        foreach (var property in Model.GetProperties())
        {
            var attrs = property.GetCustomAttributes(true);

            foreach (var attr in attrs)
            {
                if (attr is not ReferenceAttribute { IncludeInQuery: true } refAttr) continue;

                if (seenRefs.FirstOrDefault(r => r.Equals(refAttr)) != null) continue;

                seenRefs.Add(refAttr);
                refAttr.ParseProperties(seenRefs);

                Columns.Add(ShouldFilterTopLevel
                    ? $"{refAttr.TableName}!inner({string.Join(",", refAttr.Columns.ToArray())})"
                    : $"{refAttr.TableName}({string.Join(",", refAttr.Columns.ToArray())})");
            }
        }
    }

    private static bool IsDerivedFromBaseModel(Type type) =>
        type.GetInheritanceHierarchy().Any(t => t == typeof(BaseModel));
}