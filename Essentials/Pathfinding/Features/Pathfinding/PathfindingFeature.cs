﻿
namespace ME.ECS.Pathfinding.Features {

    using Collections;
    using ME.ECS.Pathfinding;
    using Pathfinding.Components; using Pathfinding.Modules; using Pathfinding.Systems; using Pathfinding.Markers;
    
    namespace Pathfinding.Components {}
    namespace Pathfinding.Modules {}
    namespace Pathfinding.Systems {}
    namespace Pathfinding.Markers {}
    
    #if ECS_COMPILE_IL2CPP_OPTIONS
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
    #endif
    public sealed class PathfindingFeature : Feature {

        private ME.ECS.Pathfinding.Pathfinding pathfindingInstance;
        private Entity pathfindingEntity;

        [UnityEngine.Header("Path smoothing")]
        public bool usePathSmoothing = false;
        public int bezierCurveSubdivisions = 2;
        public float bezierTangentLength = 0.2f;
        
        public void SetInstance(ME.ECS.Pathfinding.Pathfinding pathfinding) {

            this.pathfindingInstance = pathfinding;
            
        }

        public ME.ECS.Pathfinding.Pathfinding GetInstance() {

            return this.pathfindingInstance;

        }

        public Entity GetEntity() {

            return this.pathfindingEntity;

        }
        
        protected override void OnConstruct() {

            this.pathfindingInstance = null;
            
            PathfindingComponentsInitializer.Init(ref this.world.GetStructComponents());
            ComponentsInitializerWorld.Register(PathfindingComponentsInitializer.InitEntity);
            
            var entity = new Entity("Pathfinding");
            entity.Set(new IsPathfinding());
            this.pathfindingEntity = entity;
            
            this.AddSystem<SetPathfindingInstanceSystem>();
            this.AddSystem<BuildGraphsSystem>();
            this.AddSystem<PathfindingUpdateSystem>();

        }

        protected override void OnDeconstruct() {
            
            if (this.pathfindingInstance != null) this.pathfindingInstance.Recycle();
            this.pathfindingInstance = null;
            ComponentsInitializerWorld.UnRegister(PathfindingComponentsInitializer.InitEntity);
            
        }

        public void SetPathFlowField(in Entity entity, ME.ECS.Pathfinding.Path path, Constraint constraint, UnityEngine.Vector3 from, UnityEngine.Vector3 to, bool alignToGraphNodes, bool cacheEnabled) {

            if (alignToGraphNodes == true) {
                
                to = this.GetNearest(to).worldPosition;
                
            }

            entity.Set(new ME.ECS.Pathfinding.Features.PathfindingFlowField.Components.PathFlowField() {
                flowField = BufferArray<byte>.From(path.flowField),
                from = from,
                to = to,
                cacheEnabled = cacheEnabled,
            });
            entity.Set(new IsPathBuilt(), ComponentLifetime.NotifyAllSystems);
            
        }

        public void SetPathNavMesh(in Entity entity, ME.ECS.Pathfinding.Path path, Constraint constraint, UnityEngine.Vector3 from, UnityEngine.Vector3 to) {

            /*if (entity.Has<ME.ECS.Pathfinding.Features.PathfindingNavMesh.Components.PathNavMesh>() == true) {
                ref var oldPath = ref entity.Get<ME.ECS.Pathfinding.Features.PathfindingNavMesh.Components.PathNavMesh>();
                oldPath.path.Dispose();
                oldPath.path = default;
            }*/
            entity.Set(new ME.ECS.Pathfinding.Features.PathfindingNavMesh.Components.PathNavMesh() {
                result = path.result,
                path = new NativeDataBufferArray<UnityEngine.Vector3>(path.navMeshPoints.ToBufferArray()),
            });
            entity.Set(new IsPathBuilt(), ComponentLifetime.NotifyAllSystems);

        }

        public void SetPathAstar(in Entity entity, ME.ECS.Pathfinding.Path path, Constraint constraint, UnityEngine.Vector3 from, UnityEngine.Vector3 to, bool alignToGraphNodes) {
            
            this.SetPathAstar(in entity, path.nodesModified, path.result, constraint, from, to, alignToGraphNodes);
            
        }

