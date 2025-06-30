using System;
using System.Collections.Generic;

using UnityEngine;

public interface IModule<TReference>
    where TReference : class
{
    void SetReference(TReference reference);
}

public class ComponentModuleManager<TReference>
    where TReference : Component
{
    List<IModule<TReference>> Modules;

    TReference Reference;

    public void Setup() => Setup(Reference.gameObject);
    public void Setup(GameObject root)
    {
        if (Modules.Count is 0)
        {
            root.GetComponentsInChildren(true, Modules);
        }
        else
        {
            var cache = root.GetComponentsInChildren<IModule<TReference>>(true);
            Modules.AddRange(cache);
        }

        foreach (var module in Modules)
            module.SetReference(Reference);
    }

    public void Add(IModule<TReference> module)
    {
        Modules.Add(module);
        module.SetReference(Reference);
    }

    public bool TryGet<TModule>(out TModule module)
        where TModule : class
    {
        for (int i = 0; i < Modules.Count; i++)
        {
            if (Modules[i] is TModule)
            {
                module = Modules[i] as TModule;
                return true;
            }
        }

        module = default;
        return false;
    }
    public TModule Get<TModule>()
        where TModule : class
    {
        if (TryGet(out TModule module) is false)
            throw new InvalidOperationException($"No Module of Type ({typeof(TModule)}) Found on ({Reference})");

        return module;
    }

    public ComponentModuleManager(TReference Reference)
    {
        Modules = new List<IModule<TReference>>();
    }
}