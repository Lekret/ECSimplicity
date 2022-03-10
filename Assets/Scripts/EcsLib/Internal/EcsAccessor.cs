using System.Collections.Generic;
using System.Linq;
using EcsLib.Api;

namespace EcsLib.Internal
{
    internal sealed class EcsAccessor
    {
        private readonly List<List<EcsFilter>> _typeToFilter = new List<List<EcsFilter>>();
        private readonly EcsWorld _world;

        internal EcsAccessor(EcsWorld world)
        {
            _world = world;
        }

        internal EcsFilterBuilder CreateFilterBuilder()
        {
            return new EcsFilterBuilder(this);
        }
        
        internal void OnComponentChanged(Entity entity, int componentIndex)
        {
            IncreaseFiltersRegistry();
            var filters = _typeToFilter[componentIndex];
            foreach (var filter in filters)
            {
                filter.HandleEntity(entity);
            }
        }

        internal void OnEntityDestroyed(Entity entity)
        {
            foreach (var filters in _typeToFilter)
            {
                foreach (var filter in filters)
                {
                    filter.RemoveEntity(entity);
                }
            }
        }

        internal EcsFilter InternalBuildFilter(IReadOnlyCollection<int> included, IReadOnlyCollection<int> excluded)
        {
            if (TryGetExistingFilter(included, excluded, out var filter))
                return filter;
            return CreateNewFilter(included, excluded);
        }

        private bool TryGetExistingFilter(IReadOnlyCollection<int> included, IReadOnlyCollection<int> excluded, out EcsFilter filter)
        {
            foreach (var filters in _typeToFilter)
            {
                foreach (var f in filters)
                {
                    if (f.MatchesIndices(included, excluded))
                    {
                        filter = f;
                        return true;
                    }
                }
            }
            filter = null;
            return false;
        }
        
        private EcsFilter CreateNewFilter(IEnumerable<int> included, IEnumerable<int> excluded)
        {
            var filter = new EcsFilter(included.ToArray(), excluded.ToArray());
            IncreaseFiltersRegistry();
            RegisterFilter(filter, included);
            RegisterFilter(filter, excluded);
            foreach (var entity in _world.Entities)
            {
                if (entity.IsDestroyed()) 
                    continue;
                filter.HandleEntity(entity);
            }
            return filter;
        }

        private void RegisterFilter(EcsFilter filter, IEnumerable<int> componentIndices)
        {
            foreach (var index in componentIndices)
            {
                _typeToFilter[index].Add(filter);
            }
        }

        private void IncreaseFiltersRegistry()
        {
            while (_typeToFilter.Count < ComponentMeta.Count)
            {
                _typeToFilter.Add(new List<EcsFilter>());
            }
        }
    }
}