        public void SetPathAstar(in Entity entity, ListCopyable<Node> nodes, PathCompleteState result, Constraint constraint, UnityEngine.Vector3 from, UnityEngine.Vector3 to, bool alignToGraphNodes) {

            //entity.Remove<ME.ECS.Pathfinding.Features.Pathfinding.Components.Path>();

            var vPath = PoolListCopyable<UnityEngine.Vector3>.Spawn(nodes.Count);
            for (var i = 0; i < nodes.Count; ++i) {

                var node = nodes[i];
                vPath.Add(node.worldPosition);

            }

            if (nodes.Count > 1) {
            
                var currentPos = from;
                var a = vPath[0];
                var b = vPath[1];
                var target = a + UnityEngine.Vector3.Project(currentPos - a, b - a);
                vPath[0] = target;

            }
            
            var nearestTarget = this.GetNearest(to);
            if (nearestTarget.node.IsSuitable(constraint) == true) {

                if (alignToGraphNodes == true) {
                    
                    vPath.Add(nearestTarget.worldPosition);
                    
                } else {

                    vPath.Add(to);

                }

            }

            if (this.usePathSmoothing == true) {
                var newPath = PathSmoothingUtils.Bezier(vPath, this.bezierCurveSubdivisions, this.bezierTangentLength);
                PoolListCopyable<UnityEngine.Vector3>.Recycle(ref vPath);
                vPath = newPath;
            }

            entity.Set(new ME.ECS.Pathfinding.Features.PathfindingAstar.Components.Path() {
                result = result,
                path = ME.ECS.Collections.BufferArray<UnityEngine.Vector3>.From(vPath),
                nodes = ME.ECS.Collections.BufferArray<ME.ECS.Pathfinding.Node>.From(nodes),
            });
            entity.Set(new IsPathBuilt(), ComponentLifetime.NotifyAllSystems);
                
            PoolListCopyable<UnityEngine.Vector3>.Recycle(ref vPath);

        }

        public ME.ECS.Pathfinding.Path CalculatePath(UnityEngine.Vector3 from, UnityEngine.Vector3 to, Constraint constraint, bool cacheEnabled = false) {
            
            if (this.pathfindingEntity.IsAlive() == false || this.pathfindingEntity.Get<PathfindingInstance>().pathfinding == null) return default;
            return this.pathfindingEntity.Get<PathfindingInstance>().pathfinding.CalculatePath(from, to, constraint, new ME.ECS.Pathfinding.PathCornersModifier(), cacheEnabled: cacheEnabled);
            
        }
        
        public void UpdateGraphs(GraphUpdateObject graphUpdateObject) {
            
            if (this.pathfindingEntity.IsAlive() == false || this.pathfindingEntity.Get<PathfindingInstance>().pathfinding == null) return;
            this.pathfindingEntity.Get<PathfindingInstance>().pathfinding.UpdateGraphs(graphUpdateObject);
            
        }

        public void GetNodesInBounds(ListCopyable<Node> output, UnityEngine.Bounds bounds) {
         
            if (this.pathfindingEntity.IsAlive() == false || this.pathfindingEntity.Get<PathfindingInstance>().pathfinding == null) return;
            this.pathfindingEntity.Get<PathfindingInstance>().pathfinding.GetNodesInBounds(output, bounds, Constraint.Empty);
            
        }

        public void GetNodesInBounds(ListCopyable<Node> output, UnityEngine.Bounds bounds, Constraint constraint) {
         
            if (this.pathfindingEntity.IsAlive() == false || this.pathfindingEntity.Get<PathfindingInstance>().pathfinding == null) return;
            this.pathfindingEntity.Get<PathfindingInstance>().pathfinding.GetNodesInBounds(output, bounds, constraint);
            
        }

        public T GetGraphByIndex<T>(int index) where T : Graph {

            if (this.pathfindingEntity.IsAlive() == false || this.pathfindingEntity.Get<PathfindingInstance>().pathfinding == null) return default;
            return this.pathfindingEntity.Get<PathfindingInstance>().pathfinding.graphs[index] as T;

        }
        
        public UnityEngine.Vector3 ClampPosition(UnityEngine.Vector3 worldPosition) {

            if (this.pathfindingEntity.IsAlive() == false || this.pathfindingEntity.Get<PathfindingInstance>().pathfinding == null) return default;
            return this.pathfindingEntity.Get<PathfindingInstance>().pathfinding.ClampPosition(worldPosition, Constraint.Default);

        }

        public UnityEngine.Vector3 ClampPosition(UnityEngine.Vector3 worldPosition, Constraint constraint) {
            
            if (this.pathfindingEntity.IsAlive() == false || this.pathfindingEntity.Get<PathfindingInstance>().pathfinding == null) return default;
            return this.pathfindingEntity.Get<PathfindingInstance>().pathfinding.ClampPosition(worldPosition, constraint);
            
        }

        public NodeInfo GetNearest(UnityEngine.Vector3 worldPosition) {

            if (this.pathfindingEntity.IsAlive() == false || this.pathfindingEntity.Get<PathfindingInstance>().pathfinding == null) return default;
            return this.pathfindingEntity.Get<PathfindingInstance>().pathfinding.GetNearest(worldPosition, Constraint.Default);

        }

        public NodeInfo GetNearest(UnityEngine.Vector3 worldPosition, Constraint constraint) {
            
            if (this.pathfindingEntity.IsAlive() == false || this.pathfindingEntity.Get<PathfindingInstance>().pathfinding == null) return default;
            return this.pathfindingEntity.Get<PathfindingInstance>().pathfinding.GetNearest(worldPosition, constraint);
            
        }

    }

}