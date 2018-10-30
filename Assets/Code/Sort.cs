﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using CatSAT;

[DebuggerDisplay("Sort {" + nameof(Name) + "}")]
public class Sort
{
    public static readonly Dictionary<string, Sort> Sorts = new Dictionary<string, Sort>();
    private static readonly Dictionary<string, Sort> EntitySort = new Dictionary<string, Sort>();
    public static readonly Sort Entity = new Sort("entity", null);
    public static readonly Sort Person = new Sort("person", Entity, "self");
    public static readonly Sort Intimate = new Sort("intimate", Person);
    public static readonly Sort Friend = new Sort("friend", Intimate);
    public static readonly Sort LovedOne = new Sort("loved_one", Intimate, "lover", "pet");
    public static readonly Sort Family = new Sort("family", LovedOne);
    public static readonly Sort Parent = new Sort("parent", Family, "mother", "father");
    public static readonly Sort Sibling = new Sort("sibling", Family, "brother", "sister");

    public readonly string Name;
    public readonly List<string> Instances = new List<string>();
    public readonly List<Sort> Subsorts = new List<Sort>();
    public readonly Sort ParentSort;

    public Sort(string name, Sort parentSort, params string[] instances)
    {
        Name = name;
        Sorts[name] = this;
        ParentSort = parentSort;
        ParentSort?.Subsorts.Add(this);
        Instances.AddRange(instances);
        foreach (var i in instances)
            EntitySort[i] = this;
    }

    public static void DeclareSort(string entity, Sort sort)
    {
        Sort s;
        if (EntitySort.TryGetValue(entity, out s) && s != sort)
            throw new InvalidOperationException($"Sort for {entity} has already been declared to be {s.Name}");
        sort.Instances.Add(entity);
        EntitySort[entity] = sort;
    }

    public static Sort SortOf(string entity, Sort defaultSort = null)
    {
        Sort s;
        if (EntitySort.TryGetValue(entity, out s))
            return s;
        if (defaultSort == null)
            return null;
        EntitySort[entity] = s = defaultSort;
        s.Instances.Add(entity);
        return s;
    }

    public static bool IsA(string entity, Sort sort, bool force=false)
    {
        Sort s;
        if (!EntitySort.TryGetValue(entity, out s))
        {
            if (!force)
                throw new InvalidOperationException($"No declared sort for {entity}");
            EntitySort[entity] = sort;
            return true;
        }
        while (s != null)
            if (s == sort)
                return true;
            else
                s = s.ParentSort;
        return false;
    }

    public IEnumerable<string> Members()
    {
        foreach (var m in Instances)
            yield return m;
        foreach (var sub in Subsorts)
        foreach (var m in sub.Members())
            yield return m;
    }

    public static Func<string,T> Function<T>(string name, Sort s, Func<string, string, T> factory) where T:Variable
    {
        var variables = new Dictionary<string, T>();
        return entity =>
        {
            T value;
            if (variables.TryGetValue(entity, out value))
                return value;
            if (!IsA(entity, s, true))
                throw new ArgumentException($"{entity} is not of sort {s.Name}");
            value = factory(entity, $"{name}({entity})");
            variables[entity] = value;
            return value;
        };
    }
}
